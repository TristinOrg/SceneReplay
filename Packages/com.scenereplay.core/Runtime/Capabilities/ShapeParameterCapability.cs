using System;

namespace SceneReplay
{
    /// <summary>
    /// Describes one numeric parameter accepted by a shape type.
    /// </summary>
    [Serializable]
    public sealed class ShapeParameterCapability
    {
        /// <summary>Initializes an immutable numeric parameter description.</summary>
        public ShapeParameterCapability(
            string name,
            string summary,
            float  defaultValue,
            float? minimum          = null,
            float? maximum          = null,
            bool   wholeNumber      = false,
            bool   minimumExclusive = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A capability parameter requires a name.", nameof(name));

            if (!IsFinite(defaultValue) ||
                minimum.HasValue && !IsFinite(minimum.Value) ||
                maximum.HasValue && !IsFinite(maximum.Value))
                throw new ArgumentOutOfRangeException(nameof(defaultValue), "Capability ranges must be finite.");

            if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value)
                throw new ArgumentException("A capability minimum cannot exceed its maximum.");

            if (minimumExclusive && !minimum.HasValue)
                throw new ArgumentException("An exclusive minimum requires a minimum value.");

            if (minimum.HasValue &&
                (minimumExclusive ? defaultValue <= minimum.Value : defaultValue < minimum.Value) ||
                maximum.HasValue && defaultValue > maximum.Value)
                throw new ArgumentOutOfRangeException(nameof(defaultValue));

            if (wholeNumber && (defaultValue != Math.Truncate(defaultValue) ||
                                minimum.HasValue && minimum.Value != Math.Truncate(minimum.Value) ||
                                maximum.HasValue && maximum.Value != Math.Truncate(maximum.Value)))
                throw new ArgumentException("Whole-number capability ranges require integer values.");

            Name             = name;
            Summary          = summary ?? string.Empty;
            DefaultValue     = defaultValue;
            Minimum          = minimum;
            Maximum          = maximum;
            WholeNumber      = wholeNumber;
            MinimumExclusive = minimumExclusive;
        }

        /// <summary>Gets the exact key stored in <see cref="ShapeNode.Parameters"/>.</summary>
        public string Name { get; }

        /// <summary>Gets the concise authoring guidance.</summary>
        public string Summary { get; }

        /// <summary>Gets the value used when the parameter is omitted.</summary>
        public float DefaultValue { get; }

        /// <summary>Gets the inclusive minimum, or null when only semantic constraints apply.</summary>
        public float? Minimum { get; }

        /// <summary>Gets the inclusive maximum, or null when unbounded.</summary>
        public float? Maximum { get; }

        /// <summary>Gets whether the value must be an integer.</summary>
        public bool WholeNumber { get; }

        /// <summary>Gets whether the declared minimum is excluded from the valid range.</summary>
        public bool MinimumExclusive { get; }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
