using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Persists and restores compact renderer appearance data for one generated model.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityShapeAppearanceManifest : MonoBehaviour
    {
        private static readonly int ColorProperty     = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        private static MaterialPropertyBlock sharedProperties;

        [SerializeField] private List<AppearanceBinding> bindings = new();

        /// <summary>
        /// Gets the number of persisted renderer bindings.
        /// </summary>
        public int BindingCount => bindings.Count;

        /// <summary>
        /// Restores all shared-material and property-block appearance state.
        /// </summary>
        public void Apply()
        {
            for (int index = 0; index < bindings.Count; index++)
                Apply(bindings[index]);
        }

        internal void AddBinding(
            Renderer                 renderer,
            Material                 baseMaterial,
            Color                    color,
            UnityShapeAppearanceMode mode)
        {
            AppearanceBinding binding = new(renderer, baseMaterial, color, mode);
            bindings.Add(binding);
            Apply(binding);
        }

        private static void Apply(AppearanceBinding binding)
        {
            Renderer renderer = binding.Renderer;
            if (renderer == null)
                return;

            renderer.sharedMaterial = binding.BaseMaterial;

            if (binding.Mode == UnityShapeAppearanceMode.SharedMaterial)
            {
                renderer.SetPropertyBlock(null);
                renderer.sharedMaterial = UnityPaletteMaterialCache.Get(binding.BaseMaterial, binding.Color);
                return;
            }

            sharedProperties ??= new();
            sharedProperties.Clear();
            renderer.GetPropertyBlock(sharedProperties);
            sharedProperties.SetColor(ColorProperty, binding.Color);
            sharedProperties.SetColor(BaseColorProperty, binding.Color);
            renderer.SetPropertyBlock(sharedProperties);
        }

        [Serializable]
        private struct AppearanceBinding
        {
            [SerializeField] private Renderer                 renderer;
            [SerializeField] private Material                 baseMaterial;
            [SerializeField] private Color                    color;
            [SerializeField] private UnityShapeAppearanceMode mode;

            public AppearanceBinding(
                Renderer                 renderer,
                Material                 baseMaterial,
                Color                    color,
                UnityShapeAppearanceMode mode)
            {
                this.renderer     = renderer;
                this.baseMaterial = baseMaterial;
                this.color        = color;
                this.mode         = mode;
            }

            public Renderer Renderer => renderer;

            public Material BaseMaterial => baseMaterial;

            public Color Color => color;

            public UnityShapeAppearanceMode Mode => mode;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }
    }

    /// <summary>
    /// Selects the Unity rendering path for one resolved color.
    /// </summary>
    public enum UnityShapeAppearanceMode
    {
        SharedMaterial = 0,
        PropertyBlock  = 1
    }
}
