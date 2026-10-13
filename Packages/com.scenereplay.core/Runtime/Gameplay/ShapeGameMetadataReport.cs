using System;

namespace SceneReplay
{
    /// <summary>
    /// Reports validated game-semantic bindings ready for native compilation.
    /// </summary>
    public sealed class ShapeGameMetadataReport
    {
        /// <summary>Initializes immutable game-metadata coverage.</summary>
        public ShapeGameMetadataReport(
            int                   anchorCount,
            int                   damageZoneCount,
            int                   colliderCount,
            int                   lodCount,
            ShapeDiagnosticReport diagnostics)
        {
            AnchorCount     = anchorCount;
            DamageZoneCount = damageZoneCount;
            ColliderCount   = colliderCount;
            LodCount        = lodCount;
            Diagnostics     = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        /// <summary>Gets bound semantic anchor count.</summary>
        public int AnchorCount { get; }
        /// <summary>Gets bound damage-zone count.</summary>
        public int DamageZoneCount { get; }
        /// <summary>Gets bound collider-rule count.</summary>
        public int ColliderCount { get; }
        /// <summary>Gets authored LOD count.</summary>
        public int LodCount { get; }
        /// <summary>Gets contract and node-binding diagnostics.</summary>
        public ShapeDiagnosticReport Diagnostics { get; }
        /// <summary>Gets whether metadata is safe for native compilation.</summary>
        public bool IsValid => Diagnostics.IsValid;
    }
}
