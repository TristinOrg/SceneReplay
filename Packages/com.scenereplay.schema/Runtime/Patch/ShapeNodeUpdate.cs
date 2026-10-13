using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Describes optional authored-value replacements for one stable shape node.
    /// </summary>
    [Serializable]
    public sealed class ShapeNodeUpdate
    {
        /// <summary>Gets or sets a replacement generated object name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets a replacement extensible shape type.</summary>
        public string Type { get; set; }

        /// <summary>Gets or sets a complete replacement local transform.</summary>
        public ShapeTransform Transform { get; set; }

        /// <summary>Gets or sets complete replacement appearance data.</summary>
        public ShapeAppearance Appearance { get; set; }

        /// <summary>Gets or sets a replacement mirror axis.</summary>
        public ShapeMirrorAxis? MirrorAxis { get; set; }

        /// <summary>Gets or sets complete replacement numeric parameters.</summary>
        public Dictionary<string, float> Parameters { get; set; }

        /// <summary>Gets or sets a complete replacement profile.</summary>
        public List<ForgeVector2> Profile { get; set; }

        /// <summary>Gets or sets a complete replacement path.</summary>
        public List<ForgeVector3> Path { get; set; }

        /// <summary>Gets or sets complete replacement profile-loft sections.</summary>
        public List<ShapeProfileSection> ProfileSections { get; set; }

        /// <summary>Gets or sets complete replacement profile-cage sections.</summary>
        public List<ShapeProfileCageSection> ProfileCageSections { get; set; }
    }
}
