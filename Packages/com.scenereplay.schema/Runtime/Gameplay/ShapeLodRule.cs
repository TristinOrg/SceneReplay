using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines one engine-neutral LOD level and its included stable nodes.
    /// </summary>
    [Serializable]
    public sealed class ShapeLodRule
    {
        /// <summary>Gets or sets the zero-based LOD level.</summary>
        public int Level { get; set; }
        /// <summary>Gets or sets the native transition height from zero to one.</summary>
        public float ScreenRelativeHeight { get; set; } = 1f;
        /// <summary>Gets or sets stable nodes included in this level.</summary>
        public IList<string> NodeIds { get; set; } = new List<string>();
    }
}
