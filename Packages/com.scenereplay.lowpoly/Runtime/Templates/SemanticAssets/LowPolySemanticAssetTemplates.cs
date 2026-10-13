using System;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Provides shared compilation and quality resources for bounded Low Poly asset templates.
    /// </summary>
    public abstract class LowPolySemanticAssetTemplate : ShapeTemplate<LowPolySemanticAssetSpecification>
    {
        /// <inheritdoc />
        public override ShapeDefinition Compile(LowPolySemanticAssetSpecification specification)
        {
            Validate(specification);
            ShapeNode root = new(RootId, specification.Name, ShapeTypes.Group);
            Build(root, specification);
            return new(specification.Name, root);
        }

        /// <summary>Creates the required semantic-detail inventory for this template.</summary>
        public ShapeDetailInventory CreateDetailInventory(LowPolySemanticAssetSpecification specification)
        {
            ShapeDefinition definition = Compile(specification);
            ShapeDetailInventory inventory = new() { Subject = Descriptor.Id };
            foreach (ShapeNode child in definition.Root.Children)
            {
                inventory.Details.Add(new()
                {
                    Id           = child.Id,
                    Name         = child.Name,
                    Category     = Descriptor.Category,
                    TargetNodeId = child.Id
                });
            }
            return inventory;
        }

        /// <summary>Creates a compact structural quality policy for this template.</summary>
        public ShapeQualityPolicy CreateQualityPolicy(LowPolySemanticAssetSpecification specification)
        {
            ShapeDefinition definition = Compile(specification);
            ShapeQualityPolicy policy = new()
            {
                Id                    = $"{Descriptor.Id}/quality",
                MaximumNodeCount      = definition.Root.Children.Count + 1,
                MaximumHierarchyDepth = 2
            };
            policy.RequiredNodeIds.Add(RootId);
            foreach (ShapeNode child in definition.Root.Children)
                policy.RequiredNodeIds.Add(child.Id);
            return policy;
        }

        /// <summary>Gets the stable root node identifier.</summary>
        protected abstract string RootId { get; }
        /// <summary>Builds category-specific stable parts.</summary>
        protected abstract void Build(ShapeNode root, LowPolySemanticAssetSpecification specification);

        /// <summary>Creates one positioned and scaled primitive part.</summary>
        protected static ShapeNode Part(
            string id, string name, string type, ForgeVector3 position, ForgeVector3 scale)
        {
            ShapeNode node = new(id, name, type);
            node.Transform.Position = position;
            node.Transform.Scale    = scale;
            return node;
        }

        private static void Validate(LowPolySemanticAssetSpecification specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));
            if (specification.Schema != LowPolySemanticAssetSpecification.CurrentSchema)
                throw new ShapeValidationException($"Unsupported semantic-asset schema '{specification.Schema}'.");
            if (string.IsNullOrWhiteSpace(specification.Name))
                throw new ShapeValidationException("A semantic asset requires a name.");
            ValidateRange(specification.Width, nameof(specification.Width));
            ValidateRange(specification.Height, nameof(specification.Height));
            ValidateRange(specification.Depth, nameof(specification.Depth));
            ValidateRange(specification.DetailScale, nameof(specification.DetailScale));
        }

        private static void ValidateRange(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.1f || value > 10f)
                throw new ShapeValidationException($"{name} must be between 0.1 and 10.");
        }

        /// <summary>Creates standard bounded dimension discovery controls.</summary>
        protected static ShapeTemplateParameterDescriptor[] Dimensions() => new[]
        {
            new ShapeTemplateParameterDescriptor("width", "Overall width.", 1f, 0.1f, 10f),
            new ShapeTemplateParameterDescriptor("height", "Overall height.", 1f, 0.1f, 10f),
            new ShapeTemplateParameterDescriptor("depth", "Overall depth.", 1f, 0.1f, 10f),
            new ShapeTemplateParameterDescriptor("detailScale", "Secondary-detail scale.", 1f, 0.1f, 10f)
        };
    }

    /// <summary>Builds a reusable hair silhouette with stable cap and fringe parts.</summary>
    public sealed class LowPolyHairTemplate : LowPolySemanticAssetTemplate
    {
        private static readonly ShapeTemplateDescriptor Metadata = Describe(
            "lowpoly/hair/1.0", "hair", "Reusable cap, fringe, and side locks.");
        /// <summary>Gets the shared template.</summary>
        public static LowPolyHairTemplate Instance { get; } = new();
        /// <inheritdoc />
        public override ShapeTemplateDescriptor Descriptor => Metadata;
        /// <inheritdoc />
        protected override string RootId => "hair";
        /// <inheritdoc />
        protected override void Build(ShapeNode root, LowPolySemanticAssetSpecification s)
        {
            root.Add(Part("hair.cap", "Hair Cap", LowPolyShapeTypes.Sphere, new(0f, 0f, 0f), new(s.Width, s.Height, s.Depth)));
            root.Add(Part("hair.fringe", "Fringe", LowPolyShapeTypes.Wedge, new(0f, -0.35f * s.Height, -0.45f * s.Depth), new(s.Width, 0.45f * s.Height * s.DetailScale, 0.2f * s.Depth)));
            root.Add(Part("hair.side-left", "Left Side Lock", LowPolyShapeTypes.Capsule, new(-0.45f * s.Width, -0.4f * s.Height, 0f), new(0.18f * s.Width, 0.6f * s.Height * s.DetailScale, 0.18f * s.Depth)));
            root.Add(Part("hair.side-right", "Right Side Lock", LowPolyShapeTypes.Capsule, new(0.45f * s.Width, -0.4f * s.Height, 0f), new(0.18f * s.Width, 0.6f * s.Height * s.DetailScale, 0.18f * s.Depth)));
        }
        private static ShapeTemplateDescriptor Describe(string id, string category, string summary) =>
            new(id, summary, category, LowPolySemanticAssetSpecification.CurrentSchema,
                new[] { LowPolyShapeTypes.Sphere, LowPolyShapeTypes.Wedge, LowPolyShapeTypes.Capsule }, Dimensions(), category);
    }

    /// <summary>Builds reusable torso armor with stable plate and shoulder parts.</summary>
    public sealed class LowPolyArmorTemplate : LowPolySemanticAssetTemplate
    {
        private static readonly ShapeTemplateDescriptor Metadata = Describe("lowpoly/armor/1.0", "armor", "Torso plate and paired pauldrons.");
        /// <summary>Gets the shared template.</summary>
        public static LowPolyArmorTemplate Instance { get; } = new();
        /// <inheritdoc />
        public override ShapeTemplateDescriptor Descriptor => Metadata;
        /// <inheritdoc />
        protected override string RootId => "armor";
        /// <inheritdoc />
        protected override void Build(ShapeNode root, LowPolySemanticAssetSpecification s)
        {
            root.Add(Part("armor.chest", "Chest Plate", LowPolyShapeTypes.Frustum, new(0f, 0f, 0f), new(s.Width, s.Height, s.Depth)));
            root.Add(Part("armor.shoulder-left", "Left Pauldron", LowPolyShapeTypes.Sphere, new(-0.65f * s.Width, 0.35f * s.Height, 0f), new(0.35f * s.DetailScale, 0.25f * s.DetailScale, 0.4f * s.Depth)));
            root.Add(Part("armor.shoulder-right", "Right Pauldron", LowPolyShapeTypes.Sphere, new(0.65f * s.Width, 0.35f * s.Height, 0f), new(0.35f * s.DetailScale, 0.25f * s.DetailScale, 0.4f * s.Depth)));
        }
        private static ShapeTemplateDescriptor Describe(string id, string category, string summary) =>
            new(id, summary, category, LowPolySemanticAssetSpecification.CurrentSchema,
                new[] { LowPolyShapeTypes.Frustum, LowPolyShapeTypes.Sphere }, Dimensions(), category, "clothing");
    }

    /// <summary>Builds a reusable weapon with stable grip, guard, and blade parts.</summary>
    public sealed class LowPolyWeaponTemplate : LowPolySemanticAssetTemplate
    {
        private static readonly ShapeTemplateDescriptor Metadata = Describe("lowpoly/weapon/1.0", "weapon", "Grip, guard, and blade prop.");
        /// <summary>Gets the shared template.</summary>
        public static LowPolyWeaponTemplate Instance { get; } = new();
        /// <inheritdoc />
        public override ShapeTemplateDescriptor Descriptor => Metadata;
        /// <inheritdoc />
        protected override string RootId => "weapon";
        /// <inheritdoc />
        protected override void Build(ShapeNode root, LowPolySemanticAssetSpecification s)
        {
            root.Add(Part("weapon.grip", "Grip", LowPolyShapeTypes.Cylinder, new(0f, -0.55f * s.Height, 0f), new(0.16f * s.Width, 0.45f * s.Height, 0.16f * s.Depth)));
            root.Add(Part("weapon.guard", "Guard", LowPolyShapeTypes.Cube, new(0f, -0.25f * s.Height, 0f), new(s.Width * s.DetailScale, 0.12f * s.Height, 0.2f * s.Depth)));
            root.Add(Part("weapon.blade", "Blade", LowPolyShapeTypes.Wedge, new(0f, 0.35f * s.Height, 0f), new(0.35f * s.Width, s.Height, s.Depth)));
        }
        private static ShapeTemplateDescriptor Describe(string id, string category, string summary) =>
            new(id, summary, category, LowPolySemanticAssetSpecification.CurrentSchema,
                new[] { LowPolyShapeTypes.Cylinder, LowPolyShapeTypes.Cube, LowPolyShapeTypes.Wedge }, Dimensions(), category, "prop");
    }

    /// <summary>Builds a reusable building mass with stable body, roof, and entrance.</summary>
    public sealed class LowPolyBuildingTemplate : LowPolySemanticAssetTemplate
    {
        private static readonly ShapeTemplateDescriptor Metadata = Describe("lowpoly/building/1.0", "building", "Building mass, roof, and entrance.");
        /// <summary>Gets the shared template.</summary>
        public static LowPolyBuildingTemplate Instance { get; } = new();
        /// <inheritdoc />
        public override ShapeTemplateDescriptor Descriptor => Metadata;
        /// <inheritdoc />
        protected override string RootId => "building";
        /// <inheritdoc />
        protected override void Build(ShapeNode root, LowPolySemanticAssetSpecification s)
        {
            root.Add(Part("building.body", "Building Body", LowPolyShapeTypes.Cube, new(0f, 0f, 0f), new(s.Width, s.Height, s.Depth)));
            root.Add(Part("building.roof", "Roof", LowPolyShapeTypes.Wedge, new(0f, 0.65f * s.Height, 0f), new(1.15f * s.Width, 0.35f * s.Height * s.DetailScale, 1.15f * s.Depth)));
            root.Add(Part("building.entrance", "Entrance", LowPolyShapeTypes.Cube, new(0f, -0.3f * s.Height, -0.52f * s.Depth), new(0.25f * s.Width, 0.45f * s.Height, 0.08f * s.Depth)));
        }
        private static ShapeTemplateDescriptor Describe(string id, string category, string summary) =>
            new(id, summary, category, LowPolySemanticAssetSpecification.CurrentSchema,
                new[] { LowPolyShapeTypes.Cube, LowPolyShapeTypes.Wedge }, Dimensions(), category, "environment");
    }

    /// <summary>Builds a reusable vehicle body with four stable wheel nodes.</summary>
    public sealed class LowPolyVehicleTemplate : LowPolySemanticAssetTemplate
    {
        private static readonly ShapeTemplateDescriptor Metadata = Describe("lowpoly/vehicle/1.0", "vehicle", "Body, cabin, and four wheels.");
        /// <summary>Gets the shared template.</summary>
        public static LowPolyVehicleTemplate Instance { get; } = new();
        /// <inheritdoc />
        public override ShapeTemplateDescriptor Descriptor => Metadata;
        /// <inheritdoc />
        protected override string RootId => "vehicle";
        /// <inheritdoc />
        protected override void Build(ShapeNode root, LowPolySemanticAssetSpecification s)
        {
            root.Add(Part("vehicle.body", "Vehicle Body", LowPolyShapeTypes.Cube, new(0f, 0f, 0f), new(s.Width, 0.45f * s.Height, s.Depth)));
            root.Add(Part("vehicle.cabin", "Cabin", LowPolyShapeTypes.Frustum, new(0f, 0.4f * s.Height, 0f), new(0.65f * s.Width, 0.45f * s.Height, 0.75f * s.Depth)));
            AddWheel(root, "front-left", -0.4f, -0.36f, s);
            AddWheel(root, "front-right", 0.4f, -0.36f, s);
            AddWheel(root, "rear-left", -0.4f, 0.36f, s);
            AddWheel(root, "rear-right", 0.4f, 0.36f, s);
        }
        private static void AddWheel(ShapeNode root, string id, float x, float z, LowPolySemanticAssetSpecification s) =>
            root.Add(Part($"vehicle.wheel-{id}", $"Wheel {id}", LowPolyShapeTypes.Cylinder,
                new(x * s.Width, -0.3f * s.Height, z * s.Depth),
                new(0.22f * s.DetailScale, 0.12f * s.Depth, 0.22f * s.DetailScale)));
        private static ShapeTemplateDescriptor Describe(string id, string category, string summary) =>
            new(id, summary, category, LowPolySemanticAssetSpecification.CurrentSchema,
                new[] { LowPolyShapeTypes.Cube, LowPolyShapeTypes.Frustum, LowPolyShapeTypes.Cylinder }, Dimensions(), category, "transport");
    }
}
