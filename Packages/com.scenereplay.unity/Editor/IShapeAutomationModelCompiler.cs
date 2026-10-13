using UnityEngine;

namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Allows an optional implementation package to compile definitions for Editor automation.
    /// </summary>
    public interface IShapeAutomationModelCompiler
    {
        /// <summary>Gets whether this implementation owns every requested shape type.</summary>
        bool CanCompile(ShapeDefinition definition);
        /// <summary>Compiles a temporary generated model owned by the caller.</summary>
        GameObject Compile(ShapeDefinition definition);
    }
}
