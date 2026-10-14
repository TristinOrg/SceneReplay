# SceneReplay Technical Reference

This document contains the detailed API, contract, validation, geometry, performance, and workflow reference. For the project overview and getting started, see the [main README](../README.md).

## Release status

Version `0.1.0` is the first framework release. It establishes the public package boundaries, semantic Shape and Style JSON contracts, stable IDs, deterministic validation and Patch/Diff, Unity adapter, Low Poly implementation, portable GLB export, and representative presets.

SceneReplay is intended for tooling integrations and content pipelines that need assets to remain understandable and editable as semantic parts instead of becoming opaque meshes. The API is pre-1.0 and may change while the framework boundary and integrations are established.

## Core ideas

- **Shape is the smallest semantic asset concept.** Cubes, capsules, profiles, sweeps, cages, and future custom types use the same extensible node contract.
- **Models are editable assemblies.** Stable IDs and nested transforms produce a native hierarchy instead of an opaque combined mesh.
- **Style is separate from geometry.** Palette roles and inherited styles allow the same definition to be rendered with different visual treatments.
- **External AI is an author, not a dependency.** LLMs produce validated SceneReplay JSON; no AI provider is referenced by Schema or Core.
- **SceneReplay is the semantic middle layer.** It owns portable asset meaning, identity, structure, edits, validation, and adapter contracts. The engine owns runtime scene behavior and rendering.
- **Motion is a separate responsibility.** SceneReplay exposes semantic pivots and transform targets so MotionForge or another animation system can bind without owning model generation.
- **Low Poly is an implementation.** Developers can add realistic, voxel, SDF, custom-mesh, or engine-specific generators without changing Core.
- **Performance is explicit.** Meshes and materials are cached, repeated definitions can be prepared once, and large batches remain caller-budgeted.

## Architecture

```mermaid
flowchart LR
    A["Code / JSON / Editor / External LLM"] --> S["SceneReplay Schema"]
    S --> C["SceneReplay Core"]
    C --> U["Engine Adapter"]
    C --> G["Geometry / Style Backend"]
    U --> H["Native editable hierarchy"]
    G --> H
    H --> M["Motion system via stable targets"]
```

The Unity integration contains four Unity Package Manager packages:

| Package | Responsibility |
|---|---|
| `com.scenereplay.schema` | Serializable, engine-neutral semantic shape, style, patch, and rig contracts |
| `com.scenereplay.core` | Validation, semantic editing, adapter capability descriptors and conformance checks, styles, templates, and rig lookup |
| `com.scenereplay.unity` | JSON serialization, Unity hierarchy adaptation, appearance resources, bindings, plans, and regeneration |
| `com.scenereplay.lowpoly` | Official Low Poly geometry backend, caches, templates, presets, batches, and Editor tools |

Dependency direction is one-way:

```text
Schema <- Core <- Unity Adapter <- Low Poly
```

The independent JavaScript adapter is in `Adapters/three` and is consumed by the runnable project in `Samples/three-consumer`. It supports stable node IDs, hierarchy, local transforms, semantic rig targets, game-metadata preservation, atomic semantic patches, and all eleven Low Poly geometry types. The sample includes character, weapon, building, profile geometry, and semantic rig examples plus live patch and rig-target demonstrations. Mirror transforms and engine-native collider/LOD behavior remain adapter gaps.

The GDScript adapter is in `Adapters/godot` and has a standalone project in `Samples/godot-consumer`. It compiles stable IDs, hierarchy, local transforms, direct palette/color overrides, semantic rig roles and rotation limits, game-metadata bindings, and all eleven Low Poly types into Godot `Node3D` and `MeshInstance3D` objects. It applies atomic semantic patches and preserves compatible native node identities. An opt-in compiles declared colliders into Godot physics nodes. The sample switches between a collider-enabled character, a five-shape profile gallery, a patch demonstration, and the semantic rig fixture. Patch rollback and native LOD switching remain adapter gaps.

| Adapter | Current engine support | Native semantic output |
|---|---|---|
| Unity | All eleven Low Poly types, stable-ID hierarchy, reversible edits, semantic rigs | Colliders, LOD groups, rig bindings, Prefab workflow |
| Three.js | All eleven Low Poly types, stable-ID hierarchy, semantic rig targets, atomic add/remove/move/update patches | Scene metadata; rendering and physics stay with the host application |
| Godot | All eleven Low Poly types, stable-ID hierarchy, semantic rig targets, atomic add/remove/move/update patches | Optional box, sphere, capsule, and convex mesh colliders; LOD metadata stays descriptive |
| Unreal Engine | Planned | No adapter implementation yet |

The same rig fixture lives in `Samples/conformance/rigged-character.json` and is mirrored byte-for-byte into both runnable consumers. This gives the adapters a shared input for checking role-to-node resolution and retained rotation constraints.

Schema and Core have no `UnityEngine`, render-pipeline, concrete-style, or AI-provider dependency.

Engine adapters publish a `scenereplay.adapter/1.0` descriptor listing their semantic features and unsupported-feature policy. Contract v1 requires stable node IDs, hierarchy, and local transforms. `ShapeAdapterConformanceValidator` checks those promises against a compiled hierarchy, including identity, parent relationships, transform values, and writable transform restoration. Unity passes the shared C# conformance validator; the JavaScript Three.js and GDScript Godot adapters have engine-native headless test suites for their published behavior.

## Requirements

- Unity `2022.3` LTS or newer compatible 2022.3 release
- Newtonsoft JSON for the Unity adapter (`com.unity.nuget.newtonsoft-json` `3.2.1`)
- Unity Test Framework only when running the included Editor tests
- Node.js 20.19+ or 22.12+ to run the Three.js example and adapter tests
- Godot 4.x to open the Godot adapter example and run its headless test suite

The reference project currently uses Unity `2022.3.62f3`.

## Installation

For the complete Low Poly stack, add all four packages to the consuming project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.scenereplay.schema": "https://github.com/TristinOrg/SceneReplay.git?path=Packages/com.scenereplay.schema#v0.1.0",
    "com.scenereplay.core": "https://github.com/TristinOrg/SceneReplay.git?path=Packages/com.scenereplay.core#v0.1.0",
    "com.scenereplay.unity": "https://github.com/TristinOrg/SceneReplay.git?path=Packages/com.scenereplay.unity#v0.1.0",
    "com.scenereplay.lowpoly": "https://github.com/TristinOrg/SceneReplay.git?path=Packages/com.scenereplay.lowpoly#v0.1.0"
  }
}
```

Pin production projects to a release tag. Use `main` only when intentionally testing unreleased changes.

### Three.js example

The standalone example runs outside Unity. From `Samples/three-consumer`:

```sh
npm ci
npm test
npm run dev
```

Open the local URL printed by Vite. The sample source and additional JSON definitions are in [Samples/three-consumer](../Samples/three-consumer). For Godot, open [Samples/godot-consumer/project.godot](../Samples/godot-consumer/project.godot) in Godot 4, or run `godot --headless --path Samples/godot-consumer --script res://tests/run_tests.gd`. Unreal integration remains planned; see [ROADMAP.md](../ROADMAP.md) for the adapter sequence.

## Quick start

Generate an official preset through the reusable runtime pipeline:

```csharp
using SceneReplay.LowPoly;
using SceneReplay;
using UnityEngine;

ShapeDefinition      definition = LowPolyRobotPreset.CreateDefinition();
ShapeStyleDefinition style      = LowPolyRobotPreset.CreateStyle();
LowPolyModelGenerator generator = new(new[] { style });

GameObject robot = generator.Generate(definition);
```

Every generated root contains a `UnityShapeModel`. Resolve stable transform targets once and cache them:

```csharp
using SceneReplay.Unity;

UnityShapeModel model = robot.GetComponent<UnityShapeModel>();
if (model.TryGetTarget("robot.head.pivot", out IShapeTransformTarget head))
    head.LocalEulerAngles = new(0f, 25f, 0f);
```

## Fluent shape authoring

`ShapeBuilder` creates engine-neutral definitions without Unity types:

```csharp
ShapeDefinition table = ShapeBuilder
    .Create("Simple Table")
    .WithStyle("lowpoly/furniture")
    .Root("table", "Table", root => root
        .Shape("table.top", "Table Top", LowPolyShapeTypes.Cube, top => top
            .Position(0f, 0.9f, 0f)
            .Scale(2f, 0.15f, 1.1f)
            .ColorRole("wood"))
        .Shape("table.leg.left", "Left Leg", LowPolyShapeTypes.Cube, leg => leg
            .Position(-0.75f, 0.42f, 0f)
            .Scale(0.18f, 0.85f, 0.18f)
            .ColorRole("wood.dark")
            .Mirror(ShapeMirrorAxis.X)))
    .Build();
```

Stable node IDs are part of the data contract. They support regeneration, diagnostics, Editor inspection, and motion binding independently of generated GameObject names.

## Supported Low Poly geometry

The official backend publishes eleven discoverable capabilities:

- Cube
- Sphere
- Cylinder
- Capsule
- Wedge
- Frustum
- Extruded Profile
- Profile Loft
- Profile Cage
- Lathe Profile
- Profile Sweep

Simple primitives reuse Unity template meshes. Procedural shapes use deterministic cache keys, so equivalent geometry shares a generated `Mesh` rather than rebuilding it per instance.

`LowPolyShapeCapabilityCatalog.Instance` exposes machine-readable descriptions, required inputs, limitations, parameter ranges, and generation-cost categories. External tools should query this catalog rather than assume supported shape types.

## Profiles and complex forms

SceneReplay moves beyond primitive block assembly through several readable geometry forms:

- **Extruded Profile** turns a normalized 2D silhouette into a volume.
- **Profile Loft** varies scale and offset across ordered depth sections.
- **Profile Cage** allows every depth section to carry an independent outline.
- **Lathe Profile** revolves an ordered radius/height profile.
- **Profile Sweep** moves a profile along a 3D path.
- **Smoothing and bevel controls** add bounded geometric continuity while retaining deterministic generation.

These forms support stylized heads, hair volumes, clothing, boots, furniture, signs, building facades, vehicles, weapons, and other shapes that primitives alone cannot express cleanly.

## Styles and appearance

Geometry refers to semantic palette roles such as `metal.dark`, `skin`, `glass`, or `screen.blue`. `ShapeStyleResolver` validates style inheritance, rejects missing parents and cycles, flattens inherited palettes, and caches role lookup.

The Unity adapter uses:

- shared cached materials for reusable palette colors;
- `MaterialPropertyBlock` only for explicit per-shape overrides;
- one root appearance manifest to restore renderer state safely across lifecycle transitions.

This avoids creating a unique material instance for every generated renderer.

## JSON and external LLM workflow

The Unity adapter serializes and validates SceneReplay documents through `ShapeJsonSerializer`:

```csharp
ShapeJsonSerializer serializer = new();
string              json       = serializer.Serialize(definition);
ShapeDefinition     restored   = serializer.DeserializeShape(json);
```

For iterative LLM edits, exchange a small `scenereplay.patch/1.0` document instead of regenerating the complete model. Patches address stable node IDs and support add, remove, move/reorder, and authored-value update operations:

```csharp
ShapePatchDocument patch   = serializer.DeserializePatch(patchJson);
ShapePatchResult   result  = new ShapePatchApplier().TryApply(restored, patch);

if (result.Succeeded)
    restored = result.Definition;
else
    Report(result.Diagnostics);
```

`ShapePatchApplier` works on a deep copy and publishes the result only after final validation succeeds. The source definition therefore remains unchanged after an invalid operation or invalid final model. For reversible edits, `ApplyWithInverse` returns the validated definition together with a reverse patch that can be stored for review or applied to undo the edit:

```csharp
ShapePatchTransaction transaction = new ShapePatchApplier().ApplyWithInverse(restored, patch);
ShapeDefinition       edited      = transaction.Definition;
ShapeDefinition       restoredAgain = new ShapePatchApplier().Apply(edited, transaction.InversePatch);
```

`ShapeDefinitionValidator.Analyze` provides the same structured diagnostic format for complete documents, while `ShapeDefinitionDiffer.Compare` provides deterministic semantic changes for review, tests, and tooling.

Use a versioned `scenereplay.quality/1.0` policy as the acceptance boundary for a game-ready asset. A policy can require stable nodes, shape capabilities, a rig type, semantic rig roles, and structural complexity limits:

```csharp
ShapeQualityPolicy policy = serializer.DeserializeQualityPolicy(policyJson);
ShapeQualityReport quality = new ShapeQualityGate().Evaluate(restored, policy);

if (!quality.Passed)
    RequestPatch(quality.Diagnostics, quality.Metrics);
```

This forms a deterministic authoring loop:

```text
Definition -> Quality Gate -> diagnostics -> LLM-authored Patch
     ^                                      |
     +------------- atomic apply <----------+
```

Quality Gate checks structural and semantic completeness. Rendering and visual review belong to the consuming engine or authoring tool.

Published Draft 2020-12 contracts live in:

- `Packages/com.scenereplay.schema/Documentation~/Schemas`
- `Packages/com.scenereplay.lowpoly/Documentation~/Templates`

A recommended external-authoring flow is:

```text
Capability catalog + template descriptor + JSON Schema
                         ↓
                  External LLM/tool
                         ↓
                SceneReplay JSON document
                         ↓
            Parse → validate → generate safely
```

External models should output semantic node IDs, meaningful part names, palette roles, bounded parameters, and supported shape types. They should not emit raw mesh vertex arrays when a SceneReplay capability can describe the same intent.

SceneReplay contains no OpenAI, Gemini, or other provider client. Applications own provider selection, credentials, prompting, retries, and content policy.

## Automation CLI

`tools/scenereplay.py` keeps orchestration in Python while executing model rules through the authoritative SceneReplay C# assemblies inside Unity:

```text
python tools/scenereplay.py validate model.json
python tools/scenereplay.py diff before.json after.json
python tools/scenereplay.py patch model.json change.json -o updated.json
python tools/scenereplay.py quality model.json policy.json
python tools/scenereplay.py inventory model.json inventory.json
python tools/scenereplay.py export-glb model.json --asset Library/SceneReplay/model.glb -o export-report.json
python tools/scenereplay.py validate-glb Library/SceneReplay/model.glb -o glb-validation.json
python tools/scenereplay.py validate-glb Library/SceneReplay/model.glb --validator gltf-validator -i {input}
python tools/scenereplay.py export-external Library/SceneReplay/model.glb --asset Exports/model.fbx --converter blender --background --python convert.py -- {input} {output}
python tools/scenereplay.py export-external Library/SceneReplay/model.glb --asset Exports/model.fbx --profile Docs/converter-profile.json
python tools/scenereplay.py plan construction-plan.json
python tools/scenereplay.py step model.json construction-plan.json --pass structure -o result.json
python tools/scenereplay.py game model.json game-metadata.json
python tools/scenereplay.py discover -o catalogs.json
python tools/scenereplay.py repository
python tools/scenereplay.py verify
```

GLB is the portable, self-contained SceneReplay interchange output. It preserves geometry, hierarchy,
materials, transforms, and stable node IDs. FBX and USD-family files are deliberately delegated to an
explicitly configured external converter: SceneReplay passes literal arguments without a shell and
accepts success only when a non-empty target asset is created. This keeps third-party SDKs, licenses,
and tool versions outside Core. For publishable artifacts, copy `Docs/converter-profile.example.json`,
pin the converter ID, version, license, supported formats, and literal command, then use `--profile` so
the export report records the toolchain identity. The inline example command is a contract illustration; the converter script
and supported Blender version belong to the consuming pipeline.

Document operations require the Unity MCP server. `verify` refreshes assets and runs the three SceneReplay EditMode test assemblies while excluding Unity package tests. It rejects zero-test runs, survives temporary MCP reconnects, and automatically selects the connected SceneReplay Editor when multiple Unity projects are open. Use `--instance Name@hash` only when automatic selection is ambiguous.

## Semantic interoperability boundary

The responsibility boundary is:

| Concern | Owner |
|---|---|
| Portable asset definition, part meaning, stable IDs, validation, and structural edits | SceneReplay Schema/Core |
| Procedural geometry implementation and engine-native hierarchy creation | Geometry backend and engine adapter |
| Scene execution, rendering, camera, and application interaction | Host engine and application (for example, Three.js) |
| Initial modeling or image-to-3D authoring | External authoring tools or AI providers |

Three.js provides a scene and rendering runtime. SceneReplay addresses a different layer: a portable, semantically editable definition that engine adapters map into host-native hierarchies while retaining SceneReplay IDs and metadata. Unity, Three.js, and Godot integrations are implemented; Unreal remains planned. Each adapter must preserve the shared semantic contract rather than redefine it. SceneReplay itself does not render scenes, model arbitrary geometry, or reconstruct assets from reference images.

Game Semantic Metadata is also portable. A `scenereplay.game-metadata/1.0` document binds sockets, grips, interaction and IK anchors, damage zones, collider rules, LOD membership, and gameplay tags to stable node IDs. Core validates those bindings; the Unity adapter compiles them into child transforms, native colliders, `LODGroup`, and a cached runtime manifest. This keeps gameplay intent readable and editable without placing Unity types in Core.

The official Low Poly semantic library covers the ordered Hair, Armor, Weapon, Building, and Vehicle categories. Catalog descriptors expose bounded dimensions for tool discovery. Each template compiles stable node IDs and can publish a matching Detail Inventory and Quality Policy; versioned JSON Schema and representative specifications live under `Packages/com.scenereplay.lowpoly/Documentation~/Templates`.

### MotionForge boundary

SceneReplay owns stable node IDs, semantic rig roles, authored local rest poses, joint rotation limits, and writable transform targets. `ShapeMotionBindingResolver` validates those contracts once and caches role-to-target bindings for an external motion system. It can clamp offsets and restore the authored rest pose without knowing about clips.

MotionForge should own motion intent, clips, tracks, keyframes, curves, interpolation, composition, and serialization. Engine adapters should own playback, blending, IK, retargeting, and runtime optimization. None of those concerns belong in SceneReplay Core.

Construction Plans organize generation into resumable dependency-ordered passes: Structure, Primary Forms, Secondary Forms, Details, Appearance, Gameplay Semantics, and Final Quality. Each pass owns an atomic ShapePatch and an optional post-pass quality-policy ID. The evaluator derives ready and blocked passes; the executor applies one ready patch to cloned state and advances the plan only on success.

## Semantic templates

`ShapeTemplate<TSpecification>` compiles domain-readable specifications into ordinary `ShapeDefinition` documents. `ShapeTemplateCatalog` exposes versioned descriptors with category, tags, required shape capabilities, and specification schema IDs.

Templates are optional convenience layers. Raw Shape Definitions remain the universal interchange format for characters, furniture, buildings, robots, environments, and future categories.

## Rig and MotionForge integration

SceneReplay does not define a complete animation format. It provides the structural contract a dedicated motion system needs:

- hierarchical group and pivot nodes;
- stable semantic node IDs;
- optional semantic rig roles, including the canonical biped Humanoid roles;
- authored local transforms as the model's rest pose;
- joint rotation constraints;
- `IShapeTransformResolver` and writable `IShapeTransformTarget` interfaces;
- cached Unity bindings through `UnityShapeModel`.

A complete biped semantic rig can be validated through `ShapeHumanoidRig`. The Unity Adapter can then create a valid `Avatar` for a generated rigid-part hierarchy with `UnityHumanoidAvatarBuilder.CreateAvatar`. This is optional: existing articulated presets remain semantic Pivot rigs until they provide every required role and the canonical bone hierarchy.

MotionForge should be an LLM-friendly, engine-neutral motion layer rather than a replacement for Unity Animator, Godot AnimationTree, or Unreal Animation Blueprint. It should own structured motion intent, semantic composition, constraints, a versioned Motion IR, deterministic compilation, validation, and serialization. Engine adapters should translate that IR into native clips or runtime pose targets and delegate native playback, blending, IK, retargeting, and optimization to the host engine.

```text
LLM / user intent
    -> MotionForge intent
    -> MotionForge IR
    -> engine adapter
    -> native animation system
```

The Low Poly package includes small transform-based animation examples for the robot, workbench, and hero. They use one centralized player, cache all targets, allocate no per-part behaviours, and serve only as integration demonstrations.

## Runtime generation and regeneration

`LowPolyModelGenerator` supports:

- validated definitions;
- one-off external JSON generation;
- parse-once repeated generation;
- reusable prepared plans;
- caller-budgeted batches;
- failure-safe model replacement.

For repeated instances:

```csharp
ShapeDefinition definition = generator.ParseJson(json);
LowPolyGenerationBatch batch = generator.CreateBatch(
    definition,
    totalCount: 200,
    parent: container,
    onGenerated: instance => instance.SetActive(false));

// Called by the application's existing loading scheduler.
batch.GenerateNext(modelBudget: 8);
```

SceneReplay deliberately creates no hidden coroutine, global runner, background task, or per-model `Update`. The caller owns scheduling and frame budgets.

Safe regeneration builds the replacement first:

```csharp
UnityShapeModel existing = current.GetComponent<UnityShapeModel>();
current = generator.RegenerateJson(existing, updatedJson);
```

If parsing, validation, mesh generation, or appearance setup fails, the existing model remains intact.

## Validation and safety limits

Definitions are validated before a backend allocates GameObjects or meshes. Validation covers:

- schema versions;
- stable and unique node IDs;
- required transforms, appearances, and collections;
- finite transforms, parameters, profiles, paths, and cage points;
- palette color ranges;
- mirror constraints;
- rig roles, targets, and joint limits;
- configurable complexity budgets.

Default `ShapeValidationLimits` allow at most:

- 4,096 authored nodes;
- 64 hierarchy levels;
- 262,144 combined profile, path, section, and cage points.

Applications accepting untrusted external JSON can provide stricter limits to `LowPolyModelGenerator` or `UnityShapeModelGenerator`.

## Performance model

SceneReplay optimizes generation and steady-state use separately:

- built-in primitive meshes are reused;
- procedural meshes use bounded deterministic caches;
- palette colors use shared materials;
- explicit overrides use property blocks;
- style inheritance is flattened and cached;
- capability and template catalogs use cached exact-ID lookup;
- prepared plans validate immutable definitions once;
- batches use caller-owned count or time budgets;
- transform targets are resolved once and cached;
- generated models have no default per-object scripts or colliders.

Complex procedural shapes cost more during first generation than primitives. Once generated, they are ordinary Unity meshes and renderers. Actual frame cost depends on renderer count, triangle count, lighting, shadows, material variety, culling, and the consuming project's render pipeline.

Use `SceneReplay > Diagnostics > Benchmark JSON Generation` for a local Editor baseline. It reports parsing and prepared-generation timing, managed heap growth, and shared resource counts. Results are diagnostic evidence for the current machine, not cross-platform performance guarantees.

## Included presets

Use `SceneReplay > Generate` in the Unity Editor. Commands are Undo-aware and select the generated root.

- **Inventor Workbench** — layered furniture, drawers, lamp, tools, storage, and props.
- **Sentinel Robot** — articulated mechanical hierarchy with semantic pivots.
- **Japanese Town** — traditional buildings, market furniture, shrine pieces, lanterns, and vegetation.
- **Shibuya Crossing** — 189 nodes and 162 renderers forming a modern scramble crossing, four media buildings, signals, signage, and a lightweight crowd.
- **Animated examples** — centralized transform motion for the workbench, robot, and hero.

Presets are examples of the same public API available to applications; they are not special generator code paths.

## Extending SceneReplay

To add a new visual backend:

1. Define stable shape type IDs outside Core.
2. Publish their capabilities through `IShapeCapabilityCatalog`.
3. Implement the engine-side generator interface.
4. Validate parameters before allocating geometry.
5. Cache equivalent immutable resources.
6. Keep style resolution and appearance ownership explicit.
7. Add generation and contract tests.

To port SceneReplay to another engine, reuse the Schema/Core concepts and implement equivalents for hierarchy creation, appearance, resource caching, serialization, and transform target resolution.

## Tests and continuous validation

First-party EditMode tests cover:

- fluent authoring and validation;
- JSON contracts and published files;
- style inheritance and appearance lifecycle;
- capabilities and semantic templates;
- semantic detail coverage and construction passes;
- procedural mesh generation and caches;
- hierarchy bindings and mirroring;
- generation plans, batches, and safe regeneration;
- official presets and motion-ready pivots.

GitHub Actions runs a Unity-independent release-contract check on every push and pull request. It validates package versions and dependencies, JSON syntax, release-pinned Schema IDs, required `.meta` files, and the Schema/Core engine boundary.

Unity compilation and EditMode tests should also be run in Unity `2022.3` before publishing a release.

## Scope and limitations

SceneReplay is not intended to be:

- a replacement for Blender, Maya, Houdini, or ZBrush;
- a general vertex sculpting editor;
- an FBX/OBJ importer;
- an automatic texture or UV-authoring system;
- a skinned-mesh deformation pipeline;
- an AI-provider SDK;
- a complete animation state machine or timeline system.

It works best when an asset can be expressed as meaningful parts, readable dimensions, controlled procedural forms, palette roles, and editable transform relationships.

## Repository documents

- [Schema specification](../Packages/com.scenereplay.schema/Documentation~/SceneReplay-Specification.md)
- [Project architecture context](AI/UnityProjectContext.md)
- [Changelog](../CHANGELOG.md)
- [Contributing](../CONTRIBUTING.md)
- [Security policy](../SECURITY.md)

## License

SceneReplay uses a dual-license model:

- **AGPL-3.0-or-later** for open-source use under the GNU Affero General Public License;
- **Commercial license** for proprietary products and organizations that do not want AGPL obligations.

Generated models are not covered merely because they were produced by an unmodified SceneReplay distribution. See [LICENSE.md](../LICENSE.md) and [commercial licensing](../COMMERCIAL-LICENSE.md). Commercial inquiries: `Tristin_Wen@outlook.com`.

## Project history

SceneReplay was created by Tristin Wen as a practical response to the modeling barrier faced by independent game developers: provide a readable procedural language that humans and external AI tools can both understand, validate, modify, and regenerate.
