using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines one resumable construction stage and its atomic definition patch.
    /// </summary>
    [Serializable]
    public sealed class ShapeConstructionPass
    {
        /// <summary>Gets or sets the stable pass identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the human-readable pass name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the standard construction stage.</summary>
        public ShapeConstructionPassKind Kind { get; set; }

        /// <summary>Gets or sets persisted execution state.</summary>
        public ShapeConstructionPassState State { get; set; }

        /// <summary>Gets or sets pass IDs that must complete or be skipped first.</summary>
        public IList<string> DependsOn { get; set; } = new List<string>();

        /// <summary>Gets or sets the atomic definition edit produced by this pass.</summary>
        public ShapePatchDocument Patch { get; set; } = new();

        /// <summary>Gets or sets an optional quality-policy ID evaluated after the pass.</summary>
        public string QualityPolicyId { get; set; } = string.Empty;
    }
}
