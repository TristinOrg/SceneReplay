using NUnit.Framework;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies the modern scramble-crossing preset through the complete generation pipeline.
    /// </summary>
    public sealed class LowPolyShibuyaCrossingPresetTests
    {
        private GameObject generatedRoot;

        [TearDown]
        public void TearDown()
        {
            if (generatedRoot != null)
                Object.DestroyImmediate(generatedRoot);
        }

        [Test]
        public void GenerateCreatesDenseModernCrossingEnvironment()
        {
            ShapeDefinition      definition = LowPolyShibuyaCrossingPreset.CreateDefinition();
            ShapeStyleDefinition style      = LowPolyShibuyaCrossingPreset.CreateStyle();
            ShapeStyleResolver   resolver   = new(new[] { style });
            UnityShapeModelGenerator generator = new(
                new IUnityShapeGenerator[] { new LowPolyPrimitiveGenerator() },
                resolver);

            generatedRoot = generator.Generate(definition);

            Assert.That(generatedRoot.name, Is.EqualTo("Shibuya Crossing"));
            Assert.That(generatedRoot.GetComponentsInChildren<MeshRenderer>().Length, Is.GreaterThan(120));
            Assert.That(generatedRoot.transform.Find("Scramble Crosswalks"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Northwest Media Tower/Large Digital Screen"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("southeast Traffic Signal/Walk Signal"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Crossing Crowd/Pedestrian 1/Coat"), Is.Not.Null);

            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            Assert.That(model.TryGetTarget("shibuya.crosswalk.diagonal-ne.0", out _), Is.True);
            Assert.That(model.TryGetTarget("shibuya.building.northwest.screen", out _), Is.True);
            Assert.That(model.TryGetTarget("shibuya.signal.southeast.light", out _), Is.True);
            Assert.That(model.TryGetTarget("shibuya.crowd.15", out _), Is.True);
        }
    }
}
