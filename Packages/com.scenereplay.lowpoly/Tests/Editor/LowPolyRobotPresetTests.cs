using NUnit.Framework;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies the modular robot preset and its transform-ready pivot hierarchy.
    /// </summary>
    public sealed class LowPolyRobotPresetTests
    {
        private GameObject generatedRoot;

        [TearDown]
        public void TearDown()
        {
            if (generatedRoot != null)
                Object.DestroyImmediate(generatedRoot);
        }

        [Test]
        public void DefinitionKeepsMotionTargetsSeparateFromGeometry()
        {
            ShapeDefinition definition = LowPolyRobotPreset.CreateDefinition();

            ShapeNode leftShoulder = FindNode(definition.Root, "robot.arm.left.shoulder.pivot");
            ShapeNode leftArm      = FindNode(definition.Root, "robot.arm.left.upper");
            ShapeRigIndex rig      = new(definition.Rig);

            Assert.That(leftShoulder, Is.Not.Null);
            Assert.That(leftArm, Is.Not.Null);
            Assert.That(leftShoulder.Type, Is.EqualTo(ShapeTypes.Group));
            Assert.That(FindNode(leftShoulder, leftArm.Id), Is.SameAs(leftArm));
            Assert.That(leftArm.Type, Is.EqualTo(LowPolyShapeTypes.LatheProfile));
            Assert.That(rig.TryGetNodeId(ShapeRigRoles.LeftElbow, out string elbowId), Is.True);
            Assert.That(elbowId, Is.EqualTo("robot.arm.left.elbow.pivot"));
            Assert.That(rig.ConstrainRotationOffset(ShapeRigRoles.LeftElbow, new(160f, 5f, 5f)),
                Is.EqualTo(new ForgeVector3(125f, 0f, 0f)));
        }

        [Test]
        public void GenerateCreatesColoredRobotWithEditablePivots()
        {
            ShapeDefinition          definition = LowPolyRobotPreset.CreateDefinition();
            ShapeStyleDefinition     style      = LowPolyRobotPreset.CreateStyle();
            ShapeStyleResolver       resolver   = new(new[] { style });
            UnityShapeModelGenerator generator  = new(
                new IUnityShapeGenerator[] { new LowPolyPrimitiveGenerator() },
                resolver);

            generatedRoot = generator.Generate(definition);

            Assert.That(generatedRoot.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(37));
            Assert.That(generatedRoot.transform.Find("Left Shoulder Pivot/Left Elbow Pivot/Left Hand"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Right Hip Pivot/Right Knee Pivot/Right Foot"), Is.Not.Null);

            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            Assert.That(model.TryGetTarget("robot.arm.left.elbow.pivot", out _), Is.True);
        }

        private static ShapeNode FindNode(ShapeNode node, string id)
        {
            if (node.Id == id)
                return node;

            foreach (ShapeNode child in node.Children)
            {
                ShapeNode result = FindNode(child, id);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
