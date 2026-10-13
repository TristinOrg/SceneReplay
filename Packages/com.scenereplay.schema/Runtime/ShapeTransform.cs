using System;

namespace SceneReplay
{
    /// <summary>
    /// Describes an engine-agnostic local transform.
    /// </summary>
    [Serializable]
    public sealed class ShapeTransform
    {
        /// <summary>
        /// Gets or sets the local position in meters.
        /// </summary>
        public ForgeVector3 Position { get; set; } = ForgeVector3.Zero;

        /// <summary>
        /// Gets or sets the local Euler rotation in degrees.
        /// </summary>
        public ForgeVector3 EulerAngles { get; set; } = ForgeVector3.Zero;

        /// <summary>
        /// Gets or sets the local scale.
        /// </summary>
        public ForgeVector3 Scale { get; set; } = ForgeVector3.One;
    }
}
