namespace SceneReplay
{
    /// <summary>
    /// Identifies the structural meaning of one ShapeDefinition difference.
    /// </summary>
    public enum ShapeDifferenceKind
    {
        /// <summary>A semantic node was added.</summary>
        NodeAdded,

        /// <summary>A semantic node was removed.</summary>
        NodeRemoved,

        /// <summary>A semantic node changed parent or sibling order.</summary>
        NodeMoved,

        /// <summary>A definition or node value changed.</summary>
        ValueChanged
    }
}
