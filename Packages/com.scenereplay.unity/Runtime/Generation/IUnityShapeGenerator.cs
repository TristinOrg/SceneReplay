using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Generates Unity geometry for supported engine-agnostic shape nodes.
    /// </summary>
    public interface IUnityShapeGenerator
    {
        /// <summary>
        /// Determines whether this generator supports a shape node.
        /// </summary>
        bool CanGenerate(ShapeNode node);

        /// <summary>
        /// Generates the Unity object representing a supported shape node.
        /// </summary>
        GameObject Generate(ShapeNode node, ShapeGenerationContext context);
    }
}
