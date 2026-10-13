namespace SceneReplay.Unity.Editor
{
    /// <summary>Reports a patched Prefab compilation and the data needed to undo the semantic edit.</summary>
    public sealed class UnityShapePrefabPatchCompilationResult
    {
        /// <summary>Initializes a compiled patch result.</summary>
        public UnityShapePrefabPatchCompilationResult(
            UnityShapePrefabCompilationResult compilation,
            ShapeDefinition definition,
            ShapePatchDocument inversePatch)
        {
            Compilation = compilation;
            Definition = definition;
            InversePatch = inversePatch;
        }

        /// <summary>Gets the native Prefab and persistent mesh paths.</summary>
        public UnityShapePrefabCompilationResult Compilation { get; }

        /// <summary>Gets the portable definition compiled into the Prefab.</summary>
        public ShapeDefinition Definition { get; }

        /// <summary>Gets the inverse patch that restores the source definition.</summary>
        public ShapePatchDocument InversePatch { get; }
    }
}
