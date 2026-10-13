using System;

namespace SceneReplay
{
    /// <summary>
    /// Carries immutable model-level information through a generation pass.
    /// </summary>
    public sealed class ShapeGenerationContext
    {
        /// <summary>
        /// Initializes a generation context for a model definition.
        /// </summary>
        public ShapeGenerationContext(
            ShapeDefinition      definition,
            IShapeStyleResolver  styleResolver = null)
        {
            Definition    = definition ?? throw new ArgumentNullException(nameof(definition));
            StyleResolver = styleResolver;
        }

        /// <summary>
        /// Gets the model being generated.
        /// </summary>
        public ShapeDefinition Definition { get; }

        /// <summary>
        /// Gets the optional style resolver used by this generation pass.
        /// </summary>
        public IShapeStyleResolver StyleResolver { get; }

        /// <summary>
        /// Resolves a direct color override or delegates to the configured style resolver.
        /// </summary>
        public bool TryResolveColor(ShapeNode node, out ForgeColor color)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (node.Appearance.HasColorOverride)
            {
                color = node.Appearance.Color;
                return true;
            }

            if (StyleResolver != null)
                return StyleResolver.TryResolveColor(Definition, node, out color);

            color = default;
            return false;
        }
    }
}
