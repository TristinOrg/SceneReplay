"""Deterministic staged parameter search for offline inverse modeling."""

from __future__ import annotations

import copy
from dataclasses import dataclass
from typing import Any, Callable


Evaluate = Callable[[dict[str, Any], int], tuple[float, dict[str, Any]]]


@dataclass(frozen=True)
class ModelParameter:
    """Describes one bounded numeric degree of freedom in a ShapeDefinition."""

    path: tuple[str | int, ...]
    stage: int
    step: float
    minimum: float
    maximum: float

    @property
    def key(self) -> str:
        return "/" + "/".join(str(part) for part in self.path)


def discover_parameters(model: dict[str, Any], include_profiles: bool = True) -> list[ModelParameter]:
    """Discover stable transform, primitive, and optional profile parameters."""
    root = model.get("root")
    if not isinstance(root, dict):
        raise ValueError("ShapeDefinition requires a root object.")
    result: list[ModelParameter] = []
    _discover_node(root, ("root",), result, True, include_profiles)
    return result


def optimize_model(model: dict[str, Any], evaluate: Evaluate, maximum_evaluations: int = 100,
                   minimum_improvement: float = 0.0005, include_profiles: bool = True,
                   maximum_parameters_per_stage: int = 64, target_score: float = 0.995,
                   maximum_passes_per_stage: int = 4) -> tuple[dict[str, Any], dict[str, Any]]:
    """Optimize a model using bounded staged coordinate search and rollback."""
    if maximum_evaluations < 1 or maximum_evaluations > 10000:
        raise ValueError("Maximum evaluations must be between 1 and 10000.")
    if not 0.0 <= minimum_improvement < 1.0:
        raise ValueError("Minimum improvement must be between zero and one.")
    if not 0.0 < target_score <= 1.0:
        raise ValueError("Target score must be between zero and one.")
    parameters = discover_parameters(model, include_profiles)
    best       = copy.deepcopy(model)
    score, details = evaluate(best, 0)
    history = [{"evaluation": 0, "score": score, "parameter": None, "direction": 0,
                "accepted": True, "details": details}]
    evaluation = 1
    accepted   = 0
    stages = sorted({parameter.stage for parameter in parameters})
    status = "evaluationBudget"
    for stage_index, stage in enumerate(stages):
        stage_parameters = [parameter for parameter in parameters if parameter.stage == stage]
        stage_parameters = stage_parameters[:maximum_parameters_per_stage]
        remaining_stages = len(stages) - stage_index
        stage_budget = evaluation + max(1, (maximum_evaluations - evaluation) // remaining_stages)
        steps = {parameter.key: parameter.step for parameter in stage_parameters}
        for pass_index in range(maximum_passes_per_stage):
            pass_accepted = 0
            for parameter in stage_parameters:
                if evaluation >= min(stage_budget, maximum_evaluations) or score >= target_score:
                    break
                origin = float(_get(best, parameter.path))
                candidates = []
                for direction in (1, -1):
                    if evaluation >= min(stage_budget, maximum_evaluations):
                        break
                    step  = steps[parameter.key]
                    value = min(parameter.maximum, max(parameter.minimum, origin + direction * step))
                    if value == origin:
                        continue
                    candidate = copy.deepcopy(best)
                    _set(candidate, parameter.path, round(value, 6))
                    candidate_score, candidate_details = evaluate(candidate, evaluation)
                    record = {"evaluation": evaluation, "stage": stage, "pass": pass_index,
                              "score": candidate_score, "parameter": parameter.key,
                              "direction": direction, "value": value, "step": step,
                              "accepted": False, "details": candidate_details}
                    history.append(record)
                    candidates.append((candidate_score, candidate, record))
                    evaluation += 1
                if candidates:
                    candidate_score, candidate, record = max(candidates, key=lambda item: item[0])
                    if candidate_score >= score + minimum_improvement:
                        best, score = candidate, candidate_score
                        record["accepted"] = True
                        accepted += 1
                        pass_accepted += 1
            if score >= target_score:
                status = "targetReached"
                break
            if pass_accepted == 0:
                for parameter in stage_parameters:
                    steps[parameter.key] *= 0.5
                if max(steps.values(), default=0.0) < 0.0025:
                    break
            if evaluation >= min(stage_budget, maximum_evaluations):
                break
        if score >= target_score or evaluation >= maximum_evaluations:
            break
    if status != "targetReached" and evaluation < maximum_evaluations:
        status = "converged"
    report = {
        "schema": "scenereplay.offline-optimization-report/1.0",
        "bestScore": round(score, 6),
        "status": status,
        "targetScore": target_score,
        "evaluations": evaluation,
        "acceptedChanges": accepted,
        "discoveredParameters": len(parameters),
        "history": history,
    }
    return best, report


def _discover_node(node: dict[str, Any], path: tuple[str | int, ...], result: list[ModelParameter],
                   is_root: bool, include_profiles: bool) -> None:
    transform = node.get("transform", {})
    for group, limits in (("scale", (0.02, 20.0)), ("position", (-20.0, 20.0))):
        values = transform.get(group, {})
        for axis in ("x", "y", "z"):
            if isinstance(values.get(axis), (int, float)):
                current = abs(float(values[axis]))
                step    = max(0.01, current * (0.12 if group == "scale" else 0.08))
                stage   = 0 if is_root and group == "scale" else 1
                result.append(ModelParameter(path + ("transform", group, axis), stage, step, *limits))
    for name, value in sorted((node.get("parameters") or {}).items()):
        if isinstance(value, (int, float)):
            current = float(value)
            result.append(ModelParameter(path + ("parameters", name), 1,
                                         max(0.01, abs(current) * 0.12), 0.0, max(10.0, abs(current) * 4.0)))
    if include_profiles:
        _discover_points(node.get("profile"), path + ("profile",), result)
        for section_name in ("profileSections", "profileCageSections"):
            for index, section in enumerate(node.get(section_name) or []):
                _discover_points(section.get("profile"), path + (section_name, index, "profile"), result)
    for index, child in enumerate(node.get("children") or []):
        _discover_node(child, path + ("children", index), result, False, include_profiles)


def _discover_points(points: Any, path: tuple[str | int, ...], result: list[ModelParameter]) -> None:
    if not isinstance(points, list):
        return
    for index, point in enumerate(points):
        if not isinstance(point, dict):
            continue
        for axis in ("x", "y"):
            if isinstance(point.get(axis), (int, float)):
                result.append(ModelParameter(path + (index, axis), 2, 0.04, -2.0, 2.0))


def _get(value: Any, path: tuple[str | int, ...]) -> Any:
    for part in path:
        value = value[part]
    return value


def _set(value: Any, path: tuple[str | int, ...], replacement: float) -> None:
    for part in path[:-1]:
        value = value[part]
    value[path[-1]] = replacement
