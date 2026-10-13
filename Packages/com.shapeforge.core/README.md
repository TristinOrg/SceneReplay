# ShapeForge Core

ShapeForge Core defines style-independent shape data, palettes, style resolution, and generation contracts. It must not depend on Low Poly implementations, render pipelines, or external AI providers.

`ShapeDefinitionValidator` applies configurable `ShapeValidationLimits` for node count, hierarchy depth, and aggregate authored points before a backend allocates engine objects or procedural meshes.

Styles may set `BaseStyle` to inherit another registered style. Derived palette roles override inherited roles; unresolved roles fall back through the inheritance chain. `ShapeStyleResolver` validates missing parents and cycles when styles are registered, then caches flattened palettes for constant-time generation lookups.

Geometry implementations describe their authoring surface through `IShapeCapabilityCatalog`. The immutable capability contracts cover intended uses, limitations, required profile/path/section counts, numeric parameter ranges, and cost scaling without referencing an engine or concrete style. `CoreShapeCapabilityCatalog` describes the framework-owned `core/group` type.

Semantic packages implement `ShapeTemplate<TSpecification>` to compile domain-readable input into a standard `ShapeDefinition`. `ShapeTemplateCatalog` caches exact-ID compiler lookup, while versioned descriptor documents expose the specification schema, category, tags, and required shape types without serializing compiler instances.

`ShapeHumanoidRig` defines and validates the canonical role set for a full biped humanoid skeleton. It remains engine-neutral: the authored ShapeNode hierarchy provides the rest pose, while each engine adapter validates and maps the resulting transforms to its native avatar system.
