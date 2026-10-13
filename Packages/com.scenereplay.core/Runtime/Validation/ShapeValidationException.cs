using System;

namespace SceneReplay
{
    /// <summary>
    /// Reports a SceneReplay definition that violates the current specification.
    /// </summary>
    public sealed class ShapeValidationException : Exception
    {
        /// <summary>
        /// Initializes a shape validation exception.
        /// </summary>
        public ShapeValidationException(string message)
            : this("shape.invalid", message)
        {
        }

        /// <summary>
        /// Initializes a shape validation exception with machine-readable context.
        /// </summary>
        public ShapeValidationException(string code, string message, string nodeId = null, string path = null)
            : base(message)
        {
            Code   = code;
            NodeId = nodeId;
            Path   = path;
        }

        /// <summary>Gets the stable machine-readable validation code.</summary>
        public string Code { get; }

        /// <summary>Gets the affected semantic node ID, when available.</summary>
        public string NodeId { get; }

        /// <summary>Gets the affected JSON-style document path, when available.</summary>
        public string Path { get; }
    }
}
