using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines one engine-agnostic depth section with an independently authored closed profile.
    /// </summary>
    [Serializable]
    public sealed class ShapeProfileCageSection
    {
        private readonly List<ForgeVector2> profile = new();

        /// <summary>Initializes an empty section for serialization.</summary>
        public ShapeProfileCageSection()
        {
        }

        /// <summary>Initializes a section at the supplied normalized depth.</summary>
        public ShapeProfileCageSection(float z, IEnumerable<ForgeVector2> profilePoints)
        {
            if (profilePoints == null)
                throw new ArgumentNullException(nameof(profilePoints));

            Z = z;
            profile.AddRange(profilePoints);
        }

        /// <summary>Gets or sets the normalized depth coordinate.</summary>
        public float Z { get; set; }

        /// <summary>Gets the independently authored closed profile.</summary>
        public IList<ForgeVector2> Profile => profile;
    }
}
