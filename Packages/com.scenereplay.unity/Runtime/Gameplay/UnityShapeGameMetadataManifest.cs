using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Stores compiled game-semantic anchors, damage values, and asset tags on one model root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityShapeGameMetadataManifest : MonoBehaviour
    {
        [SerializeField] private string          metadataId           = string.Empty;
        [SerializeField] private List<string>    anchorIds            = new();
        [SerializeField] private List<string>    anchorRoles          = new();
        [SerializeField] private List<Transform> anchorTransforms     = new();
        [SerializeField] private List<string>    damageZoneIds        = new();
        [SerializeField] private List<Transform> damageZoneTransforms = new();
        [SerializeField] private List<float>     damageMultipliers    = new();
        [SerializeField] private List<string>    tags                 = new();

        private Dictionary<string, int> anchorIndices;
        private Dictionary<string, int> damageZoneIndices;

        /// <summary>Gets the compiled metadata identifier.</summary>
        public string MetadataId => metadataId;

        /// <summary>Gets asset-wide gameplay tags.</summary>
        public IReadOnlyList<string> Tags => tags;

        /// <summary>Tries to resolve a compiled semantic anchor.</summary>
        public bool TryGetAnchor(string anchorId, out Transform anchor)
        {
            EnsureLookups();
            if (anchorIndices.TryGetValue(anchorId, out int index))
            {
                anchor = anchorTransforms[index];
                return true;
            }
            anchor = null;
            return false;
        }

        /// <summary>Tries to resolve a compiled anchor role.</summary>
        public bool TryGetAnchorRole(string anchorId, out string role)
        {
            EnsureLookups();
            if (anchorIndices.TryGetValue(anchorId, out int index))
            {
                role = anchorRoles[index];
                return true;
            }
            role = null;
            return false;
        }

        /// <summary>Tries to resolve a compiled damage zone.</summary>
        public bool TryGetDamageZone(string zoneId, out Transform target, out float multiplier)
        {
            EnsureLookups();
            if (damageZoneIndices.TryGetValue(zoneId, out int index))
            {
                target     = damageZoneTransforms[index];
                multiplier = damageMultipliers[index];
                return true;
            }
            target     = null;
            multiplier = default;
            return false;
        }

        internal void Initialize(ShapeGameMetadata metadata)
        {
            metadataId = metadata.Id;
            tags.AddRange(metadata.Tags);
        }

        internal void AddAnchor(string id, string role, Transform anchor)
        {
            anchorIds.Add(id);
            anchorRoles.Add(role);
            anchorTransforms.Add(anchor);
            anchorIndices = null;
        }

        internal void AddDamageZone(string id, Transform target, float multiplier)
        {
            damageZoneIds.Add(id);
            damageZoneTransforms.Add(target);
            damageMultipliers.Add(multiplier);
            damageZoneIndices = null;
        }

        private void EnsureLookups()
        {
            if (anchorIndices != null)
                return;
            anchorIndices     = new(anchorIds.Count, StringComparer.Ordinal);
            damageZoneIndices = new(damageZoneIds.Count, StringComparer.Ordinal);
            for (int index = 0; index < anchorIds.Count; index++)
                anchorIndices.Add(anchorIds[index], index);
            for (int index = 0; index < damageZoneIds.Count; index++)
                damageZoneIndices.Add(damageZoneIds[index], index);
        }

        private void OnValidate()
        {
            anchorIndices     = null;
            damageZoneIndices = null;
        }
    }
}
