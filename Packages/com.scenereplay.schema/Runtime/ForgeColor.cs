using System;

namespace SceneReplay
{
    /// <summary>
    /// Represents an engine-agnostic linear RGBA color.
    /// </summary>
    [Serializable]
    public struct ForgeColor : IEquatable<ForgeColor>
    {
        /// <summary>
        /// Initializes a color from normalized components.
        /// </summary>
        public ForgeColor(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Gets or sets the red component.
        /// </summary>
        public float R { get; set; }

        /// <summary>
        /// Gets or sets the green component.
        /// </summary>
        public float G { get; set; }

        /// <summary>
        /// Gets or sets the blue component.
        /// </summary>
        public float B { get; set; }

        /// <summary>
        /// Gets or sets the alpha component.
        /// </summary>
        public float A { get; set; }

        /// <summary>
        /// Gets an opaque white color.
        /// </summary>
        public static ForgeColor White => new ForgeColor(1f, 1f, 1f);

        /// <inheritdoc />
        public bool Equals(ForgeColor other)
        {
            return R.Equals(other.R) &&
                   G.Equals(other.G) &&
                   B.Equals(other.B) &&
                   A.Equals(other.A);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ForgeColor other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = R.GetHashCode();
                hashCode = (hashCode * 397) ^ G.GetHashCode();
                hashCode = (hashCode * 397) ^ B.GetHashCode();
                hashCode = (hashCode * 397) ^ A.GetHashCode();
                return hashCode;
            }
        }
    }
}
