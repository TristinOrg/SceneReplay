#!/usr/bin/env python3
"""Run an explicitly configured external converter against a SceneReplay GLB."""

from __future__ import annotations

import subprocess
import json
from pathlib import Path
from typing import Any


SUPPORTED_FORMATS = {"fbx", "usd", "usda", "usdc", "usdz"}


def export_external(
    source: Path,
    output: Path,
    command: list[str],
    timeout: int = 300,
    converter: dict[str, Any] | None = None,
) -> dict:
    """Invoke a converter without a shell and verify that it produced the requested asset."""
    source = source.resolve()
    output = output.resolve()
    if not source.is_file() or source.suffix.lower() != ".glb":
        raise ValueError("External export requires an existing SceneReplay .glb source")
    target_format = output.suffix.lower().lstrip(".")
    if target_format not in SUPPORTED_FORMATS:
        raise ValueError(f"Unsupported external export format: {target_format or '(none)'}")
    if not command:
        raise ValueError("External export requires a converter command")

    arguments = [part.replace("{input}", str(source)).replace("{output}", str(output)) for part in command]
    if not any("{input}" in part for part in command) or not any("{output}" in part for part in command):
        raise ValueError("Converter command must contain both {input} and {output} placeholders")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.unlink(missing_ok=True)
    completed = subprocess.run(arguments, capture_output=True, text=True, timeout=timeout, shell=False)
    if completed.returncode != 0:
        detail = (completed.stderr or completed.stdout).strip()
        raise RuntimeError(f"Converter exited with {completed.returncode}: {detail[-1000:]}")
    if not output.is_file() or output.stat().st_size == 0:
        raise RuntimeError("Converter reported success but did not create a non-empty output asset")
    result = {
        "schema": "scenereplay.external-export-report/1.0",
        "format": target_format,
        "path": str(output),
        "bytes": output.stat().st_size,
        "converter": Path(arguments[0]).name,
    }
    if converter:
        result["converterProfile"] = {
            "id": converter["id"],
            "version": converter["version"],
            "license": converter["license"],
        }
    return result


def export_with_profile(source: Path, output: Path, profile_path: Path, timeout: int = 300) -> dict:
    """Load an auditable converter profile and export through its declared command."""
    profile_path = profile_path.resolve()
    try:
        profile = json.loads(profile_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exception:
        raise ValueError(f"Converter profile could not be read: {exception}") from exception
    _validate_profile(profile, output.suffix.lower().lstrip("."))
    result = export_external(source, output, profile["command"], timeout, profile)
    result["converterProfile"]["source"] = str(profile_path)
    return result


def _validate_profile(profile: dict[str, Any], target_format: str) -> None:
    if profile.get("schema") != "scenereplay.converter-profile/1.0":
        raise ValueError("Unsupported converter profile schema")
    for field in ("id", "version", "license"):
        if not isinstance(profile.get(field), str) or not profile[field].strip():
            raise ValueError(f"Converter profile requires a non-empty {field}")
    formats = profile.get("formats")
    if not isinstance(formats, list) or target_format not in formats:
        raise ValueError(f"Converter profile does not support {target_format or '(none)'}")
    command = profile.get("command")
    if not isinstance(command, list) or not command or not all(isinstance(item, str) for item in command):
        raise ValueError("Converter profile requires a string command array")
