namespace SceneReplay.Unity
{
    /// <summary>
    /// Publishes the Unity adapter's versioned engine-neutral capability declaration.
    /// </summary>
    public static class UnityShapeAdapter
    {
        /// <summary>Gets the stable adapter identifier shared by generated Unity models.</summary>
        public const string AdapterId = "com.scenereplay.unity";

        /// <summary>Gets the Unity adapter's current conformance descriptor.</summary>
        public static ShapeAdapterDescriptor Descriptor => new(
            AdapterId,
            "unity",
            "0.1.0",
            new[]
            {
                ShapeAdapterFeatureIds.StableNodeIds,
                ShapeAdapterFeatureIds.Hierarchy,
                ShapeAdapterFeatureIds.LocalTransforms,
                ShapeAdapterFeatureIds.SemanticRig,
                ShapeAdapterFeatureIds.GameMetadata
            },
            ShapeAdapterUnsupportedFeaturePolicy.Reject);
    }
}
