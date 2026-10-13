# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `D:/git/ShapeForge`
- Purpose: semantic intermediate layer between AI-authored 3D intent and engine-native assets; Low Poly is the first implementation.
- Last analyzed: 2026-07-29
- Last analyzed commit: `ef20d77`

## Confirmed Environment

- Unity version: 2022.3.62f3
- Render pipeline: Built-in Render Pipeline
- Input system: legacy Input Manager
- Target platforms: not yet defined

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Tests | Unity Test Framework 1.1.33 | Confirmed | `Packages/packages-lock.json` |
| Rendering | No SRP package or configured render pipeline | Confirmed | `Packages/manifest.json`, `ProjectSettings/GraphicsSettings.asset` |
| Networking | No networking framework detected | Confirmed | `Packages/manifest.json` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scenes` | Default Unity sample scene | Confirmed | repository files |
| `Packages/com.shapeforge.core` | Engine-neutral validation and style resolution | Confirmed | package manifest |
| `Packages/com.shapeforge.schema` | Engine-agnostic versioned data contracts | Confirmed | package manifest |
| `Packages/com.shapeforge.unity` | Unity generation and JSON adapter | Confirmed | package manifest |
| `Packages/com.shapeforge.lowpoly` | Official Low Poly implementation | Confirmed | package manifest |
| `Docs/AI` | Concise persistent project context | Confirmed | this document |

## Assembly Boundaries

Schema and Core use assemblies with no UnityEngine references. Unity adapts Schema/Core to GameObjects and JSON. LowPoly references the Unity Adapter; dependencies remain one-directional.

## Scenes And Startup Flow

- Build scenes: none enabled.
- Likely startup scene: unknown; `Assets/Scenes/SampleScene.unity` exists but is not in Build Settings.
- Scene loading flow: none implemented.

## Architecture

Schema provides versioned engine-agnostic shape, style, patch, construction, quality, gameplay, and rig documents with published Draft 2020-12 JSON Schema contracts and minimal external-tool examples. Core provides validation, deterministic diff and patch operations, cached style inheritance, fluent authoring, engine-neutral transform contracts, backend-independent shape capabilities, semantic completeness, and semantic-template compilation/discovery without UnityEngine. The Unity Adapter owns JSON, validated generic specification serialization, GameObject generation, lifecycle-safe appearance, discovery export, portable GLB export, and stable transform resolution. LowPoly supplies a cached eleven-shape capability catalog including independent profile cages with bounded interpolated rings, the first versioned Stylized Human semantic compiler, reusable generation pipelines, cached geometry, modern and traditional environment presets, and undoable Editor preview commands.

## Coding Conventions

- Follow `C:/Users/Administrator/.codex/PREFERENCES.md` and `.editorconfig`.
- Use English comments and concise XML documentation for public APIs.
- Keep runtime and editor assemblies separate.

## Testing And Validation

- Unity Test Framework is installed.
- First-party EditMode tests cover fluent authoring, published JSON contracts, cached style inheritance, validated capability and semantic-template discovery/export, runtime generation, prepared count- and time-budgeted batches, hierarchy adaptation, validation, lifecycle-safe appearance, shared render resources, procedural geometry including independent profile cages, remaining presets, and motion-ready pivots.
- `ShapeForge > Diagnostics > Benchmark JSON Generation` measures 200 alternating Table and Robot JSON generations, managed heap growth, and shared render-resource counts without saving generated objects.
- The benchmark reports JSON parsing and prepared-definition generation separately. Repeated runtime models should call `LowPolyModelGenerator.ParseJson` once and reuse the validated definition.
- Prepared batches use `UnityShapeGenerationPlan` to validate an immutable definition once before repeated generation.
- `LowPolyGenerationBatch` supports explicit model-count and elapsed-time step budgets without coroutines or global scheduling state.
- GitHub Actions validates package versions and dependencies, JSON syntax, release-pinned Schema IDs, required `.meta` files, release documents, and the Schema/Core engine boundary without requiring a Unity license.
- The `v0.1.0` release baseline passed all 77 EditMode tests in an isolated Unity 2022.3.62f3 project. No first-party PlayMode or player-build suite exists because ShapeForge currently ships packages rather than a player application.

## Available Unity Tooling

- Repository and Git CLI access: available.
- Unity MCP and live Editor inspection: available through CoplayDev Unity MCP 10.1.0.
- Unity Editor-generated C# projects exist, but no first-party code exists to compile.

## Important Constraints

- Shape is the smallest public modelling concept.
- Geometry, appearance, style, and animation data must remain extensible.
- Low Poly is an implementation package, not a Core dependency.
- External AI produces validated definitions and is not a Core dependency.
- Preserve generated node hierarchy for editing and pivot animation.

## Unknowns And Confidence

- The initial API and Unity JSON serialization shape are implemented but remain pre-1.0 and may evolve.
- Motion clip serialization and additional asset-category templates remain design work.
- Supported Unity versions and public release license are not yet decided.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/Scenes/SampleScene.unity`

<!-- unity-onboarding:generated:end -->
