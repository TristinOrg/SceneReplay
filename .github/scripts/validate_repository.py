#!/usr/bin/env python3
"""Validate release-critical SceneReplay package structure without requiring Unity."""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PACKAGES = {
    "com.scenereplay.schema": {},
    "com.scenereplay.core": {"com.scenereplay.schema": "0.1.0"},
    "com.scenereplay.unity": {
        "com.scenereplay.schema": "0.1.0",
        "com.scenereplay.core": "0.1.0",
    },
    "com.scenereplay.lowpoly": {
        "com.scenereplay.schema": "0.1.0",
        "com.scenereplay.core": "0.1.0",
        "com.scenereplay.unity": "0.1.0",
    },
}
VERSION = "0.1.0"
TAG = f"v{VERSION}"
REQUIRED_ROOT_FILES = (
    "CHANGELOG.md",
    "COMMERCIAL-LICENSE.md",
    "CONTRIBUTING.md",
    "COPYING",
    "LICENSE.md",
    "README.md",
    "SECURITY.md",
)


def load_json(path: Path) -> dict:
    with path.open(encoding="utf-8") as stream:
        return json.load(stream)


def validate_packages(errors: list[str]) -> None:
    for package_name, local_dependencies in PACKAGES.items():
        package_root = ROOT / "Packages" / package_name
        manifest = load_json(package_root / "package.json")
        if manifest.get("name") != package_name:
            errors.append(f"{package_name}: package name does not match its directory")
        if manifest.get("version") != VERSION:
            errors.append(f"{package_name}: expected version {VERSION}")
        if manifest.get("unity") != "2022.3":
            errors.append(f"{package_name}: expected Unity 2022.3 compatibility")
        if f"/{TAG}/" not in manifest.get("documentationUrl", ""):
            errors.append(f"{package_name}: documentation URL is not pinned to {TAG}")
        if f"/{TAG}/" not in manifest.get("changelogUrl", ""):
            errors.append(f"{package_name}: changelog URL is not pinned to {TAG}")
        if f"/{TAG}/" not in manifest.get("licensesUrl", ""):
            errors.append(f"{package_name}: license URL is not pinned to {TAG}")

        dependencies = manifest.get("dependencies", {})
        for dependency, version in local_dependencies.items():
            if dependencies.get(dependency) != version:
                errors.append(f"{package_name}: expected {dependency}@{version}")

        for source in package_root.rglob("*.cs"):
            if not source.with_suffix(source.suffix + ".meta").exists():
                errors.append(f"Missing Unity meta file for {source.relative_to(ROOT)}")


def validate_json_files(errors: list[str]) -> None:
    for path in (ROOT / "Packages").rglob("*.json"):
        try:
            document = load_json(path)
        except (OSError, json.JSONDecodeError) as exception:
            errors.append(f"Invalid JSON in {path.relative_to(ROOT)}: {exception}")
            continue

        if path.name.endswith(".schema.json"):
            schema_id = document.get("$id", "")
            if "/SceneReplay/" in schema_id and f"/SceneReplay/{TAG}/" not in schema_id:
                errors.append(f"Schema ID is not pinned to {TAG}: {path.relative_to(ROOT)}")


def validate_engine_boundaries(errors: list[str]) -> None:
    for package_name in ("com.scenereplay.schema", "com.scenereplay.core"):
        runtime_root = ROOT / "Packages" / package_name / "Runtime"
        for path in runtime_root.rglob("*.cs"):
            source = path.read_text(encoding="utf-8")
            if "UnityEngine" in source or "UnityEditor" in source:
                errors.append(f"Engine dependency leaked into {path.relative_to(ROOT)}")


def main() -> int:
    errors: list[str] = []
    for filename in REQUIRED_ROOT_FILES:
        if not (ROOT / filename).is_file():
            errors.append(f"Missing release document: {filename}")

    validate_packages(errors)
    validate_json_files(errors)
    validate_engine_boundaries(errors)

    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    print(f"SceneReplay {VERSION} repository validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
