using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines the canonical semantic roles required by a biped humanoid rig.
    /// </summary>
    public static class ShapeHumanoidRig
    {
        private static readonly string[] requiredRoles =
        {
            ShapeRigRoles.Hips,
            ShapeRigRoles.Spine,
            ShapeRigRoles.Chest,
            ShapeRigRoles.Neck,
            ShapeRigRoles.Head,
            ShapeRigRoles.LeftUpperArm,
            ShapeRigRoles.LeftLowerArm,
            ShapeRigRoles.LeftHand,
            ShapeRigRoles.RightUpperArm,
            ShapeRigRoles.RightLowerArm,
            ShapeRigRoles.RightHand,
            ShapeRigRoles.LeftUpperLeg,
            ShapeRigRoles.LeftLowerLeg,
            ShapeRigRoles.LeftFoot,
            ShapeRigRoles.RightUpperLeg,
            ShapeRigRoles.RightLowerLeg,
            ShapeRigRoles.RightFoot
        };

        /// <summary>
        /// Gets the mandatory semantic roles for a Unity-compatible biped humanoid skeleton.
        /// </summary>
        public static IReadOnlyList<string> RequiredRoles => requiredRoles;

        /// <summary>
        /// Throws when a semantic rig does not provide every mandatory humanoid role.
        /// </summary>
        public static void ValidateRequiredRoles(ShapeRigDefinition rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));

            if (rig.Joints == null)
                throw new ShapeValidationException("Humanoid rig requires a joint collection.");

            HashSet<string> roles = new(StringComparer.Ordinal);
            foreach (ShapeRigJoint joint in rig.Joints)
            {
                if (joint == null || string.IsNullOrWhiteSpace(joint.Role) ||
                    string.IsNullOrWhiteSpace(joint.NodeId))
                {
                    throw new ShapeValidationException("Every humanoid rig joint requires a role and node ID.");
                }

                if (!roles.Add(joint.Role))
                    throw new ShapeValidationException($"Duplicate humanoid rig role '{joint.Role}'.");
            }

            ShapeRigIndex index = new(rig);
            foreach (string role in requiredRoles)
            {
                if (!index.TryGetNodeId(role, out _))
                    throw new ShapeValidationException($"Humanoid rig requires semantic role '{role}'.");
            }
        }
    }
}
