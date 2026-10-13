using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Compiles a generated hierarchy, portable source, meshes, and metadata into a Unity Prefab.
    /// </summary>
    public static class UnityShapePrefabCompiler
    {
        /// <summary>Persists a validated generated model as a self-describing Prefab.</summary>
        public static UnityShapePrefabCompilationResult Compile(
            GameObject      root,
            ShapeDefinition definition,
            string          folder = "Assets/SceneReplay/Generated")
        {
            return CompileInternal(root, definition, null, folder);
        }

        /// <summary>Compiles a model and its game metadata into a self-describing Unity Prefab.</summary>
        public static UnityShapePrefabCompilationResult CompileWithMetadata(
            GameObject root,
            ShapeDefinition definition,
            ShapeGameMetadata metadata,
            string folder = "Assets/SceneReplay/Generated")
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            return CompileInternal(root, definition, metadata, folder);
        }

        private static UnityShapePrefabCompilationResult CompileInternal(
            GameObject root,
            ShapeDefinition definition,
            ShapeGameMetadata metadata,
            string folder)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (!root.TryGetComponent(out UnityShapeModel model) || model.BindingCount == 0)
                throw new ShapeValidationException("Prefab compilation requires a generated UnityShapeModel.");
            ShapeDiagnosticReport conformance = metadata == null
                ? new ShapeAdapterConformanceValidator().Analyze(definition, UnityShapeAdapter.Descriptor, model)
                : new ShapeAdapterConformanceValidator().Analyze(definition, metadata, UnityShapeAdapter.Descriptor, model);
            if (!conformance.IsValid)
            {
                ShapeDiagnostic diagnostic = conformance.Diagnostics[0];
                throw new ShapeValidationException(diagnostic.Code, diagnostic.Message, diagnostic.NodeId, diagnostic.Path);
            }
            if (metadata != null)
            {
                if (root.TryGetComponent(out UnityShapeGameMetadataManifest _))
                    throw new InvalidOperationException("Game metadata has already been compiled for this model.");
            }
            ValidateFolder(folder);
            EnsureFolder(folder);

            ShapeJsonSerializer serializer = new();
            string              json         = serializer.Serialize(definition);
            string              metadataJson = metadata == null ? string.Empty : serializer.Serialize(metadata);
            string              fingerprint  = Fingerprint(metadata == null ? json : $"{json}\n{metadataJson}");
            UnityShapeAssetManifest manifest = root.GetComponent<UnityShapeAssetManifest>();
            bool addedManifest = manifest == null;
            if (addedManifest)
                manifest = root.AddComponent<UnityShapeAssetManifest>();
            manifest.Initialize(definition.Schema, definition.Name, fingerprint, json, model.BindingCount, metadataJson);

            string meshPath   = null;
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Sanitize(root.name)}.prefab");
            try
            {
                meshPath = UnityGeneratedModelAssetStore.PersistMeshes(root, folder);
                if (metadata != null)
                    new UnityShapeGameMetadataCompiler().Compile(model, definition, metadata);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                    throw new InvalidOperationException("Unity did not create the generated Prefab.");
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);
                return new(prefabPath, meshPath);
            }
            catch
            {
                if (!string.IsNullOrEmpty(prefabPath))
                    AssetDatabase.DeleteAsset(prefabPath);
                if (!string.IsNullOrEmpty(meshPath))
                    AssetDatabase.DeleteAsset(meshPath);
                if (addedManifest && manifest != null)
                    UnityEngine.Object.DestroyImmediate(manifest);
                throw;
            }
        }

        private static string Fingerprint(string value)
        {
            using SHA256 algorithm = SHA256.Create();
            byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            StringBuilder result = new(hash.Length * 2);
            foreach (byte item in hash)
                result.Append(item.ToString("x2"));
            return result.ToString();
        }

        private static void ValidateFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) ||
                (!folder.Equals("Assets", StringComparison.Ordinal) &&
                 !folder.StartsWith("Assets/", StringComparison.Ordinal)))
                throw new ArgumentException("Compiled Prefabs must be stored inside Assets.", nameof(folder));
        }

        private static void EnsureFolder(string folder)
        {
            string[] segments = folder.Split('/');
            string   current  = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "Generated Model" : value;
        }
    }
}
