using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies style lookup and direct appearance override behavior.
    /// </summary>
    public sealed class ShapeStyleResolverTests
    {
        [Test]
        public void ResolverUsesSelectedStylePalette()
        {
            ShapeStyleDefinition style = new ShapeStyleDefinition("lowpoly/default");
            style.Palette.Set("primary", new ForgeColor(0f, 0f, 1f));

            ShapeDefinition definition = new ShapeDefinition(
                "Model",
                new ShapeNode("root", "Root", ShapeTypes.Group))
            {
                Style = style.Id
            };
            ShapeNode node = new ShapeNode("body", "Body", "example/cube");
            node.Appearance.ColorRole = "primary";

            ShapeStyleResolver resolver = new ShapeStyleResolver(new[] { style });

            Assert.That(resolver.TryResolveColor(definition, node, out ForgeColor color), Is.True);
            Assert.That(color, Is.EqualTo(new ForgeColor(0f, 0f, 1f)));
        }

        [Test]
        public void ContextPrefersDirectColorOverride()
        {
            ShapeDefinition definition = new ShapeDefinition(
                "Model",
                new ShapeNode("root", "Root", ShapeTypes.Group));
            ShapeNode node = new ShapeNode("body", "Body", "example/cube");
            node.Appearance.HasColorOverride = true;
            node.Appearance.Color            = new ForgeColor(1f, 0f, 0f);

            ShapeGenerationContext context = new ShapeGenerationContext(
                definition,
                new ConstantColorResolver(new ForgeColor(0f, 0f, 1f)));

            Assert.That(context.TryResolveColor(node, out ForgeColor color), Is.True);
            Assert.That(color, Is.EqualTo(new ForgeColor(1f, 0f, 0f)));
        }

        [Test]
        public void ResolverReturnsFalseForUnknownStyle()
        {
            ShapeDefinition definition = new ShapeDefinition(
                "Model",
                new ShapeNode("root", "Root", ShapeTypes.Group))
            {
                Style = "missing/style"
            };
            ShapeNode node = new ShapeNode("body", "Body", "example/cube");
            node.Appearance.ColorRole = "primary";

            ShapeStyleResolver resolver = new ShapeStyleResolver(new ShapeStyleDefinition[0]);

            Assert.That(resolver.TryResolveColor(definition, node, out _), Is.False);
        }

        [Test]
        public void ResolverInheritsPaletteAndPrefersDerivedOverrides()
        {
            ShapeStyleDefinition baseStyle = new("lowpoly/base");
            baseStyle.Palette
                .Set("primary", new(0f, 0f, 1f))
                .Set("accent", new(1f, 0f, 0f));

            ShapeStyleDefinition derivedStyle = new("lowpoly/night")
            {
                BaseStyle = baseStyle.Id
            };
            derivedStyle.Palette.Set("primary", new(0.1f, 0.1f, 0.2f));

            ShapeDefinition definition = new(
                "Model",
                new ShapeNode("root", "Root", ShapeTypes.Group))
            {
                Style = derivedStyle.Id
            };
            ShapeNode node = new("body", "Body", "example/cube");
            ShapeStyleResolver resolver = new(new[] { derivedStyle, baseStyle });

            node.Appearance.ColorRole = "primary";
            Assert.That(resolver.TryResolveColor(definition, node, out ForgeColor primary), Is.True);
            Assert.That(primary, Is.EqualTo(new ForgeColor(0.1f, 0.1f, 0.2f)));

            node.Appearance.ColorRole = "accent";
            Assert.That(resolver.TryResolveColor(definition, node, out ForgeColor accent), Is.True);
            Assert.That(accent, Is.EqualTo(new ForgeColor(1f, 0f, 0f)));
        }

        [Test]
        public void ResolverRejectsMissingOrCyclicBaseStyles()
        {
            ShapeStyleDefinition missing = new("missing-child")
            {
                BaseStyle = "missing-parent"
            };
            Assert.Throws<ShapeValidationException>(() => new ShapeStyleResolver(new[] { missing }));

            ShapeStyleDefinition first = new("first")
            {
                BaseStyle = "second"
            };
            ShapeStyleDefinition second = new("second")
            {
                BaseStyle = "first"
            };
            Assert.Throws<ShapeValidationException>(() => new ShapeStyleResolver(new[] { first, second }));
        }

        private sealed class ConstantColorResolver : IShapeStyleResolver
        {
            private readonly ForgeColor color;

            public ConstantColorResolver(ForgeColor color)
            {
                this.color = color;
            }

            public bool TryResolveColor(ShapeDefinition definition, ShapeNode node, out ForgeColor color)
            {
                color = this.color;
                return true;
            }
        }
    }
}
