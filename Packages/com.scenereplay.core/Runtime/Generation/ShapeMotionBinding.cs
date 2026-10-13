using System;

namespace SceneReplay
{
    /// <summary>
    /// Describes one stable semantic motion target and its SceneReplay-owned rest pose.
    /// </summary>
    public sealed class ShapeMotionBinding
    {
        /// <summary>Initializes immutable motion binding metadata.</summary>
        public ShapeMotionBinding(
            string                     role,
            string                     nodeId,
            ShapeTransform             restPose,
            ShapeRigRotationConstraint rotationConstraint)
        {
            Role               = role ?? throw new ArgumentNullException(nameof(role));
            NodeId             = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            RestPose           = restPose ?? throw new ArgumentNullException(nameof(restPose));
            RotationConstraint = rotationConstraint;
        }

        /// <summary>Gets the semantic rig role.</summary>
        public string Role { get; }
        /// <summary>Gets the stable SceneReplay node ID.</summary>
        public string NodeId { get; }
        /// <summary>Gets an isolated copy of the authored local rest pose.</summary>
        public ShapeTransform RestPose { get; }
        /// <summary>Gets optional SceneReplay-owned rotation limits.</summary>
        public ShapeRigRotationConstraint RotationConstraint { get; }
    }
}
