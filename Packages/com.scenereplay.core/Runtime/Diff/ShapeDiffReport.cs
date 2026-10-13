using System;
using System.Collections.Generic;
using System.Linq;

namespace SceneReplay
{
    /// <summary>
    /// Provides an immutable ordered set of ShapeDefinition differences.
    /// </summary>
    public sealed class ShapeDiffReport
    {
        /// <summary>Initializes a report from deterministic differences.</summary>
        public ShapeDiffReport(IEnumerable<ShapeDifference> differences)
        {
            if (differences == null)
                throw new ArgumentNullException(nameof(differences));

            Differences = differences.ToArray();
        }

        /// <summary>Gets all differences in deterministic document order.</summary>
        public IReadOnlyList<ShapeDifference> Differences { get; }

        /// <summary>Gets whether the definitions differ.</summary>
        public bool HasChanges => Differences.Count > 0;
    }
}
