using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Creates an isolated appearance session for one generated model.
    /// </summary>
    public interface IUnityAppearanceBackend
    {
        /// <summary>
        /// Begins an appearance generation session.
        /// </summary>
        IUnityAppearanceSession Begin(ShapeGenerationContext context);
    }

    /// <summary>
    /// Applies resolved appearance data during one model generation pass.
    /// </summary>
    public interface IUnityAppearanceSession
    {
        /// <summary>
        /// Attaches the session to the generated model root before applying node appearance.
        /// </summary>
        void Attach(GameObject root);

        /// <summary>
        /// Applies the appearance for one generated renderer.
        /// </summary>
        void Apply(Renderer renderer, ShapeNode node);

        /// <summary>
        /// Persists all runtime appearance bindings on the generated root.
        /// </summary>
        void Complete(GameObject root);
    }
}
