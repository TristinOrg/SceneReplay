using System;

namespace SceneReplay
{
    /// <summary>
    /// Describes one deterministic difference between two ShapeDefinitions.
    /// </summary>
    public sealed class ShapeDifference
    {
        /// <summary>Initializes an immutable difference.</summary>
        public ShapeDifference(
            ShapeDifferenceKind kind,
            string              path,
            string              nodeId = null,
            string              beforeValue = null,
            string              afterValue = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A shape difference requires a stable path.", nameof(path));

            Kind        = kind;
            Path        = path;
            NodeId      = nodeId;
            BeforeValue = beforeValue;
            AfterValue  = afterValue;
        }

        /// <summary>Gets the structural difference kind.</summary>
        public ShapeDifferenceKind Kind { get; }

        /// <summary>Gets the stable node ID, when the difference targets a node.</summary>
        public string NodeId { get; }

        /// <summary>Gets the stable document path.</summary>
        public string Path { get; }

        /// <summary>Gets the invariant value before the change.</summary>
        public string BeforeValue { get; }

        /// <summary>Gets the invariant value after the change.</summary>
        public string AfterValue { get; }
    }
}
