using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines an ordered, versioned set of atomic ShapeDefinition edits.
    /// </summary>
    [Serializable]
    public sealed class ShapePatchDocument
    {
        /// <summary>Identifies the current ShapePatch schema.</summary>
        public const string CurrentSchema = "scenereplay.patch/1.0";

        /// <summary>Gets or sets the patch schema identifier.</summary>
        public string Schema { get; set; } = CurrentSchema;

        /// <summary>Gets or sets the ordered operations.</summary>
        public IList<ShapePatchOperation> Operations { get; set; } = new List<ShapePatchOperation>();
    }
}
