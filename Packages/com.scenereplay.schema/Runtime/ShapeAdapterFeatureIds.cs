namespace SceneReplay
{
    /// <summary>
    /// Defines standard feature identifiers used by versioned adapter capability documents.
    /// </summary>
    public static class ShapeAdapterFeatureIds
    {
        /// <summary>Stable semantic node identifiers are retained in the host hierarchy.</summary>
        public const string StableNodeIds = "stable-node-ids";

        /// <summary>Semantic parent-child relationships are retained in the host hierarchy.</summary>
        public const string Hierarchy = "hierarchy";

        /// <summary>Authored local position, rotation, and scale are mapped to host transforms.</summary>
        public const string LocalTransforms = "local-transforms";

        /// <summary>Semantic rig roles and joint constraints can be preserved or compiled.</summary>
        public const string SemanticRig = "semantic-rig";

        /// <summary>Versioned gameplay metadata can be preserved or compiled.</summary>
        public const string GameMetadata = "game-metadata";

        /// <summary>Ordered SceneReplay patches can be applied to authored definitions.</summary>
        public const string ShapePatches = "shape-patches";

        /// <summary>Applied patches can be reversed with their generated inverse patch.</summary>
        public const string PatchRollback = "patch-rollback";
    }
}
