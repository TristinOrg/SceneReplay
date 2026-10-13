using System;
using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies engine-agnostic capability metadata and Core-owned shape discovery.
    /// </summary>
    public sealed class ShapeCapabilityCatalogTests
    {
        [Test]
        public void CoreCatalogDescribesHierarchyOnlyGroup()
        {
            IShapeCapabilityCatalog catalog = CoreShapeCapabilityCatalog.Instance;

            Assert.That(catalog.Shapes, Has.Count.EqualTo(1));
            Assert.That(catalog.TryGet(ShapeTypes.Group, out ShapeCapability capability), Is.True);
            Assert.That(capability.Cost, Is.EqualTo(ShapeGenerationCost.Constant));
            Assert.That(capability.BestFor, Does.Contain("animation pivots"));
            Assert.That(catalog.TryGet("unknown/shape", out _), Is.False);
        }

        [Test]
        public void ParameterCapabilityPreservesRangeSemantics()
        {
            ShapeParameterCapability parameter = new(
                "depth",
                "Positive depth.",
                0.2f,
                0f,
                2f,
                minimumExclusive: true);

            Assert.That(parameter.Minimum, Is.EqualTo(0f));
            Assert.That(parameter.Maximum, Is.EqualTo(2f));
            Assert.That(parameter.MinimumExclusive, Is.True);
            Assert.That(parameter.WholeNumber, Is.False);
        }

        [Test]
        public void CapabilityMetadataRejectsInvalidRangesAndDuplicateParameters()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShapeParameterCapability(
                "quality", "Quality.", 9f, 0f, 4f, true));

            ShapeParameterCapability parameter = new("quality", "Quality.", 1f, 0f, 4f, true);
            Assert.Throws<ArgumentException>(() => new ShapeCapability(
                "example/shape",
                "Example.",
                "Tests.",
                string.Empty,
                ShapeGenerationCost.Parameterized,
                parameters: new[] { parameter, parameter }));
        }
    }
}
