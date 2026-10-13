namespace SceneReplay
{
    /// <summary>
    /// Resolves stable node IDs and semantic parent relationships from a compiled engine hierarchy.
    /// </summary>
    public interface IShapeHierarchyTransformResolver : IShapeTransformResolver
    {
        /// <summary>Gets the stable adapter identifier reported in its capability descriptor.</summary>
        string AdapterId { get; }

        /// <summary>Gets the semantic parent node ID, or an empty value for the definition root.</summary>
        bool TryGetParentNodeId(string nodeId, out string parentNodeId);
    }
}
