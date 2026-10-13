# ShapeForge Unity Adapter

The Unity Adapter converts engine-agnostic ShapeForge definitions into Unity object hierarchies and provides the default JSON implementation.

Generated appearance uses shared cached materials for reusable palette colors and a `MaterialPropertyBlock` only for explicit per-shape overrides. A single root manifest restores all renderer state across Unity lifecycle events.

Every generated root includes a `UnityShapeModel` that resolves stable node IDs through the engine-neutral `IShapeTransformResolver` contract. Motion systems should resolve targets once and cache the returned `IShapeTransformTarget` instead of traversing the hierarchy each frame.

Use `UnityShapeModelGenerator.Prepare` when generating the same immutable definition repeatedly. Its `UnityShapeGenerationPlan` validates once and skips redundant tree validation for subsequent instances.

The generator accepts optional complexity limits and supports failure-safe regeneration. An existing hierarchy remains intact unless its replacement completes successfully.

`UnityHumanoidAvatarBuilder.CreateAvatar` creates a Unity Humanoid Avatar from a generated rigid-part hierarchy and a complete `ShapeHumanoidRig`. It validates every required semantic role, requires the canonical biped parent-child chain, enables Unity's default Human bone limits for muscle animation, and rejects invalid Unity Avatars. It does not require `SkinnedMeshRenderer`; smooth skinning remains a separate future concern.

Before saving an Editor-generated hierarchy as a Prefab, call `UnityGeneratedModelAssetStore.PersistMeshes`. It stores each unique transient mesh once in a shared asset container and rewires the hierarchy, so Prefab reloads retain procedural geometry without runtime reconstruction. Official Low Poly preset menu commands perform this step automatically.

`ShapeJsonSerializer.Serialize(ShapeCapabilityCatalogDocument)` exports any backend capability catalog as compact, versioned JSON for external authoring tools and LLM context.

The same serializer exports `ShapeTemplateCatalogDocument` discovery metadata. Template-specific specification serialization remains owned by the package that defines that specification.

Use `SerializeSpecification` and `DeserializeSpecification` for template-owned data at the Unity JSON boundary. Deserialization requires the owning package's validator callback, so external documents cannot bypass semantic validation.
