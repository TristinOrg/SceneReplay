using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SceneReplay.Unity.Tests
{
    /// <summary>
    /// Verifies Unity Humanoid Avatar creation from semantic SceneReplay rig roles.
    /// </summary>
    public sealed class UnityHumanoidAvatarBuilderTests
    {
        private GameObject generatedRoot;
        private Avatar     avatar;

        [TearDown]
        public void TearDown()
        {
            if (avatar != null)
                Object.DestroyImmediate(avatar);

            if (generatedRoot != null)
                Object.DestroyImmediate(generatedRoot);
        }

        [Test]
        public void CreateAvatarBuildsValidHumanoidFromRigidPartHierarchy()
        {
            ShapeDefinition definition = CreateHumanoidDefinition();
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[0]);
            generatedRoot = generator.Generate(definition);
            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();

            avatar = UnityHumanoidAvatarBuilder.CreateAvatar(model, definition.Rig);

            Assert.That(avatar.isValid, Is.True);
            Assert.That(avatar.isHuman, Is.True);
            Assert.That(
                avatar.humanDescription.human,
                Has.All.Matches<HumanBone>(bone => bone.limit.useDefaultValues));
        }

        [Test]
        public void CreateAvatarRejectsBrokenBoneHierarchy()
        {
            ShapeDefinition definition = CreateHumanoidDefinition();
            ShapeNode chest        = definition.Root.Children[0].Children[0].Children[0];
            ShapeNode leftUpperArm = chest.Children[1];
            chest.Children.Remove(leftUpperArm);
            definition.Root.Add(leftUpperArm);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[0]);
            generatedRoot = generator.Generate(definition);

            ShapeValidationException exception = Assert.Throws<ShapeValidationException>(() =>
                UnityHumanoidAvatarBuilder.CreateAvatar(generatedRoot.GetComponent<UnityShapeModel>(), definition.Rig));

            Assert.That(exception.Message, Does.Contain(ShapeRigRoles.LeftUpperArm));
        }

        private static ShapeDefinition CreateHumanoidDefinition()
        {
            ShapeNode root          = Node("root", "Character", 0f, 0f, 0f);
            ShapeNode hips          = Node("hips", "Hips", 0f, 1f, 0f);
            ShapeNode spine         = Node("spine", "Spine", 0f, 0.25f, 0f);
            ShapeNode chest         = Node("chest", "Chest", 0f, 0.25f, 0f);
            ShapeNode neck          = Node("neck", "Neck", 0f, 0.2f, 0f);
            ShapeNode head          = Node("head", "Head", 0f, 0.2f, 0f);
            ShapeNode leftUpperArm  = Node("left-upper-arm", "LeftUpperArm", -0.3f, 0.15f, 0f);
            ShapeNode leftLowerArm  = Node("left-lower-arm", "LeftLowerArm", -0.35f, 0f, 0f);
            ShapeNode leftHand      = Node("left-hand", "LeftHand", -0.25f, 0f, 0f);
            ShapeNode rightUpperArm = Node("right-upper-arm", "RightUpperArm", 0.3f, 0.15f, 0f);
            ShapeNode rightLowerArm = Node("right-lower-arm", "RightLowerArm", 0.35f, 0f, 0f);
            ShapeNode rightHand     = Node("right-hand", "RightHand", 0.25f, 0f, 0f);
            ShapeNode leftUpperLeg  = Node("left-upper-leg", "LeftUpperLeg", -0.15f, -0.45f, 0f);
            ShapeNode leftLowerLeg  = Node("left-lower-leg", "LeftLowerLeg", 0f, -0.45f, 0f);
            ShapeNode leftFoot      = Node("left-foot", "LeftFoot", 0f, -0.4f, 0.1f);
            ShapeNode rightUpperLeg = Node("right-upper-leg", "RightUpperLeg", 0.15f, -0.45f, 0f);
            ShapeNode rightLowerLeg = Node("right-lower-leg", "RightLowerLeg", 0f, -0.45f, 0f);
            ShapeNode rightFoot     = Node("right-foot", "RightFoot", 0f, -0.4f, 0.1f);

            root.Add(hips);
            hips.Add(spine).Add(leftUpperLeg).Add(rightUpperLeg);
            spine.Add(chest);
            chest.Add(neck).Add(leftUpperArm).Add(rightUpperArm);
            neck.Add(head);
            leftUpperArm.Add(leftLowerArm);
            leftLowerArm.Add(leftHand);
            rightUpperArm.Add(rightLowerArm);
            rightLowerArm.Add(rightHand);
            leftUpperLeg.Add(leftLowerLeg);
            leftLowerLeg.Add(leftFoot);
            rightUpperLeg.Add(rightLowerLeg);
            rightLowerLeg.Add(rightFoot);

            return new ShapeDefinition("Humanoid", root)
            {
                Rig = new ShapeRigDefinition
                {
                    Type = "humanoid/full",
                    Joints = new List<ShapeRigJoint>
                    {
                        Joint(ShapeRigRoles.Hips, hips),
                        Joint(ShapeRigRoles.Spine, spine),
                        Joint(ShapeRigRoles.Chest, chest),
                        Joint(ShapeRigRoles.Neck, neck),
                        Joint(ShapeRigRoles.Head, head),
                        Joint(ShapeRigRoles.LeftUpperArm, leftUpperArm),
                        Joint(ShapeRigRoles.LeftLowerArm, leftLowerArm),
                        Joint(ShapeRigRoles.LeftHand, leftHand),
                        Joint(ShapeRigRoles.RightUpperArm, rightUpperArm),
                        Joint(ShapeRigRoles.RightLowerArm, rightLowerArm),
                        Joint(ShapeRigRoles.RightHand, rightHand),
                        Joint(ShapeRigRoles.LeftUpperLeg, leftUpperLeg),
                        Joint(ShapeRigRoles.LeftLowerLeg, leftLowerLeg),
                        Joint(ShapeRigRoles.LeftFoot, leftFoot),
                        Joint(ShapeRigRoles.RightUpperLeg, rightUpperLeg),
                        Joint(ShapeRigRoles.RightLowerLeg, rightLowerLeg),
                        Joint(ShapeRigRoles.RightFoot, rightFoot)
                    }
                }
            };
        }

        private static ShapeNode Node(string id, string name, float x, float y, float z)
        {
            ShapeNode node = new(id, name, ShapeTypes.Group);
            node.Transform.Position = new(x, y, z);
            return node;
        }

        private static ShapeRigJoint Joint(string role, ShapeNode node)
        {
            return new(role, node.Id);
        }
    }
}
