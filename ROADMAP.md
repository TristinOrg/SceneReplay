# SceneReplay Development Roadmap

This document is the repository-owned continuation point for maintainers and coding agents. Update it in the same commit that completes or materially changes a milestone.

The public execution board is [SceneReplay Roadmap](https://github.com/orgs/TristinOrg/projects/1). GitHub Issues are the task/status source of truth; this file preserves architectural context and acceptance criteria beside the code.

## Product direction

SceneReplay is an engine-neutral semantic intermediate layer for AI-authored 3D assets:

```text
AI / Authoring Tool / Existing Asset
                -> SceneReplay Semantic Definition
                -> Validate / Diff / Patch
                -> Engine Adapter / Portable Asset
```

SceneReplay owns stable semantic identity, editable structure, deterministic validation and patches, semantic completeness, and portable engine adaptation. Rendering, geometry authoring, and image-to-3D reconstruction belong to external engines and tools. Core and Schema must remain independent of render engines, concrete art styles, and AI providers.

## Resume instructions

Before changing code:

1. Read `AGENTS.md` and `C:/Users/Administrator/.codex/PREFERENCES.md`.
2. Read this roadmap and inspect the latest commits.
3. Preserve unrelated working-tree changes.
4. Extend existing package boundaries instead of creating parallel systems.
5. Complete one coherent stage, validate it, commit with Conventional Commits, and push.

Useful commands:

```text
python tools/scenereplay.py repository
python tools/scenereplay.py verify
git log --oneline -15
git status --short
```

`verify` requires a connected Unity MCP session. Document operations use the real C# implementation through the Editor automation bridge.

## Definition of done

Every milestone should include, where applicable:

- engine-neutral versioned contracts;
- deterministic validation with stable diagnostic codes and JSON-style paths;
- Core behavior and EditMode tests;
- Draft 2020-12 JSON Schema and minimal examples;
- Unity JSON/adapter integration without leaking Unity into Core;
- Python CLI orchestration for repeatable agent workflows;
- README and changelog updates;
- repository validation and relevant Unity tests;
- independently useful commits pushed to the current branch.

## Completed foundation

### Framework and generation foundation — Complete

- Schema, Core, Unity, and Low Poly package boundaries.
- Deterministic semantic definitions, styles, capabilities, templates, validation, and generation.
- Unity hierarchy adaptation, lifecycle-safe regeneration, cached materials, and Prefab-safe mesh storage.
- Semantic rigs, canonical Humanoid roles, runtime Avatar creation, and FBX Humanoid animation integration.

### LLM editing foundation — Complete

- Structured validation diagnostics — `56c82a3`.
- Deterministic semantic definition diffs — `3ed6c1e`.
- Atomic ShapePatch add/remove/move/update operations — `3a42cce`.
- Reversible ShapePatch transactions that produce inverse operations for deterministic undo.
- Patch JSON workflow — `960e53c`.
- Declarative Quality Gate and structural metrics — `c80c2d9`.
- Quality/Patch schemas and LLM correction loop — `3639fc6`.

### De-scoped image reconstruction work

Reference-image preprocessing, image comparison, inverse fitting, and reconstruction orchestration were removed. They expanded SceneReplay into image-to-3D tooling instead of strengthening its semantic interoperability layer. Do not restore these workflows without a demonstrated need that cannot be handled by an external authoring or reconstruction tool.

### Automation CLI — Implemented

- C# Editor bridge and Python document commands — `46fc66d`.
- Repository verification, Unity test orchestration, instance selection, reconnect handling, and Python tests — `9cd471d`.

Current validation note: on 2026-08-12 the connected Unity Editor completed all three first-party EditMode assemblies with 22/22 tests passing and no compilation errors. Python tests and repository validation also pass.

## Ordered milestones

### M2 — Construction Pass System

Status: Done — [#3](https://github.com/TristinOrg/SceneReplay/issues/3). Commits `fb66be1`, `1ccdc22`; live validation `792eb37`.

Goal: build and review assets in deterministic passes rather than one monolithic generation.

Initial passes:

```text
Structure -> Primary Forms -> Secondary Forms -> Details -> Appearance -> Gameplay Semantics -> Final Quality
```

Deliverables include versioned pass plans, dependencies, completion state, per-pass patches, quality policies, resumability, and reports.

### M3 — Game Semantic Metadata

Status: Done — [#4](https://github.com/TristinOrg/SceneReplay/issues/4). Commits `b28ce6e`, `784d7f0`, `cf75cb1`; live validation `792eb37`.

Goal: make generated output directly useful as a game asset rather than only a model.

Develop in this order:

1. generic semantic anchors and sockets;
2. hand grips, weapon sockets, mount points, and interaction points;
3. damage zones and gameplay tags;
4. Foot IK and grounding markers;
5. collider rules;
6. LOD rules.

Core owns engine-neutral contracts and validation. Engine packages compile them into native components and assets.

### M4 — Semantic Template Library

Status: Done — [#5](https://github.com/TristinOrg/SceneReplay/issues/5). Commits `12cb22d`, `0cc90ba`; live validation `792eb37`.

Expand reusable templates only after the metadata foundation exists:

1. hair;
2. clothing and armor;
3. weapons and props;
4. buildings;
5. vehicles.

Each library must publish discovery metadata, bounded parameters, detail inventories, quality policies, representative presets, and tests. Low Poly remains the first implementation, not the framework identity.

### M6 — MotionForge boundary integration

Status: Done — [#7](https://github.com/TristinOrg/SceneReplay/issues/7). Commit `56cd954`; live validation `792eb37`.

SceneReplay continues to own rest pose, stable nodes, semantic rig roles, constraints, and transform targets. MotionForge should own engine-neutral motion intent/IR, tracks, keyframes, curves, composition, and serialization. Native adapters own playback, blending, IK, retargeting, and optimization.

Do not place complete animation formats or Unity Animator ownership in SceneReplay Core.

### M7 — Engine adapters and export

Status: Portable export scope done; Unity is the first engine adapter. Cross-engine availability is a product goal. Adapter descriptor and hierarchy/transform conformance contract v1 are implemented in Core and Unity. Game-metadata capability and stable-node reference checks are part of the shared conformance validator, and Core verifies reversible semantic edits across authored-value and structural operations. Unity's native Prefab workflow now persists complete source metadata and supports compiling reversible semantic edits; EditMode coverage saves and reloads edited and restored Prefabs. Unity Prefab compilation and initial adapter conformance are implemented in `eb652b2` and `d431b7b`; self-contained GLB export in `94cd8b9`; the licensed external FBX/USD tool boundary in `c3863b3`; GLB validation and auditable converter profiles in `cc902cc` and `d9a7ae4`.

Develop in this order:

1. Three.js adapter first slice and a standalone consuming project are in the repository; extend shape and metadata coverage, then add Godot and Unreal adapters against real consuming projects;
2. publish adapter setup guides and conformance fixtures for additional engine teams;
3. validate each engine's metadata and reversible edits through its native asset workflow;
4. export geometry-preserving GLB with stable semantic node metadata;
5. delegate FBX/USD conversion through an explicit, verified external toolchain.

### M8 — SceneReplay Agent and MCP

Status: Done — [#9](https://github.com/TristinOrg/SceneReplay/issues/9). Commit `a29d133` plus the earlier automation commits; live MCP validation `792eb37`.

Expose stable workflows only after their local APIs and Python orchestration are mature:

- capability and template discovery;
- validate, inventory, diff, patch, and quality operations;
- staged build execution and resumability;
- concise machine-readable results with bounded output.

MCP must remain a thin orchestration surface, not a second implementation of SceneReplay rules.

## Explicit non-goals

- Competing with image-to-3D systems on raw reconstruction fidelity.
- Ingesting reference images or fitting geometry to reference renders.
- Owning rendering, camera, or visual-comparison systems supplied by target engines.
- Moving provider SDKs into Schema or Core.
- Treating Low Poly or Unity as the SceneReplay framework identity.
- Reimplementing C# validation, Patch, Diff, or Quality Gate logic in Python or MCP.
- Adding primitives without a demonstrated semantic/template requirement.
- Starting multi-engine adapters before the conformance contracts are stable.

## Immediate continuation

1. build adapters for Three.js, Godot, and Unreal against consuming projects;
2. validate metadata and reversible edits through each engine's native asset workflow;
3. run `validate-glb` with the Khronos validator and real consuming pipelines; local structural and external-command validation landed in `cc902cc`;
4. create a pinned converter profile from `Docs/converter-profile.example.json` before publishing FBX/USD artifacts; profile enforcement and reporting landed in `d9a7ae4`.
