using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Caches Unity's built-in primitive render resources without retaining Collider components.
    /// </summary>
    internal static class UnityPrimitiveTemplateCache
    {
        private static readonly Dictionary<PrimitiveType, PrimitiveTemplate> Templates = new();

        public static PrimitiveTemplate Get(PrimitiveType primitiveType)
        {
            if (Templates.TryGetValue(primitiveType, out PrimitiveTemplate template))
                return template;

            GameObject source = GameObject.CreatePrimitive(primitiveType);
            source.SetActive(false);
            source.hideFlags = HideFlags.HideAndDontSave;

            template = new(
                source.GetComponent<MeshFilter>().sharedMesh,
                source.GetComponent<MeshRenderer>().sharedMaterial);

            Templates.Add(primitiveType, template);

            if (Application.isPlaying)
                Object.Destroy(source);
            else
                Object.DestroyImmediate(source);

            return template;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Templates.Clear();
        }
    }

    /// <summary>
    /// Holds shared render resources copied from one built-in Unity primitive.
    /// </summary>
    internal readonly struct PrimitiveTemplate
    {
        public PrimitiveTemplate(Mesh mesh, Material material)
        {
            Mesh     = mesh;
            Material = material;
        }

        public Mesh Mesh { get; }

        public Material Material { get; }
    }
}
