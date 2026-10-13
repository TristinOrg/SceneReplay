namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Reports persistent Unity assets produced by native Prefab compilation.
    /// </summary>
    public sealed class UnityShapePrefabCompilationResult
    {
        /// <summary>Initializes immutable compiled asset paths.</summary>
        public UnityShapePrefabCompilationResult(string prefabPath, string meshAssetPath)
        {
            PrefabPath    = prefabPath;
            MeshAssetPath = meshAssetPath;
        }

        /// <summary>Gets the generated Prefab asset path.</summary>
        public string PrefabPath { get; }
        /// <summary>Gets the generated mesh container path, when required.</summary>
        public string MeshAssetPath { get; }
    }
}
