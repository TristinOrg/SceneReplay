using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Creates a Unity Humanoid Avatar from a generated SceneReplay rigid-part hierarchy.
    /// </summary>
    public static class UnityHumanoidAvatarBuilder
    {
        private static readonly HumanBoneMapping[] mappings =
        {
            new(ShapeRigRoles.Hips, HumanBodyBones.Hips),
            new(ShapeRigRoles.Spine, HumanBodyBones.Spine),
            new(ShapeRigRoles.Chest, HumanBodyBones.Chest),
            new(ShapeRigRoles.Neck, HumanBodyBones.Neck),
            new(ShapeRigRoles.Head, HumanBodyBones.Head),
            new(ShapeRigRoles.LeftUpperArm, HumanBodyBones.LeftUpperArm),
            new(ShapeRigRoles.LeftLowerArm, HumanBodyBones.LeftLowerArm),
            new(ShapeRigRoles.LeftHand, HumanBodyBones.LeftHand),
            new(ShapeRigRoles.RightUpperArm, HumanBodyBones.RightUpperArm),
            new(ShapeRigRoles.RightLowerArm, HumanBodyBones.RightLowerArm),
            new(ShapeRigRoles.RightHand, HumanBodyBones.RightHand),
            new(ShapeRigRoles.LeftUpperLeg, HumanBodyBones.LeftUpperLeg),
            new(ShapeRigRoles.LeftLowerLeg, HumanBodyBones.LeftLowerLeg),
            new(ShapeRigRoles.LeftFoot, HumanBodyBones.LeftFoot),
            new(ShapeRigRoles.RightUpperLeg, HumanBodyBones.RightUpperLeg),
            new(ShapeRigRoles.RightLowerLeg, HumanBodyBones.RightLowerLeg),
            new(ShapeRigRoles.RightFoot, HumanBodyBones.RightFoot)
        };

        /// <summary>
        /// Creates a valid Unity Humanoid Avatar when the generated hierarchy matches the required biped skeleton.
        /// </summary>
        public static Avatar CreateAvatar(UnityShapeModel model, ShapeRigDefinition rig)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (rig == null)
                throw new ArgumentNullException(nameof(rig));

            if (model.transform.parent != null)
                throw new ShapeValidationException("The generated humanoid root must be a top-level GameObject.");

            ShapeHumanoidRig.ValidateRequiredRoles(rig);
            Dictionary<string, Transform> transformsByRole = ResolveTransforms(model, rig);
            ValidateHierarchy(model.transform, transformsByRole);

            Avatar avatar = AvatarBuilder.BuildHumanAvatar(model.gameObject, new HumanDescription
            {
                human = CreateHumanBones(transformsByRole),
                skeleton = Array.Empty<SkeletonBone>()
            });
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                DestroyAvatar(avatar);
                throw new ShapeValidationException("Unity could not create a valid Humanoid Avatar from this rig.");
            }

            return avatar;
        }

        private static Dictionary<string, Transform> ResolveTransforms(UnityShapeModel model, ShapeRigDefinition rig)
        {
            ShapeRigIndex                    index            = new(rig);
            Dictionary<string, Transform> transformsByRole = new(mappings.Length, StringComparer.Ordinal);
            foreach (HumanBoneMapping mapping in mappings)
            {
                index.TryGetNodeId(mapping.Role, out string nodeId);
                if (!model.TryGetTransform(nodeId, out Transform transform))
                    throw new ShapeValidationException(
                        $"Humanoid role '{mapping.Role}' targets unavailable generated node '{nodeId}'.");

                transformsByRole.Add(mapping.Role, transform);
            }

            return transformsByRole;
        }

        private static void ValidateHierarchy(Transform root, IReadOnlyDictionary<string, Transform> transformsByRole)
        {
            ValidateParent(root, transformsByRole, ShapeRigRoles.Hips);
            ValidateParent(transformsByRole[ShapeRigRoles.Hips], transformsByRole, ShapeRigRoles.Spine);
            ValidateParent(transformsByRole[ShapeRigRoles.Spine], transformsByRole, ShapeRigRoles.Chest);
            ValidateParent(transformsByRole[ShapeRigRoles.Chest], transformsByRole, ShapeRigRoles.Neck);
            ValidateParent(transformsByRole[ShapeRigRoles.Neck], transformsByRole, ShapeRigRoles.Head);
            ValidateParent(transformsByRole[ShapeRigRoles.Chest], transformsByRole, ShapeRigRoles.LeftUpperArm);
            ValidateParent(transformsByRole[ShapeRigRoles.LeftUpperArm], transformsByRole, ShapeRigRoles.LeftLowerArm);
            ValidateParent(transformsByRole[ShapeRigRoles.LeftLowerArm], transformsByRole, ShapeRigRoles.LeftHand);
            ValidateParent(transformsByRole[ShapeRigRoles.Chest], transformsByRole, ShapeRigRoles.RightUpperArm);
            ValidateParent(transformsByRole[ShapeRigRoles.RightUpperArm], transformsByRole, ShapeRigRoles.RightLowerArm);
            ValidateParent(transformsByRole[ShapeRigRoles.RightLowerArm], transformsByRole, ShapeRigRoles.RightHand);
            ValidateParent(transformsByRole[ShapeRigRoles.Hips], transformsByRole, ShapeRigRoles.LeftUpperLeg);
            ValidateParent(transformsByRole[ShapeRigRoles.LeftUpperLeg], transformsByRole, ShapeRigRoles.LeftLowerLeg);
            ValidateParent(transformsByRole[ShapeRigRoles.LeftLowerLeg], transformsByRole, ShapeRigRoles.LeftFoot);
            ValidateParent(transformsByRole[ShapeRigRoles.Hips], transformsByRole, ShapeRigRoles.RightUpperLeg);
            ValidateParent(transformsByRole[ShapeRigRoles.RightUpperLeg], transformsByRole, ShapeRigRoles.RightLowerLeg);
            ValidateParent(transformsByRole[ShapeRigRoles.RightLowerLeg], transformsByRole, ShapeRigRoles.RightFoot);
        }

        private static void ValidateParent(
            Transform                           expectedParent,
            IReadOnlyDictionary<string, Transform> transformsByRole,
            string                              childRole)
        {
            if (transformsByRole[childRole].parent != expectedParent)
                throw new ShapeValidationException(
                    $"Humanoid role '{childRole}' must be a direct child of '{expectedParent.name}'.");
        }

        private static HumanBone[] CreateHumanBones(IReadOnlyDictionary<string, Transform> transformsByRole)
        {
            HumanBone[] bones = new HumanBone[mappings.Length];
            for (int index = 0; index < mappings.Length; index++)
            {
                HumanBoneMapping mapping = mappings[index];
                bones[index] = new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)mapping.Bone],
                    boneName  = transformsByRole[mapping.Role].name,
                    limit     = new HumanLimit { useDefaultValues = true }
                };
            }

            return bones;
        }

        private static void DestroyAvatar(Avatar avatar)
        {
            if (avatar == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(avatar);
            else
                UnityEngine.Object.DestroyImmediate(avatar);
        }

        private readonly struct HumanBoneMapping
        {
            public HumanBoneMapping(string role, HumanBodyBones bone)
            {
                Role = role;
                Bone = bone;
            }

            public string         Role { get; }
            public HumanBodyBones Bone { get; }
        }
    }
}
