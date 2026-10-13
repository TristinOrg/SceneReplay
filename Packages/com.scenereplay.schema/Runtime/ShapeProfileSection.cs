using System;

namespace SceneReplay
{
    /// <summary>
    /// Defines one engine-agnostic depth section of a profile loft.
    /// </summary>
    [Serializable]
    public sealed class ShapeProfileSection
    {
        /// <summary>Initializes a default centered unit section.</summary>
        public ShapeProfileSection()
        {
        }

        /// <summary>Initializes a section with normalized depth, scale, and offset.</summary>
        public ShapeProfileSection(float z, ForgeVector2 scale, ForgeVector2 offset)
        {
            Z      = z;
            Scale  = scale;
            Offset = offset;
        }

        /// <summary>Gets or sets the normalized depth coordinate.</summary>
        public float Z { get; set; }

        /// <summary>Gets or sets the two-dimensional profile scale.</summary>
        public ForgeVector2 Scale { get; set; } = ForgeVector2.One;

        /// <summary>Gets or sets the two-dimensional profile offset.</summary>
        public ForgeVector2 Offset { get; set; } = ForgeVector2.Zero;
    }
}
