using NUnit.Framework;
using SceneReplay.Unity.Editor;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies style-neutral agent discovery includes registered Low Poly catalogs.
    /// </summary>
    public sealed class LowPolyAutomationCatalogProviderTests
    {
        [Test]
        public void ProviderPublishesCapabilitiesAndTemplates()
        {
            SceneReplay.LowPoly.Editor.LowPolyAutomationCatalogProvider provider = new();

            Assert.That(provider.CreateCapabilities().Shapes, Is.Not.Empty);
            Assert.That(provider.CreateTemplates().Templates.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(ShapeAutomationCatalogRegistry.GetProviders().Count, Is.GreaterThanOrEqualTo(2));
        }
    }
}
