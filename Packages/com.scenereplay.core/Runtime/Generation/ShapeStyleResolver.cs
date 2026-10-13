using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Resolves shape colors from explicitly supplied serializable style definitions.
    /// </summary>
    public sealed class ShapeStyleResolver : IShapeStyleResolver
    {
        private readonly Dictionary<string, ShapeStyleDefinition> styles   = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ResolvedPalette>      palettes = new(StringComparer.Ordinal);
        private readonly ShapeStyleDefinitionValidator            validator  = new();

        /// <summary>
        /// Initializes a resolver with its available style definitions.
        /// </summary>
        public ShapeStyleResolver(IEnumerable<ShapeStyleDefinition> styles)
        {
            if (styles == null)
                throw new ArgumentNullException(nameof(styles));

            foreach (ShapeStyleDefinition style in styles)
            {
                if (style == null)
                    throw new ArgumentException("Styles cannot contain null entries.", nameof(styles));

                validator.Validate(style);

                if (!this.styles.TryAdd(style.Id, style))
                    throw new ArgumentException($"Duplicate style ID '{style.Id}'.", nameof(styles));
            }

            RebuildPalettes();
        }

        /// <summary>
        /// Adds or replaces a validated style without rebuilding the resolver.
        /// </summary>
        public void Set(ShapeStyleDefinition style)
        {
            if (style == null)
                throw new ArgumentNullException(nameof(style));

            validator.Validate(style);
            bool                 hadPrevious = styles.TryGetValue(style.Id, out ShapeStyleDefinition previous);
            styles[style.Id]                 = style;

            try
            {
                RebuildPalettes();
            }
            catch
            {
                if (hadPrevious)
                    styles[style.Id] = previous;
                else
                    styles.Remove(style.Id);

                RebuildPalettes();
                throw;
            }
        }

        /// <inheritdoc />
        public bool TryResolveColor(ShapeDefinition definition, ShapeNode node, out ForgeColor color)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (palettes.TryGetValue(definition.Style, out ResolvedPalette palette))
                return palette.TryGetColor(node.Appearance.ColorRole, out color);

            color = default;
            return false;
        }

        private void RebuildPalettes()
        {
            palettes.Clear();
            foreach (string styleId in styles.Keys)
                ResolvePalette(styleId, new HashSet<string>(StringComparer.Ordinal));
        }

        private ResolvedPalette ResolvePalette(string styleId, HashSet<string> chain)
        {
            if (palettes.TryGetValue(styleId, out ResolvedPalette resolved))
                return resolved;

            if (!styles.TryGetValue(styleId, out ShapeStyleDefinition style))
                throw new ShapeValidationException($"Inherited style '{styleId}' is not registered.");

            if (!chain.Add(styleId))
                throw new ShapeValidationException($"Style inheritance contains a cycle at '{styleId}'.");

            ResolvedPalette palette = string.IsNullOrWhiteSpace(style.BaseStyle)
                ? new ResolvedPalette()
                : new ResolvedPalette(ResolvePalette(style.BaseStyle, chain));

            foreach (ShapePaletteEntry entry in style.Palette.Entries)
                palette.Set(entry.Role, entry.Color);

            chain.Remove(styleId);
            palettes.Add(styleId, palette);
            return palette;
        }

        private sealed class ResolvedPalette
        {
            private readonly Dictionary<string, ForgeColor> colors;

            public ResolvedPalette()
            {
                colors = new(StringComparer.Ordinal);
            }

            public ResolvedPalette(ResolvedPalette source)
            {
                colors = new(source.colors, StringComparer.Ordinal);
            }

            public void Set(string role, ForgeColor color)
            {
                colors[role] = color;
            }

            public bool TryGetColor(string role, out ForgeColor color)
            {
                return colors.TryGetValue(role, out color);
            }
        }
    }
}
