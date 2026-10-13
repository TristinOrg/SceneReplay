using System;

namespace SceneReplay
{
    /// <summary>
    /// Represents an engine-agnostic three-dimensional vector.
    /// </summary>
    [Serializable]
    public struct ForgeVector3 : IEquatable<ForgeVector3>
    {
        /// <summary>
        /// Initializes a vector from its components.
        /// </summary>
        public ForgeVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Gets or sets the X component.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Gets or sets the Y component.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Gets or sets the Z component.
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// Gets a zero vector.
        /// </summary>
        public static ForgeVector3 Zero => new ForgeVector3(0f, 0f, 0f);

        /// <summary>
        /// Gets a unit vector.
        /// </summary>
        public static ForgeVector3 One => new ForgeVector3(1f, 1f, 1f);

        /// <inheritdoc />
        public bool Equals(ForgeVector3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ForgeVector3 other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }
    }
}
