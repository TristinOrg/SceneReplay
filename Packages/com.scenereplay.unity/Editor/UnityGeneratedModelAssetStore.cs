using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Persists transient meshes from a generated model as shared Unity assets suitable for Prefabs.
    /// </summary>
    public static class UnityGeneratedModelAssetStore
    {
        private const string DefaultFolder = "Assets/SceneReplay/Generated";

        /// <summary>
        /// Stores each unique transient mesh once and rewires the generated hierarchy to the persistent copies.
        /// </summary>
        public static string PersistMeshes(GameObject root, string folder = DefaultFolder)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (string.IsNullOrWhiteSpace(folder) ||
                (!folder.Equals("Assets", StringComparison.Ordinal) &&
                 !folder.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Generated mesh assets must be stored inside Assets.", nameof(folder));
            }

            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            Dictionary<Mesh, Mesh> persistentMeshes = new();
            for (int index = 0; index < filters.Length; index++)
            {
                Mesh source = filters[index].sharedMesh;
                if (source != null && !AssetDatabase.Contains(source))
                    persistentMeshes.TryAdd(source, null);
            }

            if (persistentMeshes.Count == 0)
                return null;

            EnsureFolder(folder);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{SanitizeFileName(root.name)} Meshes.asset");
            bool assetCreated = false;

            try
            {
                List<Mesh> sources = new(persistentMeshes.Keys);
                for (int index = 0; index < sources.Count; index++)
                {
                    Mesh source     = sources[index];
                    Mesh persistent = UnityEngine.Object.Instantiate(source);
                    persistent.hideFlags = HideFlags.None;
                    persistent.name      = source.name;

                    if (!assetCreated)
                    {
                        AssetDatabase.CreateAsset(persistent, assetPath);
                        assetCreated = true;
                    }
                    else
                    {
                        AssetDatabase.AddObjectToAsset(persistent, assetPath);
                    }

                    persistentMeshes[source] = persistent;
                }

                for (int index = 0; index < filters.Length; index++)
                {
                    Mesh source = filters[index].sharedMesh;
                    if (source != null && persistentMeshes.TryGetValue(source, out Mesh persistent))
                        filters[index].sharedMesh = persistent;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                return assetPath;
            }
            catch
            {
                if (assetCreated)
                    AssetDatabase.DeleteAsset(assetPath);

                throw;
            }
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

        private static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int index = 0; index < invalid.Length; index++)
                value = value.Replace(invalid[index], '_');

            return string.IsNullOrWhiteSpace(value) ? "Generated Model" : value;
        }
    }
}
