using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines one serializable shape in an editable model hierarchy.
    /// </summary>
    [Serializable]
    public sealed class ShapeNode
    {
        private readonly List<ShapeNode>               children            = new();
        private readonly List<ForgeVector2>            profile             = new();
        private readonly List<ForgeVector3>            path                = new();
        private readonly List<ShapeProfileSection>     profileSections     = new();
        private readonly List<ShapeProfileCageSection> profileCageSections = new();
        private readonly Dictionary<string, float>     parameters          = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes an empty group node for serialization.
        /// </summary>
        public ShapeNode()
        {
        }

        /// <summary>
        /// Initializes a shape node with stable identity and type information.
        /// </summary>
        public ShapeNode(string id, string name, string type)
        {
            Id   = id;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets or sets the stable node identifier used by animation and regeneration.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the generated object name.
        /// </summary>
        public string Name { get; set; } = "Shape";

        /// <summary>
        /// Gets or sets the extensible shape type identifier.
        /// </summary>
        public string Type { get; set; } = ShapeTypes.Group;

        /// <summary>
        /// Gets or sets the local transform definition.
        /// </summary>
        public ShapeTransform Transform { get; set; } = new ShapeTransform();

        /// <summary>
        /// Gets or sets the appearance request for this node.
        /// </summary>
        public ShapeAppearance Appearance { get; set; } = new ShapeAppearance();

        /// <summary>
        /// Gets or sets the optional parent-space axis used to generate a mirrored instance.
        /// </summary>
        public ShapeMirrorAxis MirrorAxis { get; set; }

        /// <summary>
        /// Gets engine-agnostic numeric parameters interpreted by the selected shape type.
        /// </summary>
        public IDictionary<string, float> Parameters => parameters;

        /// <summary>
        /// Gets the optional normalized two-dimensional outline interpreted by the selected shape type.
        /// </summary>
        public IList<ForgeVector2> Profile => profile;

        /// <summary>
        /// Gets the optional ordered three-dimensional path interpreted by path-based shape types.
        /// </summary>
        public IList<ForgeVector3> Path => path;

        /// <summary>
        /// Gets the optional ordered depth sections used to form a profile loft.
        /// </summary>
        public IList<ShapeProfileSection> ProfileSections => profileSections;

        /// <summary>
        /// Gets optional ordered depth sections whose independent profiles form a profile cage.
        /// </summary>
        public IList<ShapeProfileCageSection> ProfileCageSections => profileCageSections;

        /// <summary>
        /// Gets the child shape definitions.
        /// </summary>
        public IList<ShapeNode> Children => children;

        /// <summary>
        /// Adds a child shape and returns this node for fluent composition.
        /// </summary>
        public ShapeNode Add(ShapeNode child)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            children.Add(child);
            return this;
        }

        private bool ShouldSerializeParameters()
        {
            return parameters.Count > 0;
        }

        private bool ShouldSerializeProfile()
        {
            return profile.Count > 0;
        }

        private bool ShouldSerializePath()
        {
            return path.Count > 0;
        }

        private bool ShouldSerializeProfileSections()
        {
            return profileSections.Count > 0;
        }

        private bool ShouldSerializeProfileCageSections()
        {
            return profileCageSections.Count > 0;
        }

        private bool ShouldSerializeMirrorAxis()
        {
            return MirrorAxis != ShapeMirrorAxis.None;
        }
    }
}
