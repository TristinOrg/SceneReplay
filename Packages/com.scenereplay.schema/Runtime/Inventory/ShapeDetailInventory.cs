using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines a versioned semantic inventory that guides staged asset construction.
    /// </summary>
    [Serializable]
    public sealed class ShapeDetailInventory
    {
        /// <summary>Identifies the current detail-inventory schema.</summary>
        public const string CurrentSchema = "scenereplay.detail-inventory/1.0";

        /// <summary>Gets or sets the schema identifier.</summary>
        public string Schema { get; set; } = CurrentSchema;

        /// <summary>Gets or sets the assessed subject.</summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>Gets or sets ordered semantic details.</summary>
        public IList<ShapeDetailItem> Details { get; set; } = new List<ShapeDetailItem>();
    }
}
