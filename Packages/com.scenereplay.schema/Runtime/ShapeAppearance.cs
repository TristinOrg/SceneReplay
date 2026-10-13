using System;

namespace SceneReplay
{
    /// <summary>
    /// Describes style-independent appearance requests for a shape node.
    /// </summary>
    [Serializable]
    public sealed class ShapeAppearance
    {
        /// <summary>
        /// Gets or sets the semantic palette role used by a style.
        /// </summary>
        public string ColorRole { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this node overrides its resolved palette color.
        /// </summary>
        public bool HasColorOverride { get; set; }

        /// <summary>
        /// Gets or sets the direct linear color override.
        /// </summary>
        public ForgeColor Color { get; set; } = ForgeColor.White;
    }
}
