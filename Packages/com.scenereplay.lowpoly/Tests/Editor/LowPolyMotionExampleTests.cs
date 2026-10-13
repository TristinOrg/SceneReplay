using NUnit.Framework;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies Low Poly motion examples through engine-neutral transform targets.
    /// </summary>
    public sealed class LowPolyMotionExampleTests
    {
        private GameObject generatedRoot;

        [TearDown]
        public void TearDown()
        {
            if (generatedRoot != null)
                Object.DestroyImmediate(generatedRoot);
        }

        [Test]
        public void RobotExampleWalksForwardWithOpposingLimbs()
        {
            generatedRoot = Generate(
                LowPolyRobotPreset.CreateDefinition(),
                LowPolyRobotPreset.CreateStyle());
            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            model.TryGetTarget("robot.arm.left.shoulder.pivot", out IShapeTransformTarget shoulder);
            model.TryGetTarget("robot.leg.left.hip.pivot", out IShapeTransformTarget leftHip);
            model.TryGetTarget("robot.leg.right.hip.pivot", out IShapeTransformTarget rightHip);
            model.TryGetTarget("robot", out IShapeTransformTarget root);
            ForgeVector3 initialPosition = root.LocalPosition;

            LowPolyMotionExample motion = generatedRoot.AddComponent<LowPolyMotionExample>();
            motion.Configure(LowPolyMotionPreset.RobotShowcase);
            motion.Evaluate(0.25f);

            Assert.That(root.LocalPosition.Z, Is.LessThan(initialPosition.Z));
            Assert.That(shoulder.LocalEulerAngles.X, Is.GreaterThan(180f));
            Assert.That(leftHip.LocalEulerAngles.X, Is.GreaterThan(0f));
            Assert.That(rightHip.LocalEulerAngles.X, Is.GreaterThan(180f));
        }

        [Test]
        public void WorkbenchExampleOpensDrawerAndMovesLamp()
        {
            generatedRoot = Generate(
                LowPolyWorkbenchPreset.CreateDefinition(),
                LowPolyWorkbenchPreset.CreateStyle());
            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            model.TryGetTarget("workbench.drawer", out IShapeTransformTarget drawer);
            model.TryGetTarget("workbench.lamp", out IShapeTransformTarget lamp);
            ForgeVector3 initialDrawerPosition = drawer.LocalPosition;
            ForgeVector3 initialLampRotation   = lamp.LocalEulerAngles;

            LowPolyMotionExample motion = generatedRoot.AddComponent<LowPolyMotionExample>();
            motion.Configure(LowPolyMotionPreset.WorkbenchShowcase);
            motion.Evaluate(0.25f);

            Assert.That(drawer.LocalPosition.Z, Is.LessThan(initialDrawerPosition.Z));
            Assert.That(
                Mathf.Abs(lamp.LocalEulerAngles.Y - initialLampRotation.Y),
                Is.GreaterThan(0.01f));
        }

        private static GameObject Generate(
            ShapeDefinition      definition,
            ShapeStyleDefinition style)
        {
            ShapeStyleResolver       resolver  = new(new[] { style });
            UnityShapeModelGenerator generator = new(
                new IUnityShapeGenerator[] { new LowPolyPrimitiveGenerator() },
                resolver);
            return generator.Generate(definition);
        }
    }
}
