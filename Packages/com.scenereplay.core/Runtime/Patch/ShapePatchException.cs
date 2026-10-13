using System;

namespace SceneReplay
{
    /// <summary>
    /// Reports a machine-readable failure while applying a ShapePatch operation.
    /// </summary>
    public sealed class ShapePatchException : Exception
    {
        /// <summary>Initializes a patch failure.</summary>
        public ShapePatchException(string code, string message, int operationIndex = -1, string nodeId = null)
            : base(message)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("A patch failure requires a stable code.", nameof(code));

            Code           = code;
            OperationIndex = operationIndex;
            NodeId         = nodeId;
        }

        /// <summary>Gets the stable machine-readable failure code.</summary>
        public string Code { get; }

        /// <summary>Gets the zero-based failing operation index, or -1 for a document failure.</summary>
        public int OperationIndex { get; }

        /// <summary>Gets the affected stable node ID, when available.</summary>
        public string NodeId { get; }

        /// <summary>Gets the JSON-style path to the failing operation.</summary>
        public string Path => OperationIndex < 0 ? null : $"/operations/{OperationIndex}";
    }
}
