using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines versioned game semantics compiled by engine adapters into native assets.
    /// </summary>
    [Serializable]
    public sealed class ShapeGameMetadata
    {
        /// <summary>Identifies the current game-metadata schema.</summary>
        public const string CurrentSchema = "scenereplay.game-metadata/1.0";
        /// <summary>Gets or sets the schema identifier.</summary>
        public string Schema { get; set; } = CurrentSchema;
        /// <summary>Gets or sets the stable metadata identifier.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets semantic anchors.</summary>
        public IList<ShapeSemanticAnchor> Anchors { get; set; } = new List<ShapeSemanticAnchor>();
        /// <summary>Gets or sets semantic damage zones.</summary>
        public IList<ShapeDamageZone> DamageZones { get; set; } = new List<ShapeDamageZone>();
        /// <summary>Gets or sets collider compilation rules.</summary>
        public IList<ShapeColliderRule> Colliders { get; set; } = new List<ShapeColliderRule>();
        /// <summary>Gets or sets ordered LOD rules.</summary>
        public IList<ShapeLodRule> Lods { get; set; } = new List<ShapeLodRule>();
        /// <summary>Gets or sets asset-wide gameplay tags.</summary>
        public IList<string> Tags { get; set; } = new List<string>();
    }
}
