using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Validates engine-agnostic SceneReplay style definitions.
    /// </summary>
    public sealed class ShapeStyleDefinitionValidator
    {
        /// <summary>
        /// Validates schema compatibility, style identity, and palette roles.
        /// </summary>
        public void Validate(ShapeStyleDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (!string.Equals(definition.Schema, ShapeStyleDefinition.CurrentSchema, StringComparison.Ordinal))
                throw new ShapeValidationException($"Unsupported style schema '{definition.Schema}'.");

            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new ShapeValidationException("Every style requires a stable ID.");

            if (string.Equals(definition.Id, definition.BaseStyle, StringComparison.Ordinal))
                throw new ShapeValidationException($"Style '{definition.Id}' cannot inherit from itself.");

            if (definition.Palette == null)
                throw new ShapeValidationException($"Style '{definition.Id}' requires a palette.");

            HashSet<string> roles = new HashSet<string>(StringComparer.Ordinal);
            foreach (ShapePaletteEntry entry in definition.Palette.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Role))
                    throw new ShapeValidationException($"Style '{definition.Id}' contains an invalid palette role.");

                if (!roles.Add(entry.Role))
                    throw new ShapeValidationException($"Style '{definition.Id}' contains duplicate role '{entry.Role}'.");

                ForgeColorValidator.Validate(entry.Color, $"Style '{definition.Id}' role '{entry.Role}'");
            }
        }
    }
}
