using System;

namespace SceneReplay
{
    /// <summary>
    /// Describes one machine-readable problem found in a SceneReplay document.
    /// </summary>
    public sealed class ShapeDiagnostic
    {
        /// <summary>Initializes an immutable diagnostic.</summary>
        public ShapeDiagnostic(
            string                  code,
            ShapeDiagnosticSeverity severity,
            string                  message,
            string                  nodeId = null,
            string                  path = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("A diagnostic requires a stable code.", nameof(code));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A diagnostic requires a message.", nameof(message));

            Code     = code;
            Severity = severity;
            Message  = message;
            NodeId   = nodeId;
            Path     = path;
        }

        /// <summary>Gets the stable machine-readable diagnostic code.</summary>
        public string Code { get; }

        /// <summary>Gets the diagnostic severity.</summary>
        public ShapeDiagnosticSeverity Severity { get; }

        /// <summary>Gets the human-readable diagnostic message.</summary>
        public string Message { get; }

        /// <summary>Gets the affected semantic node ID, when available.</summary>
        public string NodeId { get; }

        /// <summary>Gets the affected JSON-style document path, when available.</summary>
        public string Path { get; }
    }
}
