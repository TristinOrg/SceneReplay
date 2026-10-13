namespace SceneReplay
{
    /// <summary>
    /// Identifies one engine-neutral ShapePatch operation.
    /// </summary>
    public enum ShapePatchOperationKind
    {
        /// <summary>Adds a node subtree to a parent.</summary>
        AddNode,

        /// <summary>Removes a node subtree.</summary>
        RemoveNode,

        /// <summary>Moves a node subtree to a parent and sibling index.</summary>
        MoveNode,

        /// <summary>Updates authored values without replacing child nodes.</summary>
        UpdateNode
    }
}
