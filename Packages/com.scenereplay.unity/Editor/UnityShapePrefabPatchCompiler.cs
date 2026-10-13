using System;
using UnityEngine;

namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Applies a reversible semantic edit and compiles its result with gameplay metadata into a Unity Prefab.
    /// </summary>
    public static class UnityShapePrefabPatchCompiler
    {
        /// <summary>Applies a patch, retains its inverse, and compiles the edited model as a Prefab.</summary>
        public static UnityShapePrefabPatchCompilationResult Compile(
            ShapeDefinition source,
            ShapePatchDocument patch,
            ShapeGameMetadata metadata,
            UnityShapeModelGenerator modelGenerator,
            string folder = "Assets/SceneReplay/Generated")
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (patch == null)
                throw new ArgumentNullException(nameof(patch));
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            if (modelGenerator == null)
                throw new ArgumentNullException(nameof(modelGenerator));

            ShapePatchTransaction transaction = new ShapePatchApplier().ApplyWithInverse(source, patch);
            GameObject generated = null;
            try
            {
                generated = modelGenerator.Generate(transaction.Definition);
                UnityShapePrefabCompilationResult compilation = UnityShapePrefabCompiler.CompileWithMetadata(
                    generated,
                    transaction.Definition,
                    metadata,
                    folder);
                return new(compilation, transaction.Definition, transaction.InversePatch);
            }
            finally
            {
                if (generated != null)
                    UnityEngine.Object.DestroyImmediate(generated);
            }
        }
    }
}
