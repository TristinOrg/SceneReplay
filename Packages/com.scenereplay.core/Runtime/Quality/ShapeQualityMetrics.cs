namespace SceneReplay
{
    /// <summary>
    /// Provides deterministic structural measurements collected during a quality evaluation.
    /// </summary>
    public sealed class ShapeQualityMetrics
    {
        /// <summary>Initializes immutable quality measurements.</summary>
        public ShapeQualityMetrics(int nodeCount, int hierarchyDepth, int rigRoleCount)
        {
            NodeCount      = nodeCount;
            HierarchyDepth = hierarchyDepth;
            RigRoleCount   = rigRoleCount;
        }

        /// <summary>Gets the total number of authored nodes.</summary>
        public int NodeCount { get; }

        /// <summary>Gets the deepest authored hierarchy level, including the root.</summary>
        public int HierarchyDepth { get; }

        /// <summary>Gets the number of semantic rig roles.</summary>
        public int RigRoleCount { get; }
    }
}
