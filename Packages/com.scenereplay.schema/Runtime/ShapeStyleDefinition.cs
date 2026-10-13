using System;

namespace SceneReplay
{
    /// <summary>
    /// Defines versioned style data independently from model geometry.
    /// </summary>
    [Serializable]
    public sealed class ShapeStyleDefinition
    {
        /// <summary>
        /// Identifies the current SceneReplay style schema.
        /// </summary>
        public const string CurrentSchema = "scenereplay.style/1.0";

        /// <summary>
        /// Initializes an empty style for serialization.
        /// </summary>
        public ShapeStyleDefinition()
        {
        }

        /// <summary>
        /// Initializes a style with a stable identifier.
        /// </summary>
        public ShapeStyleDefinition(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets or sets the schema identifier.
        /// </summary>
        public string Schema { get; set; } = CurrentSchema;

        /// <summary>
        /// Gets or sets the stable style identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional parent style whose palette roles are inherited.
        /// </summary>
        public string BaseStyle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the semantic color palette.
        /// </summary>
        public ShapePalette Palette { get; set; } = new ShapePalette();
    }
}
