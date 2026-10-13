using System;

namespace SceneReplay
{
    /// <summary>
    /// Maps a semantic color role to a concrete linear color.
    /// </summary>
    [Serializable]
    public sealed class ShapePaletteEntry
    {
        /// <summary>
        /// Initializes an empty palette entry for serialization.
        /// </summary>
        public ShapePaletteEntry()
        {
        }

        /// <summary>
        /// Initializes a palette entry with a semantic role and color.
        /// </summary>
        public ShapePaletteEntry(string role, ForgeColor color)
        {
            Role  = role;
            Color = color;
        }

        /// <summary>
        /// Gets or sets the semantic color role.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resolved linear color.
        /// </summary>
        public ForgeColor Color { get; set; } = ForgeColor.White;
    }
}
