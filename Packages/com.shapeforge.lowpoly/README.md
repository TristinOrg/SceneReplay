# ShapeForge Low Poly

The first official ShapeForge implementation. It provides Low Poly geometry, palettes, styles, and reusable presets for characters, furniture, buildings, props, and other model categories through ShapeForge extension contracts.

Built-in Cube, Sphere, Cylinder, and Capsule shapes reuse cached Unity meshes and base materials without generating Collider components. Parameterized Wedge and Frustum shapes use flat-shaded procedural meshes; equivalent parameter sets share one cached Mesh. Profile Cage joins ordered sections whose closed profiles may differ at every depth, enabling asymmetric shells that Profile Loft cannot express. Bounded cached interpolation adds rounded continuity without expanding authoring JSON.

Use `LowPolyShapeCapabilityCatalog.Instance` to query all eleven supported geometry types without reflection. `TryGet` performs a cached exact-ID lookup. Call `CreateDocument` and serialize it through the Unity Adapter only when external tools need the complete machine-readable catalog.

Use `LowPolyModelGenerator` as the reusable runtime entry point for validated definitions or external ShapeForge JSON. Register style documents once, then reuse the same pipeline for subsequent models.

Applications can configure `ShapeValidationLimits` for untrusted documents and use `Regenerate` or `RegenerateJson` to replace an existing model only after its new hierarchy completes successfully.

For repeated models, call `ParseJson` once and pass the returned `ShapeDefinition` to `Generate`. `GenerateJson` is intended for one-off documents because it parses and validates on every call.

For a large number of instances, create a `LowPolyGenerationBatch` and call `GenerateNext` or `GenerateForMilliseconds` from an existing update or loading scheduler. The caller owns the count or elapsed-time budget; ShapeForge creates no coroutine, iterator state machine, global runner, or hidden update loop.

Use `ShapeForge > Generate` in the Unity Editor to preview the articulated Sentinel Robot, detailed Inventor Workbench, compact Japanese Town, and dense Shibuya Crossing presets with full Undo support. Shibuya combines diagonal scramble markings, four commercial towers, media screens, signals, signage, and a lightweight crossing crowd. Animated variants control cached `IShapeTransformTarget` instances without defining a ShapeForge animation format; a future motion system remains responsible for clips, tracks, interpolation, and serialization.

Preset menu commands persist their procedural meshes under `Assets/ShapeForge/Generated` before returning the hierarchy. Keep the generated mesh asset alongside any Prefab created from that hierarchy; built-in Unity primitive meshes are not duplicated.

Use `ShapeForge > Diagnostics > Benchmark JSON Generation` to measure the end-to-end JSON generation path, managed heap growth, and shared render resources on the current Editor machine. Treat results as a local baseline, not a cross-platform guarantee.
