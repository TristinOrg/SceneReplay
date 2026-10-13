using System;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Uses shared materials for palette colors and property blocks for direct overrides.
    /// </summary>
    public sealed class HybridUnityAppearanceBackend : IUnityAppearanceBackend
    {
        /// <inheritdoc />
        public IUnityAppearanceSession Begin(ShapeGenerationContext context)
        {
            return new Session(context);
        }

        private sealed class Session : IUnityAppearanceSession
        {
            private readonly ShapeGenerationContext context;

            private GameObject                   root;
            private UnityShapeAppearanceManifest manifest;

            public Session(ShapeGenerationContext context)
            {
                this.context = context ?? throw new ArgumentNullException(nameof(context));
            }

            public void Attach(GameObject root)
            {
                this.root = root != null
                    ? root
                    : throw new ArgumentNullException(nameof(root));
            }

            public void Apply(Renderer renderer, ShapeNode node)
            {
                if (renderer == null)
                    throw new ArgumentNullException(nameof(renderer));

                if (node == null)
                    throw new ArgumentNullException(nameof(node));

                if (root == null)
                    throw new InvalidOperationException("Appearance session must be attached before use.");

                if (!context.TryResolveColor(node, out ForgeColor color))
                    return;

                if (manifest == null)
                    manifest = root.AddComponent<UnityShapeAppearanceManifest>();

                manifest.AddBinding(
                    renderer,
                    renderer.sharedMaterial,
                    color.ToUnity(),
                    node.Appearance.HasColorOverride
                        ? UnityShapeAppearanceMode.PropertyBlock
                        : UnityShapeAppearanceMode.SharedMaterial);
            }

            public void Complete(GameObject root)
            {
                if (root != this.root)
                    throw new ArgumentException("Appearance session completed with a different root.", nameof(root));
            }
        }
    }
}
