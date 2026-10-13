using System;

namespace SceneReplay
{
    /// <summary>
    /// Maps one semantic joint role to a stable shape node ID.
    /// </summary>
    [Serializable]
    public sealed class ShapeRigJoint
    {
        /// <summary>
        /// Initializes an empty joint mapping for serialization.
        /// </summary>
        public ShapeRigJoint()
        {
        }

        /// <summary>
        /// Initializes a semantic joint mapping.
        /// </summary>
        public ShapeRigJoint(string role, string nodeId)
        {
            Role   = role;
            NodeId = nodeId;
        }

        /// <summary>
        /// Initializes a constrained semantic joint mapping.
        /// </summary>
        public ShapeRigJoint(
            string                     role,
            string                     nodeId,
            ShapeRigRotationConstraint rotationConstraint)
            : this(role, nodeId)
        {
            RotationConstraint = rotationConstraint;
        }

        /// <summary>
        /// Gets or sets the engine-independent semantic role.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stable target node ID.
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional rotation limits relative to the node's authored rest pose.
        /// </summary>
        public ShapeRigRotationConstraint RotationConstraint { get; set; }
    }
}
