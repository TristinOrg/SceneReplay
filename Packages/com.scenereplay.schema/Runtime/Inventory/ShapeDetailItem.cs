using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Describes one semantic detail discovered before model construction.
    /// </summary>
    [Serializable]
    public sealed class ShapeDetailItem
    {
        /// <summary>Gets or sets the stable detail identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the human-readable detail name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets an extensible category such as armor, hair, or accessory.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Gets or sets an optional parent detail identifier.</summary>
        public string ParentId { get; set; } = string.Empty;

        /// <summary>Gets or sets the stable ShapeDefinition node expected to implement this detail.</summary>
        public string TargetNodeId { get; set; } = string.Empty;

        /// <summary>Gets or sets whether the finished asset must implement this detail.</summary>
        public bool Required { get; set; } = true;

        /// <summary>Gets or sets the observed repeated-instance count.</summary>
        public int RepeatCount { get; set; } = 1;

        /// <summary>Gets or sets observation confidence from zero to one.</summary>
        public float Confidence { get; set; } = 1f;

        /// <summary>Gets or sets extensible semantic tags.</summary>
        public IList<string> Tags { get; set; } = new List<string>();
    }
}
