using System;

namespace SceneReplay
{
    /// <summary>
    /// Defines engine-independent Euler rotation limits relative to a joint's authored rest pose.
    /// </summary>
    [Serializable]
    public sealed class ShapeRigRotationConstraint
    {
        /// <summary>
        /// Initializes unrestricted zero-valued limits for serialization.
        /// </summary>
        public ShapeRigRotationConstraint()
        {
        }

        /// <summary>
        /// Initializes minimum and maximum local Euler offsets in degrees.
        /// </summary>
        public ShapeRigRotationConstraint(ForgeVector3 minimum, ForgeVector3 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        /// Gets or sets the minimum local Euler offset in degrees.
        /// </summary>
        public ForgeVector3 Minimum { get; set; } = ForgeVector3.Zero;

        /// <summary>
        /// Gets or sets the maximum local Euler offset in degrees.
        /// </summary>
        public ForgeVector3 Maximum { get; set; } = ForgeVector3.Zero;
    }
}
