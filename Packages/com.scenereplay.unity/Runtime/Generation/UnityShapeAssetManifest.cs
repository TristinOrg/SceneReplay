using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Persists portable source metadata alongside a compiled Unity model asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityShapeAssetManifest : MonoBehaviour
    {
        [SerializeField] private string schema           = string.Empty;
        [SerializeField] private string modelName        = string.Empty;
        [SerializeField] private string fingerprint      = string.Empty;
        [SerializeField] private string definitionJson   = string.Empty;
        [SerializeField] private string gameMetadataJson = string.Empty;
        [SerializeField] private int    nodeCount;

        /// <summary>Gets the source SceneReplay schema.</summary>
        public string Schema => schema;
        /// <summary>Gets the portable model name.</summary>
        public string ModelName => modelName;
        /// <summary>Gets the deterministic SHA-256 source fingerprint.</summary>
        public string Fingerprint => fingerprint;
        /// <summary>Gets the complete portable source JSON.</summary>
        public string DefinitionJson => definitionJson;
        /// <summary>Gets the complete portable game-metadata JSON, or an empty string when absent.</summary>
        public string GameMetadataJson => gameMetadataJson;
        /// <summary>Gets the compiled stable-node count.</summary>
        public int NodeCount => nodeCount;

        /// <summary>Initializes source metadata before Prefab persistence.</summary>
        public void Initialize(string sourceSchema, string sourceName, string sourceFingerprint, string sourceJson, int sourceNodeCount)
        {
            Initialize(sourceSchema, sourceName, sourceFingerprint, sourceJson, sourceNodeCount, null);
        }

        /// <summary>Initializes source metadata and optional game semantics before Prefab persistence.</summary>
        public void Initialize(
            string sourceSchema,
            string sourceName,
            string sourceFingerprint,
            string sourceJson,
            int sourceNodeCount,
            string sourceGameMetadataJson)
        {
            schema           = sourceSchema ?? string.Empty;
            modelName        = sourceName ?? string.Empty;
            fingerprint      = sourceFingerprint ?? string.Empty;
            definitionJson   = sourceJson ?? string.Empty;
            gameMetadataJson = sourceGameMetadataJson ?? string.Empty;
            nodeCount        = sourceNodeCount;
        }
    }
}
