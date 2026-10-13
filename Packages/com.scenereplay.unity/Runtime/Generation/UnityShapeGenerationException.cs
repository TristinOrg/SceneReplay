using System;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Reports failures while adapting a valid SceneReplay definition to Unity.
    /// </summary>
    public sealed class UnityShapeGenerationException : Exception
    {
        /// <summary>
        /// Initializes a Unity shape generation exception.
        /// </summary>
        public UnityShapeGenerationException(string message)
            : base(message)
        {
        }
    }
}
