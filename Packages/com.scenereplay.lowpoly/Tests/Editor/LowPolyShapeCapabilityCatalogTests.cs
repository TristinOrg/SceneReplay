using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SceneReplay.Unity;

namespace SceneReplay.LowPoly.Tests
{
    /// <summary>
    /// Verifies that Low Poly capability metadata stays complete and matches generator constraints.
    /// </summary>
    public sealed class LowPolyShapeCapabilityCatalogTests
    {
        [Test]
        public void CatalogDescribesEveryPublicLowPolyShapeType()
        {
            LowPolyShapeCapabilityCatalog catalog = LowPolyShapeCapabilityCatalog.Instance;
            HashSet<string>                types   = new();

            foreach (ShapeCapability capability in catalog.Shapes)
                Assert.That(types.Add(capability.Type), Is.True, $"Duplicate capability '{capability.Type}'.");

            FieldInfo[] fields = typeof(LowPolyShapeTypes).GetFields(
                BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                string type = (string)field.GetRawConstantValue();
                Assert.That(catalog.TryGet(type, out _), Is.True, $"Missing capability '{type}'.");
            }

            Assert.That(types, Has.Count.EqualTo(fields.Length));
        }

        [Test]
        public void CatalogExposesProceduralInputAndQualityLimits()
        {
            LowPolyShapeCapabilityCatalog catalog = LowPolyShapeCapabilityCatalog.Instance;

            Assert.That(catalog.TryGet(LowPolyShapeTypes.ProfileSweep, out ShapeCapability sweep), Is.True);
            Assert.That(sweep.MinimumProfilePoints, Is.EqualTo(3));
            Assert.That(sweep.MinimumPathPoints, Is.EqualTo(2));
            Assert.That(sweep.Cost, Is.EqualTo(ShapeGenerationCost.InputScaled));

            Assert.That(catalog.TryGet(LowPolyShapeTypes.ProfileCage, out ShapeCapability cage), Is.True);
            Assert.That(cage.MinimumProfileCageSections, Is.EqualTo(2));
            Assert.That(cage.Cost, Is.EqualTo(ShapeGenerationCost.InputScaled));
            ShapeParameterCapability cageSubdivisions = FindParameter(
                cage,
                LowPolyShapeParameters.CageSubdivisions);
            Assert.That(cageSubdivisions.Minimum, Is.EqualTo(0f));
            Assert.That(cageSubdivisions.Maximum, Is.EqualTo(8f));

            Assert.That(catalog.TryGet(LowPolyShapeTypes.LatheProfile, out ShapeCapability lathe), Is.True);
            ShapeParameterCapability segments = FindParameter(lathe, LowPolyShapeParameters.RadialSegments);
            Assert.That(segments.Minimum, Is.EqualTo(3f));
            Assert.That(segments.Maximum, Is.EqualTo(64f));
            Assert.That(segments.WholeNumber, Is.True);
        }

        [Test]
        public void CapabilityDocumentSerializesForExternalAuthoringTools()
        {
            ShapeCapabilityCatalogDocument document   = LowPolyShapeCapabilityCatalog.Instance.CreateDocument();
            ShapeJsonSerializer             serializer = new();

            string json = serializer.Serialize(document);

            Assert.That(json, Does.Contain("\"schema\":\"scenereplay.capabilities/1.0\""));
            Assert.That(json, Does.Contain("\"id\":\"lowpoly/official\""));
            Assert.That(json, Does.Contain("\"type\":\"lowpoly/profile-loft\""));
            Assert.That(json, Does.Contain("\"minimumProfileSections\":2"));
            Assert.That(json, Does.Contain("\"minimumProfileCageSections\":2"));
        }

        private static ShapeParameterCapability FindParameter(ShapeCapability capability, string name)
        {
            foreach (ShapeParameterCapability parameter in capability.Parameters)
            {
                if (parameter.Name == name)
                    return parameter;
            }

            Assert.Fail($"Capability '{capability.Type}' has no parameter '{name}'.");
            return null;
        }
    }
}
