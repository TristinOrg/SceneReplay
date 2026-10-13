using NUnit.Framework;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies a non-character furniture model through the complete generation pipeline.
    /// </summary>
    public sealed class LowPolyWorkbenchPresetTests
    {
        private GameObject generatedRoot;
        private GameObject secondGeneratedRoot;

        [TearDown]
        public void TearDown()
        {
            if (generatedRoot != null)
                Object.DestroyImmediate(generatedRoot);

            if (secondGeneratedRoot != null)
                Object.DestroyImmediate(secondGeneratedRoot);
        }

        [Test]
        public void GenerateCreatesColoredEditableFurnitureHierarchy()
        {
            ShapeDefinition          definition = LowPolyWorkbenchPreset.CreateDefinition();
            ShapeStyleDefinition     style      = LowPolyWorkbenchPreset.CreateStyle();
            ShapeStyleResolver       resolver   = new(new[] { style });
            UnityShapeModelGenerator generator  = new(
                new IUnityShapeGenerator[] { new LowPolyPrimitiveGenerator() },
                resolver);

            generatedRoot = generator.Generate(definition);
            secondGeneratedRoot = generator.Generate(definition);

            Assert.That(generatedRoot.name, Is.EqualTo("Inventor Workbench"));
            Assert.That(generatedRoot.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(37));
            Assert.That(generatedRoot.transform.Find("Tool Board/Upper Shelf"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Task Lamp/Lamp Bulb"), Is.Not.Null);
            Assert.That(generatedRoot.transform.Find("Drawer/Drawer Face").localPosition.z, Is.LessThan(0f));
            Assert.That(generatedRoot.transform.Find("Tool Board").localPosition.z, Is.GreaterThan(0f));

            Renderer renderer       = generatedRoot.transform.Find("Worktop").GetComponent<Renderer>();
            Renderer secondRenderer = secondGeneratedRoot.transform.Find("Worktop").GetComponent<Renderer>();
            Color    color          = renderer.sharedMaterial.color;

            Assert.That(color.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(color.g, Is.EqualTo(0.24f).Within(0.0001f));
            Assert.That(color.b, Is.EqualTo(0.07f).Within(0.0001f));
            Assert.That(color.a, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(renderer.HasPropertyBlock(), Is.False);
            Assert.That(renderer.sharedMaterial, Is.SameAs(secondRenderer.sharedMaterial));
            Assert.That(generatedRoot.GetComponentsInChildren<UnityShapeAppearanceManifest>().Length, Is.EqualTo(1));
            Assert.That(generatedRoot.GetComponent<UnityShapeAppearanceManifest>().BindingCount, Is.EqualTo(36));

            renderer.sharedMaterial = null;
            generatedRoot.GetComponent<UnityShapeAppearanceManifest>().Apply();
            Assert.That(renderer.sharedMaterial, Is.SameAs(secondRenderer.sharedMaterial));
        }
    }
}
