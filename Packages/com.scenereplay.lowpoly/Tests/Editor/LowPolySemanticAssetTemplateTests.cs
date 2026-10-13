using NUnit.Framework;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies the official ordered semantic asset template library.
    /// </summary>
    public sealed class LowPolySemanticAssetTemplateTests
    {
        [Test]
        public void CatalogPublishesEveryRoadmapCategoryWithBoundedControls()
        {
            string[] ids =
            {
                "lowpoly/hair/1.0",
                "lowpoly/armor/1.0",
                "lowpoly/weapon/1.0",
                "lowpoly/building/1.0",
                "lowpoly/vehicle/1.0"
            };
            foreach (string id in ids)
            {
                Assert.That(LowPolyShapeTemplateCatalog.Instance.TryGet(id, out IShapeTemplate template), Is.True);
                Assert.That(template.Descriptor.Parameters, Has.Count.EqualTo(4));
            }
        }

        [TestCaseSource(nameof(Templates))]
        public void TemplatePublishesStableDefinitionInventoryAndQualityResources(
            LowPolySemanticAssetTemplate template)
        {
            LowPolySemanticAssetSpecification specification = new() { Name = template.Descriptor.Category };

            ShapeDefinition definition      = template.Compile(specification);
            ShapeDetailInventory inventory = template.CreateDetailInventory(specification);
            ShapeQualityPolicy policy       = template.CreateQualityPolicy(specification);

            Assert.That(definition.Root.Children, Is.Not.Empty);
            Assert.That(inventory.Details, Has.Count.EqualTo(definition.Root.Children.Count));
            Assert.That(policy.RequiredNodeIds, Has.Count.EqualTo(definition.Root.Children.Count + 1));
            Assert.That(new ShapeDefinitionValidator().Analyze(definition).IsValid, Is.True);
            Assert.That(new ShapeDetailInventoryValidator().Analyze(inventory).IsValid, Is.True);
        }

        private static LowPolySemanticAssetTemplate[] Templates() => new LowPolySemanticAssetTemplate[]
        {
            LowPolyHairTemplate.Instance,
            LowPolyArmorTemplate.Instance,
            LowPolyWeaponTemplate.Instance,
            LowPolyBuildingTemplate.Instance,
            LowPolyVehicleTemplate.Instance
        };
    }
}
