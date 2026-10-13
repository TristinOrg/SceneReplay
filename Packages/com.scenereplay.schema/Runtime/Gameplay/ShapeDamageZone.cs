using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines a semantic damage region attached to a stable model node.
    /// </summary>
    [Serializable]
    public sealed class ShapeDamageZone
    {
        /// <summary>Gets or sets the stable zone identifier.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets the stable owner node ID.</summary>
        public string NodeId { get; set; } = string.Empty;
        /// <summary>Gets or sets the non-negative damage multiplier.</summary>
        public float Multiplier { get; set; } = 1f;
        /// <summary>Gets or sets extensible gameplay tags.</summary>
        public IList<string> Tags { get; set; } = new List<string>();
    }
}
