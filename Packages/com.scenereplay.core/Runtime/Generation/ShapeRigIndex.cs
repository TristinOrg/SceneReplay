using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Provides allocation-free semantic role lookups after one-time construction.
    /// </summary>
    public sealed class ShapeRigIndex
    {
        private readonly Dictionary<string, string> nodeIdsByRole;
        private readonly Dictionary<string, ShapeRigRotationConstraint> constraintsByRole;

        /// <summary>
        /// Builds a lookup index from a validated semantic rig definition.
        /// </summary>
        public ShapeRigIndex(ShapeRigDefinition rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));

            nodeIdsByRole = new Dictionary<string, string>(rig.Joints.Count, StringComparer.Ordinal);
            constraintsByRole = new Dictionary<string, ShapeRigRotationConstraint>(rig.Joints.Count,
                StringComparer.Ordinal);
            foreach (ShapeRigJoint joint in rig.Joints)
            {
                nodeIdsByRole.Add(joint.Role, joint.NodeId);
                if (joint.RotationConstraint != null)
                    constraintsByRole.Add(joint.Role, joint.RotationConstraint);
            }
        }

        /// <summary>
        /// Tries to resolve a semantic role to its stable shape node ID.
        /// </summary>
        public bool TryGetNodeId(string role, out string nodeId)
        {
            return nodeIdsByRole.TryGetValue(role, out nodeId);
        }

        /// <summary>
        /// Clamps a requested rotation offset to the semantic joint's limits when configured.
        /// </summary>
        public ForgeVector3 ConstrainRotationOffset(string role, ForgeVector3 requestedOffset)
        {
            if (!constraintsByRole.TryGetValue(role, out ShapeRigRotationConstraint constraint))
                return requestedOffset;

            return new ForgeVector3(
                Clamp(requestedOffset.X, constraint.Minimum.X, constraint.Maximum.X),
                Clamp(requestedOffset.Y, constraint.Minimum.Y, constraint.Maximum.Y),
                Clamp(requestedOffset.Z, constraint.Minimum.Z, constraint.Maximum.Z));
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum)
                return minimum;

            return value > maximum ? maximum : value;
        }
    }
}
