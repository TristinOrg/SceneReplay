using System;
using System.Collections.Generic;
using System.Linq;

namespace SceneReplay
{
    /// <summary>
    /// Provides immutable resumable progress and scheduling information for a construction plan.
    /// </summary>
    public sealed class ShapeConstructionPlanReport
    {
        /// <summary>Initializes an immutable construction-plan report.</summary>
        public ShapeConstructionPlanReport(
            int                   passCount,
            int                   completedCount,
            IEnumerable<string>   readyPassIds,
            IEnumerable<string>   blockedPassIds,
            ShapeDiagnosticReport diagnostics)
        {
            PassCount       = passCount;
            CompletedCount  = completedCount;
            ReadyPassIds    = readyPassIds?.ToArray() ?? throw new ArgumentNullException(nameof(readyPassIds));
            BlockedPassIds  = blockedPassIds?.ToArray() ?? throw new ArgumentNullException(nameof(blockedPassIds));
            Diagnostics     = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        /// <summary>Gets the total pass count.</summary>
        public int PassCount { get; }

        /// <summary>Gets completed or intentionally skipped passes.</summary>
        public int CompletedCount { get; }

        /// <summary>Gets passes whose dependencies allow execution.</summary>
        public IReadOnlyList<string> ReadyPassIds { get; }

        /// <summary>Gets passes blocked by failed dependencies.</summary>
        public IReadOnlyList<string> BlockedPassIds { get; }

        /// <summary>Gets plan contract diagnostics.</summary>
        public ShapeDiagnosticReport Diagnostics { get; }

        /// <summary>Gets whether every pass completed or was skipped.</summary>
        public bool IsComplete => PassCount > 0 && CompletedCount == PassCount;
    }
}
