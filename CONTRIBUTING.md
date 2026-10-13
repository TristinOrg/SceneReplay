# Contributing to SceneReplay

SceneReplay welcomes focused issues and pull requests that preserve its engine-agnostic framework boundary.

## Development requirements

- Unity 2022.3 LTS
- Git
- Python 3.10 or newer for repository validation

## Architecture rules

- Keep `SceneReplay.Schema` and `SceneReplay.Core` free of Unity, render-pipeline, concrete-style, and AI-provider dependencies.
- Add concrete geometry and style behavior outside Core.
- Preserve stable node IDs and editable generated hierarchies.
- Keep Runtime, Editor, and test assemblies separate.
- Add XML summaries to public APIs.
- Avoid hidden update loops, coroutines, and per-instance resources in framework code.

## Before submitting

1. Run `python .github/scripts/validate_repository.py`.
2. Open the project in Unity 2022.3 and wait for compilation to finish.
3. Run all EditMode tests.
4. Verify affected presets or runtime workflows in the Editor.
5. Keep unrelated scenes, settings, and generated files out of the commit.

Use Conventional Commits, for example `feat(core): add shape metadata` or `fix(lowpoly): preserve cage winding`.

Changes to published JSON contracts must remain versioned. Breaking contract changes require a new schema ID rather than silently changing the meaning of an existing version.

## Contribution licensing

SceneReplay is dual-licensed. By submitting a contribution, you represent that you have the right to submit it and grant Tristin Wen a perpetual, worldwide, non-exclusive, royalty-free, irrevocable license to use, reproduce, modify, distribute, sublicense, and relicense the contribution under the project's open-source and commercial licenses. Do not submit code whose license is incompatible with these terms.
