using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines a versioned dependency graph for resumable staged asset construction.
    /// </summary>
    [Serializable]
    public sealed class ShapeConstructionPlan
    {
        /// <summary>Identifies the current construction-plan schema.</summary>
        public const string CurrentSchema = "scenereplay.construction-plan/1.0";

        /// <summary>Gets or sets the schema identifier.</summary>
        public string Schema { get; set; } = CurrentSchema;

        /// <summary>Gets or sets the stable plan identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the definition revision this plan starts from.</summary>
        public string BaseRevision { get; set; } = string.Empty;

        /// <summary>Gets or sets ordered construction passes.</summary>
        public IList<ShapeConstructionPass> Passes { get; set; } = new List<ShapeConstructionPass>();
    }
}
