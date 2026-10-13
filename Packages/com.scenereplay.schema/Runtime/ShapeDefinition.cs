using System;

namespace SceneReplay
{
    /// <summary>
    /// Defines a versioned procedural model and its requested style.
    /// </summary>
    [Serializable]
    public sealed class ShapeDefinition
    {
        /// <summary>
        /// Identifies the current SceneReplay model schema.
        /// </summary>
        public const string CurrentSchema = "scenereplay.shape/1.0";

        /// <summary>
        /// Initializes an empty definition for serialization.
        /// </summary>
        public ShapeDefinition()
        {
        }

        /// <summary>
        /// Initializes a model definition with a root shape.
        /// </summary>
        public ShapeDefinition(string name, ShapeNode root)
        {
            Name = name;
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>
        /// Gets or sets the schema identifier.
        /// </summary>
        public string Schema { get; set; } = CurrentSchema;

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Name { get; set; } = "Shape";

        /// <summary>
        /// Gets or sets the style identifier resolved by a generation backend.
        /// </summary>
        public string Style { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional semantic rig exposed to motion systems.
        /// </summary>
        public ShapeRigDefinition Rig { get; set; }

        /// <summary>
        /// Gets or sets the root shape node.
        /// </summary>
        public ShapeNode Root { get; set; } = new ShapeNode();
    }
}
