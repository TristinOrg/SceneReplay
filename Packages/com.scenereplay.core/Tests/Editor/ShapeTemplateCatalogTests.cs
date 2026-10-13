using System;
using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies typed semantic compilation, untyped dispatch, and cached template discovery.
    /// </summary>
    public sealed class ShapeTemplateCatalogTests
    {
        [Test]
        public void TypedTemplateCompilesSemanticSpecification()
        {
            ExampleTemplate      template      = new();
            ExampleSpecification specification = new() { Name = "Chair" };

            ShapeDefinition definition = template.Compile(specification);

            Assert.That(definition.Name, Is.EqualTo("Chair"));
            Assert.That(definition.Root.Type, Is.EqualTo(ShapeTypes.Group));
            Assert.That(template.SpecificationType, Is.EqualTo(typeof(ExampleSpecification)));
        }

        [Test]
        public void UntypedTemplateRejectsWrongSpecificationType()
        {
            IShapeTemplate template = new ExampleTemplate();

            Assert.Throws<ArgumentNullException>(() => template.Compile(null));
            Assert.Throws<ArgumentException>(() => template.Compile("wrong"));
        }

        [Test]
        public void CatalogCachesTemplatesAndCreatesDiscoveryDocument()
        {
            ExampleTemplate      template = new();
            ShapeTemplateCatalog catalog  = new(template);

            Assert.That(catalog.TryGet(template.Descriptor.Id, out IShapeTemplate resolved), Is.True);
            Assert.That(resolved, Is.SameAs(template));
            Assert.That(catalog.TryGet("missing/template", out _), Is.False);

            ShapeTemplateCatalogDocument document = catalog.CreateDocument("example/catalog");
            Assert.That(document.Schema, Is.EqualTo(ShapeTemplateCatalogDocument.CurrentSchema));
            Assert.That(document.Templates, Has.Count.EqualTo(1));
            Assert.That(document.Templates[0], Is.SameAs(template.Descriptor));
        }

        [Test]
        public void CatalogRejectsDuplicateTemplateIds()
        {
            Assert.Throws<ArgumentException>(() => new ShapeTemplateCatalog(
                new ExampleTemplate(),
                new ExampleTemplate()));
        }

        [Test]
        public void DescriptorPublishesValidatedBoundedParameters()
        {
            ShapeTemplateParameterDescriptor parameter = new("length", "Overall length.", 1f, 0.1f, 10f);
            ShapeTemplateDescriptor descriptor = new(
                "example/bounded", "Bounded.", "prop", "example/spec/1.0",
                new[] { ShapeTypes.Group }, new[] { parameter }, "example");

            Assert.That(descriptor.Parameters, Has.Count.EqualTo(1));
            Assert.That(descriptor.Parameters[0].Maximum, Is.EqualTo(10f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ShapeTemplateParameterDescriptor("bad", "Bad.", 2f, 0f, 1f));
        }

        private sealed class ExampleSpecification
        {
            public string Name { get; set; }
        }

        private sealed class ExampleTemplate : ShapeTemplate<ExampleSpecification>
        {
            private static readonly ShapeTemplateDescriptor TemplateDescriptor = new(
                "example/asset/1.0",
                "Builds an example asset.",
                "prop",
                "example.asset/1.0",
                new[] { ShapeTypes.Group },
                "example",
                "prop");

            public override ShapeTemplateDescriptor Descriptor => TemplateDescriptor;

            public override ShapeDefinition Compile(ExampleSpecification specification)
            {
                if (specification == null)
                    throw new ArgumentNullException(nameof(specification));

                return new(specification.Name, new ShapeNode("root", "Root", ShapeTypes.Group));
            }
        }
    }
}
