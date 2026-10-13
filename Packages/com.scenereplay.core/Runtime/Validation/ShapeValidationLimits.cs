using System;

namespace SceneReplay
{
    /// <summary>
    /// Bounds the authored complexity accepted before a shape definition reaches a generation backend.
    /// </summary>
    public sealed class ShapeValidationLimits
    {
        /// <summary>Gets the default limits suitable for runtime and external JSON generation.</summary>
        public static ShapeValidationLimits Default { get; } = new();

        /// <summary>Initializes validation limits.</summary>
        public ShapeValidationLimits(
            int maximumNodeCount       = 4096,
            int maximumHierarchyDepth  = 64,
            int maximumAuthoredPoints  = 262144)
        {
            if (maximumNodeCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumNodeCount));

            if (maximumHierarchyDepth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumHierarchyDepth));

            if (maximumAuthoredPoints <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumAuthoredPoints));

            MaximumNodeCount      = maximumNodeCount;
            MaximumHierarchyDepth = maximumHierarchyDepth;
            MaximumAuthoredPoints = maximumAuthoredPoints;
        }

        /// <summary>Gets the maximum number of authored nodes, excluding mirrored instances.</summary>
        public int MaximumNodeCount { get; }

        /// <summary>Gets the maximum root-inclusive hierarchy depth.</summary>
        public int MaximumHierarchyDepth { get; }

        /// <summary>Gets the maximum combined profile, path, and cage point count.</summary>
        public int MaximumAuthoredPoints { get; }
    }
}
