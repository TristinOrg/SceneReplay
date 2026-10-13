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
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            new ShapeDefinitionValidator().Validate(definition);
            if (!root.TryGetComponent(out UnityShapeModel model) || model.BindingCount == 0)
                throw new ShapeValidationException("Prefab compilation requires a generated UnityShapeModel.");
            ValidateFolder(folder);
            EnsureFolder(folder);

            ShapeJsonSerializer serializer = new();
            string              json       = serializer.Serialize(definition);
            string              fingerprint = Fingerprint(json);
            UnityShapeAssetManifest manifest = root.GetComponent<UnityShapeAssetManifest>();
            bool addedManifest = manifest == null;
            if (addedManifest)
                manifest = root.AddComponent<UnityShapeAssetManifest>();
            manifest.Initialize(definition.Schema, definition.Name, fingerprint, json, model.BindingCount);

            string meshPath   = null;
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Sanitize(root.name)}.prefab");
            try
            {
                meshPath = UnityGeneratedModelAssetStore.PersistMeshes(root, folder);
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
