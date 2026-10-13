using System;

namespace SceneReplay
{
    /// <summary>
    /// Describes one readable, bounded numeric control accepted by a semantic template.
    /// </summary>
    [Serializable]
    public sealed class ShapeTemplateParameterDescriptor
    {
        /// <summary>Initializes immutable template-parameter metadata.</summary>
        public ShapeTemplateParameterDescriptor(
            string name,
            string summary,
            float  defaultValue,
            float  minimum,
            float  maximum,
            bool   wholeNumber = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A template parameter requires a name.", nameof(name));
            if (!IsFinite(defaultValue) || !IsFinite(minimum) || !IsFinite(maximum) || minimum > maximum)
                throw new ArgumentOutOfRangeException(nameof(minimum), "Template parameter bounds must be finite and ordered.");
            if (defaultValue < minimum || defaultValue > maximum)
                throw new ArgumentOutOfRangeException(nameof(defaultValue));

            Name         = name;
            Summary      = summary ?? string.Empty;
            DefaultValue = defaultValue;
            Minimum      = minimum;
            Maximum      = maximum;
            WholeNumber  = wholeNumber;
        }

        /// <summary>Gets the stable parameter name.</summary>
        public string Name { get; }
        /// <summary>Gets its concise semantic meaning.</summary>
        public string Summary { get; }
        /// <summary>Gets its default value.</summary>
        public float DefaultValue { get; }
        /// <summary>Gets its inclusive minimum.</summary>
        public float Minimum { get; }
        /// <summary>Gets its inclusive maximum.</summary>
        public float Maximum { get; }
        /// <summary>Gets whether values must be whole numbers.</summary>
        public bool WholeNumber { get; }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
