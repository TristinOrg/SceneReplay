using System;

namespace SceneReplay
{
    /// <summary>
    /// Reports how completely a ShapeDefinition implements an approved detail inventory.
    /// </summary>
    public sealed class ShapeDetailCoverageReport
    {
        /// <summary>Initializes immutable inventory coverage.</summary>
        public ShapeDetailCoverageReport(
            int                   detailCount,
            int                   requiredCount,
            int                   resolvedRequiredCount,
            ShapeDiagnosticReport diagnostics)
        {
            DetailCount           = detailCount;
            RequiredCount         = requiredCount;
            ResolvedRequiredCount = resolvedRequiredCount;
            Diagnostics           = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        /// <summary>Gets the total inventory detail count.</summary>
        public int DetailCount { get; }

        /// <summary>Gets the required detail count.</summary>
        public int RequiredCount { get; }

        /// <summary>Gets the required details mapped to existing definition nodes.</summary>
        public int ResolvedRequiredCount { get; }

        /// <summary>Gets deterministic missing-detail diagnostics.</summary>
        public ShapeDiagnosticReport Diagnostics { get; }

        /// <summary>Gets whether every required detail is implemented.</summary>
        public bool Passed => Diagnostics.IsValid;

        /// <summary>Gets required-detail coverage from zero to one.</summary>
        public float RequiredCoverage => RequiredCount == 0 ? 1f : (float)ResolvedRequiredCount / RequiredCount;
    }
}
