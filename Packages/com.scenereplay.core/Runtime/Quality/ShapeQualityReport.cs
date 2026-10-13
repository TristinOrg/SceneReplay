using System;

namespace SceneReplay
{
    /// <summary>
    /// Contains deterministic quality measurements and actionable gate diagnostics.
    /// </summary>
    public sealed class ShapeQualityReport
    {
        /// <summary>Initializes an immutable quality report.</summary>
        public ShapeQualityReport(
            string                policyId,
            ShapeQualityMetrics   metrics,
            ShapeDiagnosticReport diagnostics)
        {
            PolicyId    = policyId ?? string.Empty;
            Metrics     = metrics ?? throw new ArgumentNullException(nameof(metrics));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        /// <summary>Gets the evaluated policy identifier.</summary>
        public string PolicyId { get; }

        /// <summary>Gets collected structural measurements.</summary>
        public ShapeQualityMetrics Metrics { get; }

        /// <summary>Gets validation and quality-gate diagnostics.</summary>
        public ShapeDiagnosticReport Diagnostics { get; }

        /// <summary>Gets whether every mandatory quality requirement passed.</summary>
        public bool Passed => Diagnostics.IsValid;
    }
}
