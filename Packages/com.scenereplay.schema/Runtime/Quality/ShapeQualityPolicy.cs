using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines versioned semantic requirements that a generated game asset must satisfy.
    /// </summary>
    [Serializable]
    public sealed class ShapeQualityPolicy
    {
        /// <summary>Identifies the current SceneReplay quality-policy schema.</summary>
        public const string CurrentSchema = "scenereplay.quality/1.0";

        /// <summary>Gets or sets the schema identifier.</summary>
        public string Schema { get; set; } = CurrentSchema;

        /// <summary>Gets or sets the stable policy identifier.</summary>
        public string Id { get; set; } = "shape/default";

        /// <summary>Gets or sets a required semantic rig type, or an empty string for any type.</summary>
        public string RequiredRigType { get; set; } = string.Empty;

        /// <summary>Gets or sets stable node IDs that must exist.</summary>
        public IList<string> RequiredNodeIds { get; set; } = new List<string>();

        /// <summary>Gets or sets shape types that must each occur at least once.</summary>
        public IList<string> RequiredShapeTypes { get; set; } = new List<string>();

        /// <summary>Gets or sets semantic rig roles that must each be mapped.</summary>
        public IList<string> RequiredRigRoles { get; set; } = new List<string>();

        /// <summary>Gets or sets the maximum accepted node count, or zero for no policy limit.</summary>
        public int MaximumNodeCount { get; set; }

        /// <summary>Gets or sets the maximum accepted hierarchy depth, or zero for no policy limit.</summary>
        public int MaximumHierarchyDepth { get; set; }
    }
}
