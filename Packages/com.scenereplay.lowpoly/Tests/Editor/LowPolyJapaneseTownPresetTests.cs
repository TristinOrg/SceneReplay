using NUnit.Framework;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies the Japanese town environment through the complete generation pipeline.
    /// </summary>
    public sealed class LowPolyJapaneseTownPresetTests
    {
        private GameObject generatedRoot;

        [TearDown]
        public void TearDown()
        {
            if (generatedRoot != null)
                Object.DestroyImmediate(generatedRoot);
        }

        [Test]
        public void GenerateCreatesCompleteTownEnvironment()
        {
            ShapeDefinition          definition = LowPolyJapaneseTownPreset.CreateDefinition();
            ShapeStyleDefinition     style      = LowPolyJapaneseTownPreset.CreateStyle();
            ShapeStyleResolver       resolver   = new(new[] { style });
            UnityShapeModelGenerator generator  = new(
                new IUnityShapeGenerator[] { new LowPolyPrimitiveGenerator() },
                resolver);

            generatedRoot = generator.Generate(definition);

            Assert.That(generatedRoot.name, Is.EqualTo("Japanese Town"));
            Assert.That(generatedRoot.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(95));
            Assert.That(generatedRoot.transform.Find("Ramen Shop/Tiled Roof/Front Roof Slope"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Tea House/Sliding Door"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Shrine Torii/Upper Crossbar"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Street Food Stall/Counter"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Street Bench/Bench Seat"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Cherry Tree/Center Blossoms"), Is.Not.Null);

            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            Assert.That(model.TryGetTarget("town.building.ramen.door", out _), Is.True);
            Assert.That(model.TryGetTarget("town.stall.counter", out _), Is.True);
            Assert.That(model.TryGetTarget("town.bench", out _), Is.True);
            Assert.That(model.TryGetTarget("town.torii", out _), Is.True);
        }
    }
}
