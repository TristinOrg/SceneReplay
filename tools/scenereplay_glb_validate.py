#!/usr/bin/env python3
"""Validate SceneReplay GLB output locally and through an optional external validator."""

from __future__ import annotations

import json
import struct
import subprocess
from pathlib import Path
from typing import Any


GLB_MAGIC = 0x46546C67
JSON_CHUNK = 0x4E4F534A
BIN_CHUNK = 0x004E4942


def validate_glb(path: Path, validator: list[str] | None = None, timeout: int = 120) -> dict[str, Any]:
    """Validate one binary glTF asset and optionally invoke an external validator."""
    path = path.resolve()
    if not path.is_file() or path.suffix.lower() != ".glb":
        raise ValueError("GLB validation requires an existing .glb asset")

    document, binary_size = _read_glb(path)
    errors = _validate_document(document, binary_size)
    result: dict[str, Any] = {
        "schema": "scenereplay.glb-validation-report/1.0",
        "path": str(path),
        "bytes": path.stat().st_size,
        "valid": not errors,
        "errors": errors,
        "summary": {
            "scenes": len(document.get("scenes", [])),
            "nodes": len(document.get("nodes", [])),
            "meshes": len(document.get("meshes", [])),
            "materials": len(document.get("materials", [])),
            "accessors": len(document.get("accessors", [])),
        },
    }
    if validator:
        result["externalValidator"] = _run_external(path, validator, timeout)
        result["valid"] = result["valid"] and result["externalValidator"]["valid"]
    return result


def _read_glb(path: Path) -> tuple[dict[str, Any], int]:
    data = path.read_bytes()
    if len(data) < 20:
        raise ValueError("GLB file is shorter than its header and JSON chunk")
    magic, version, declared_length = struct.unpack_from("<III", data)
    if magic != GLB_MAGIC:
        raise ValueError("GLB magic is invalid")
    if version != 2:
        raise ValueError(f"Unsupported GLB version: {version}")
    if declared_length != len(data):
        raise ValueError(f"GLB declared length {declared_length} does not match {len(data)} bytes")

    offset = 12
    chunks: list[tuple[int, bytes]] = []
    while offset < len(data):
        if offset + 8 > len(data):
            raise ValueError("GLB contains a truncated chunk header")
        length, chunk_type = struct.unpack_from("<II", data, offset)
        offset += 8
        if offset + length > len(data):
            raise ValueError("GLB contains a truncated chunk")
        chunks.append((chunk_type, data[offset:offset + length]))
        offset += length
    if not chunks or chunks[0][0] != JSON_CHUNK:
        raise ValueError("GLB first chunk must be JSON")
    if len(chunks) > 2 or (len(chunks) == 2 and chunks[1][0] != BIN_CHUNK):
        raise ValueError("GLB may contain only one JSON chunk followed by one BIN chunk")
    try:
        document = json.loads(chunks[0][1].decode("utf-8").rstrip(" \t\r\n\0"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exception:
        raise ValueError(f"GLB JSON chunk is invalid: {exception}") from exception
    return document, len(chunks[1][1]) if len(chunks) == 2 else 0


def _validate_document(document: dict[str, Any], binary_size: int) -> list[str]:
    errors: list[str] = []
    if (document.get("asset") or {}).get("version") != "2.0":
        errors.append("asset.version must be 2.0")
    scenes = document.get("scenes") or []
    nodes = document.get("nodes") or []
    meshes = document.get("meshes") or []
    if not scenes:
        errors.append("at least one scene is required")
    scene_index = document.get("scene", 0)
    if scenes and (not isinstance(scene_index, int) or not 0 <= scene_index < len(scenes)):
        errors.append("default scene index is out of range")
    for index, node in enumerate(nodes):
        mesh = node.get("mesh")
        if mesh is not None and (not isinstance(mesh, int) or not 0 <= mesh < len(meshes)):
            errors.append(f"nodes[{index}].mesh is out of range")
        for child in node.get("children", []):
            if not isinstance(child, int) or not 0 <= child < len(nodes):
                errors.append(f"nodes[{index}].children contains an out-of-range index")
    buffers = document.get("buffers") or []
    if binary_size and not buffers:
        errors.append("BIN chunk requires buffers[0]")
    if buffers and buffers[0].get("byteLength", 0) > binary_size:
        errors.append("buffers[0].byteLength exceeds the BIN chunk")
    return errors


def _run_external(path: Path, command: list[str], timeout: int) -> dict[str, Any]:
    if not any("{input}" in part for part in command):
        raise ValueError("Validator command must contain an {input} placeholder")
    arguments = [part.replace("{input}", str(path)) for part in command]
    completed = subprocess.run(arguments, capture_output=True, text=True, timeout=timeout, shell=False)
    output = (completed.stdout or completed.stderr).strip()
    return {
        "valid": completed.returncode == 0,
        "exitCode": completed.returncode,
        "executable": Path(arguments[0]).name,
        "output": output[-4000:],
    }
