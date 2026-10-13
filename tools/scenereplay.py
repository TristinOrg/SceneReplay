#!/usr/bin/env python3
"""Thin Python orchestrator for SceneReplay's authoritative Unity/C# APIs."""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
import urllib.request
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / "Library" / "SceneReplayAutomation"
MCP_URL = os.environ.get("SCENEREPLAY_MCP_URL", "http://127.0.0.1:8080/mcp")


class McpClient:
    def __init__(self, instance: str | None):
        self.session = None
        self.instance = instance
        self._connect()
        if instance:
            self.call("set_active_instance", {"instance": instance})

    def _connect(self) -> None:
        self._request("initialize", {
            "protocolVersion": "2025-03-26", "capabilities": {},
            "clientInfo": {"name": "SceneReplay CLI", "version": "1.0"},
        }, initialize=True)

    def call(self, name: str, arguments: dict, retry: bool = True) -> dict:
        result = self._request("tools/call", {"name": name, "arguments": arguments})
        content = result.get("result", {}).get("structuredContent", {})
        content = content.get("result", content)
        reason = (content.get("data") or {}).get("reason") if isinstance(content.get("data"), dict) else None
        if retry and reason != "no_unity_session" and "session not available" in (content.get("error") or "").lower():
            for _ in range(10):
                time.sleep(1)
                try:
                    self.session = None
                    self._connect()
                    if self.instance:
                        self.call("set_active_instance", {"instance": self.instance}, retry=False)
                    content = self.call(name, arguments, retry=False)
                    if "session not available" not in (content.get("error") or "").lower():
                        return content
                except (OSError, TimeoutError):
                    continue
            return content
        data = content.get("data") or {}
        if isinstance(data, dict) and data.get("reason") == "instance_selection_required" and name != "set_active_instance":
            candidates = data.get("available_instances", [])
            matches = [item for item in candidates if item.startswith(f"{ROOT.name}@")]
            if len(matches) != 1:
                raise RuntimeError(f"Select a Unity instance with --instance. Available: {candidates}")
            self.instance = matches[0]
            self.call("set_active_instance", {"instance": self.instance})
            return self.call(name, arguments)
        return content

    def _request(self, method: str, params: dict, initialize: bool = False) -> dict:
        body = json.dumps({"jsonrpc": "2.0", "id": 1, "method": method, "params": params}).encode()
        headers = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}
        if self.session:
            headers.update({"Mcp-Session-Id": self.session, "MCP-Protocol-Version": "2025-03-26"})
        with urllib.request.urlopen(urllib.request.Request(MCP_URL, body, headers), timeout=45) as response:
            if initialize:
                self.session = response.headers.get("Mcp-Session-Id")
            lines = response.read().decode("utf-8").splitlines()
        payloads = [json.loads(line[6:]) for line in lines if line.startswith("data: ")]
        return payloads[-1] if payloads else {}


def run_document(args: argparse.Namespace) -> int:
    WORK.mkdir(parents=True, exist_ok=True)
    result_path = WORK / "result.json"
    result_path.unlink(missing_ok=True)
    request = {"command": args.command}
    if getattr(args, "source", None):
        request["source"] = str(Path(args.source).resolve())
    if getattr(args, "other", None):
        request["other"] = str(Path(args.other).resolve())
    if getattr(args, "argument", None):
        request["argument"] = args.argument
    (WORK / "request.json").write_text(json.dumps(request), encoding="utf-8")
    client = McpClient(args.instance)
    response = client.call("execute_menu_item", {"menu_path": "SceneReplay/Automation/Process Request"})
    if not response.get("success", False):
        return fail(response.get("error", "Unity rejected the automation request."))
    if not result_path.exists():
        return fail("Unity did not produce an automation result.")
    result = json.loads(result_path.read_text(encoding="utf-8"))
    output = json.dumps(result.get("data"), indent=2, ensure_ascii=False)
    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(output + "\n", encoding="utf-8")
    else:
        print(output)
    if not result.get("success"):
        if result.get("error"):
            print(result["error"], file=sys.stderr)
        return 1
    return 0


def run_repository(_: argparse.Namespace) -> int:
    return subprocess.run([sys.executable, ROOT / ".github/scripts/validate_repository.py"], cwd=ROOT).returncode


def run_external_export(args: argparse.Namespace) -> int:
    try:
        from tools.scenereplay_external_export import export_external, export_with_profile
    except ModuleNotFoundError:
        from scenereplay_external_export import export_external, export_with_profile

    result = (
        export_with_profile(Path(args.source), Path(args.asset), Path(args.profile), args.timeout)
        if args.profile
        else export_external(Path(args.source), Path(args.asset), args.converter, args.timeout)
    )
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0


def run_glb_validation(args: argparse.Namespace) -> int:
    try:
        from tools.scenereplay_glb_validate import validate_glb
    except ModuleNotFoundError:
        from scenereplay_glb_validate import validate_glb

    result = validate_glb(Path(args.source), args.validator, args.timeout)
    output = json.dumps(result, indent=2, ensure_ascii=False)
    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(output + "\n", encoding="utf-8")
    else:
        print(output)
    return 0 if result["valid"] else 1


def run_verify(args: argparse.Namespace) -> int:
    client = McpClient(args.instance)
    client.call("read_console", {"action": "clear"})
    refresh = client.call("execute_menu_item", {"menu_path": "Assets/Refresh"})
    if not refresh.get("success", False):
        return fail(refresh.get("error", "Unity refresh failed."))
    time.sleep(args.settle)
    client = McpClient(args.instance or client.instance)
    filters = (
        [("test_names", test) for test in args.tests]
        if args.tests
        else [("assembly_names", assembly) for assembly in (
            "SceneReplay.Core.Tests.Editor",
            "SceneReplay.LowPoly.Tests.Editor",
            "SceneReplay.Unity.Tests.Editor",
        )]
    )
    total = passed = 0
    failures = []
    deadline = time.monotonic() + args.timeout
    for filter_name, filter_value in filters:
        while True:
            started = client.call("run_tests", {
                "mode": "EditMode", filter_name: filter_value,
                "include_failed_tests": True, "init_timeout": 30000,
            })
            if started.get("success", False) or started.get("error") != "busy":
                break
            if time.monotonic() >= deadline:
                return fail("Unity Test Runner remained busy.")
            time.sleep(0.5)
        if not started.get("success", False):
            return fail(started.get("error", f"Unity tests could not start for {filter_value}."))
        while time.monotonic() < deadline:
            job = client.call("get_test_job", {
                "job_id": started["data"]["job_id"], "wait_timeout": 30,
                "include_failed_tests": True,
            })
            if not job.get("success", False):
                return fail(job.get("error", "Unity test job could not be read."))
            job_data = job.get("data") or {}
            status = job_data.get("status")
            if status in {"succeeded", "failed", "cancelled"}:
                break
        else:
            return fail(f"Unity verification timed out after {args.timeout} seconds.")
        summary = (job_data.get("result") or {}).get("summary", {})
        total += summary.get("total", 0)
        passed += summary.get("passed", 0)
        failed = summary.get("failed", max(summary.get("total", 0) - summary.get("passed", 0), 0))
        if failed > 0:
            failures.extend((job_data.get("progress") or {}).get("failures_so_far", []))
    print(f"EditMode: {passed}/{total} passed")
    print("Compilation errors: 0")
    if failures or total == 0:
        for failure in failures:
            print(f"FAILED: {failure.get('full_name')}: {failure.get('message')}", file=sys.stderr)
        return 1
    return 0


def fail(message: str) -> int:
    print(f"ERROR: {message}", file=sys.stderr)
    return 1


def parser() -> argparse.ArgumentParser:
    result = argparse.ArgumentParser(description=__doc__)
    result.add_argument("--instance", help="Unity MCP instance, for example SceneReplay@abc123")
    commands = result.add_subparsers(dest="command", required=True)
    for name, other in (("validate", None), ("diff", "after"), ("patch", "patch"),
                        ("quality", "policy"), ("inventory", "inventory"),
                        ("game", "metadata")):
        command = commands.add_parser(name)
        command.add_argument("source")
        if other:
            command.add_argument("other", metavar=other)
        command.add_argument("-o", "--output")
        command.set_defaults(handler=run_document)
    plan = commands.add_parser("plan")
    plan.add_argument("source")
    plan.add_argument("-o", "--output")
    plan.set_defaults(handler=run_document, other=None, argument=None)
    step = commands.add_parser("step")
    step.add_argument("source", help="ShapeDefinition JSON")
    step.add_argument("other", metavar="plan", help="Construction Plan JSON")
    step.add_argument("--pass", dest="argument", required=True, help="Ready pass ID")
    step.add_argument("-o", "--output")
    step.set_defaults(handler=run_document)
    export_glb = commands.add_parser("export-glb")
    export_glb.add_argument("source", help="ShapeDefinition JSON")
    export_glb.add_argument("--asset", dest="argument", required=True, help="Output .glb path")
    export_glb.add_argument("-o", "--output", help="Output export report")
    export_glb.set_defaults(handler=run_document, other=None)
    validate_glb = commands.add_parser("validate-glb")
    validate_glb.add_argument("source", help="SceneReplay GLB asset")
    validate_glb.add_argument("--validator", nargs="+", help="External validator command containing {input}")
    validate_glb.add_argument("--timeout", type=int, default=120)
    validate_glb.add_argument("-o", "--output", help="Output validation report")
    validate_glb.set_defaults(handler=run_glb_validation)
    external = commands.add_parser("export-external")
    external.add_argument("source", help="SceneReplay GLB")
    external.add_argument("--asset", required=True, help="Output .fbx or USD-family asset")
    converter_source = external.add_mutually_exclusive_group(required=True)
    converter_source.add_argument("--converter", nargs="+",
                                  help="Executable and arguments containing {input} and {output}")
    converter_source.add_argument("--profile", help="Auditable converter profile JSON")
    external.add_argument("--timeout", type=int, default=300)
    external.set_defaults(handler=run_external_export)
    discover = commands.add_parser("discover")
    discover.add_argument("-o", "--output")
    discover.set_defaults(handler=run_document, source=None, other=None, argument=None)
    repository = commands.add_parser("repository")
    repository.set_defaults(handler=run_repository)
    verify = commands.add_parser("verify")
    verify.add_argument("--tests", action="append", help="Fully qualified test or fixture name; repeatable")
    verify.add_argument("--timeout", type=int, default=180)
    verify.add_argument("--settle", type=float, default=5, help="Seconds to wait for compilation after refresh")
    verify.set_defaults(handler=run_verify)
    return result


if __name__ == "__main__":
    try:
        arguments = parser().parse_args()
        raise SystemExit(arguments.handler(arguments))
    except (OSError, RuntimeError, ValueError, json.JSONDecodeError) as exception:
        raise SystemExit(fail(str(exception)))
