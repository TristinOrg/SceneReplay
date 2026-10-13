using System;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Converts SceneReplay specification values to Unity runtime values.
    /// </summary>
    public static class UnityShapeConversions
    {
        /// <summary>
        /// Converts an engine-agnostic vector to a Unity vector.
        /// </summary>
        public static Vector3 ToUnity(this ForgeVector3 value)
        {
            return new(value.X, value.Y, value.Z);
        }

        /// <summary>
        /// Converts a Unity vector to an engine-agnostic vector.
        /// </summary>
        public static ForgeVector3 ToForge(this Vector3 value)
        {
            return new(value.x, value.y, value.z);
        }

        /// <summary>
        /// Converts an engine-agnostic linear color to a Unity color.
        /// </summary>
        public static Color ToUnity(this ForgeColor value)
        {
            return new(value.R, value.G, value.B, value.A);
        }

        /// <summary>
        /// Applies an engine-agnostic transform definition to a Unity transform.
        /// </summary>
        public static void ApplyTo(this ShapeTransform definition, Transform target)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.localPosition    = definition.Position.ToUnity();
            target.localEulerAngles = definition.EulerAngles.ToUnity();
            target.localScale       = definition.Scale.ToUnity();
        }
    }
}
