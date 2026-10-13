using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Reuses one generated material for each base-material and palette-color pair.
    /// </summary>
    internal static class UnityPaletteMaterialCache
    {
        private static readonly int ColorProperty     = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        private static readonly Dictionary<MaterialKey, Material> Materials =
            new Dictionary<MaterialKey, Material>();

        public static Material Get(Material baseMaterial, Color color)
        {
            if (baseMaterial == null)
                return null;

            MaterialKey key = new MaterialKey(baseMaterial, color);
            if (Materials.TryGetValue(key, out Material material) && material != null)
                return material;

            material = new Material(baseMaterial)
            {
                enableInstancing = true,
                hideFlags        = HideFlags.HideAndDontSave,
                name             = $"{baseMaterial.name} (SceneReplay Palette)"
            };

            if (material.HasProperty(ColorProperty))
                material.SetColor(ColorProperty, color);

            if (material.HasProperty(BaseColorProperty))
                material.SetColor(BaseColorProperty, color);

            Materials[key] = material;
            return material;
        }

        private readonly struct MaterialKey : IEquatable<MaterialKey>
        {
            private readonly Material material;
            private readonly Color    color;

            public MaterialKey(Material material, Color color)
            {
                this.material = material;
                this.color    = color;
            }

            public bool Equals(MaterialKey other)
            {
                return material == other.material && color.Equals(other.color);
            }

            public override bool Equals(object obj)
            {
                return obj is MaterialKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((material != null ? material.GetInstanceID() : 0) * 397) ^ color.GetHashCode();
                }
            }
        }
    }
}
