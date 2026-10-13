using System;

namespace SceneReplay
{
    /// <summary>
    /// Represents an engine-agnostic two-dimensional vector.
    /// </summary>
    [Serializable]
    public struct ForgeVector2 : IEquatable<ForgeVector2>
    {
        /// <summary>
        /// Initializes a vector from its components.
        /// </summary>
        public ForgeVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Gets or sets the X component.</summary>
        public float X { get; set; }

        /// <summary>Gets or sets the Y component.</summary>
        public float Y { get; set; }

        /// <summary>Gets a zero vector.</summary>
        public static ForgeVector2 Zero => new(0f, 0f);

        /// <summary>Gets a unit vector.</summary>
        public static ForgeVector2 One => new(1f, 1f);

        /// <inheritdoc />
        public bool Equals(ForgeVector2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ForgeVector2 other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }
    }
}
