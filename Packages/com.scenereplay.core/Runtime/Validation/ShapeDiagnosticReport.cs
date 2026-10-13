using System;
using System.Collections.Generic;
using System.Linq;

namespace SceneReplay
{
    /// <summary>
    /// Provides immutable machine-readable diagnostics for one SceneReplay operation.
    /// </summary>
    public sealed class ShapeDiagnosticReport
    {
        /// <summary>Initializes a report from collected diagnostics.</summary>
        public ShapeDiagnosticReport(IEnumerable<ShapeDiagnostic> diagnostics)
        {
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            Diagnostics = diagnostics.ToArray();
        }

        /// <summary>Gets a successful report with no diagnostics.</summary>
        public static ShapeDiagnosticReport Success { get; } = new(Array.Empty<ShapeDiagnostic>());

        /// <summary>Gets all collected diagnostics in deterministic order.</summary>
        public IReadOnlyList<ShapeDiagnostic> Diagnostics { get; }

        /// <summary>Gets whether the operation produced no error diagnostics.</summary>
        public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != ShapeDiagnosticSeverity.Error);
    }
}
