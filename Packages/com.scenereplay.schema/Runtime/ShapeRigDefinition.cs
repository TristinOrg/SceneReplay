using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Describes engine-agnostic semantic joints exposed by a shape model.
    /// </summary>
    [Serializable]
    public sealed class ShapeRigDefinition
    {
        /// <summary>
        /// Gets or sets the extensible rig contract identifier.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the semantic joints mapped to stable shape node IDs.
        /// </summary>
        public IList<ShapeRigJoint> Joints { get; set; } = new List<ShapeRigJoint>();
    }
}
