using System;

namespace SceneReplay
{
    /// <summary>
    /// Describes one ordered structural or authored-value ShapePatch operation.
    /// </summary>
    [Serializable]
    public sealed class ShapePatchOperation
    {
        /// <summary>Gets or sets the operation kind.</summary>
        public ShapePatchOperationKind Kind { get; set; }

        /// <summary>Gets or sets the stable target node ID.</summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>Gets or sets the destination parent ID for add and move operations.</summary>
        public string ParentId { get; set; } = string.Empty;

        /// <summary>Gets or sets the destination index, or -1 to append.</summary>
        public int SiblingIndex { get; set; } = -1;

        /// <summary>Gets or sets the node subtree for an add operation.</summary>
        public ShapeNode Node { get; set; }

        /// <summary>Gets or sets authored values for an update operation.</summary>
        public ShapeNodeUpdate Update { get; set; }
    }
}
