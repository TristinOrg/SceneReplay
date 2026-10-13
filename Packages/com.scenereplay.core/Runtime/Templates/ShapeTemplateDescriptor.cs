using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Describes one semantic template for discovery by developers and external AI.
    /// </summary>
    [Serializable]
    public sealed class ShapeTemplateDescriptor
    {
        private readonly IReadOnlyList<string> requiredShapeTypes;
        private readonly IReadOnlyList<ShapeTemplateParameterDescriptor> parameters;
        private readonly IReadOnlyList<string> tags;

        /// <summary>Initializes immutable semantic-template metadata.</summary>
        public ShapeTemplateDescriptor(
            string   id,
            string   summary,
            string   category,
            string   specificationSchema,
            string[] requiredShapeTypes,
            params string[] tags)
            : this(id, summary, category, specificationSchema, requiredShapeTypes, null, tags)
        {
        }

        /// <summary>Initializes immutable semantic-template metadata with bounded controls.</summary>
        public ShapeTemplateDescriptor(
            string   id,
            string   summary,
            string   category,
            string   specificationSchema,
            string[] requiredShapeTypes,
            ShapeTemplateParameterDescriptor[] parameters,
            params string[] tags)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A shape template requires a stable ID.", nameof(id));

            if (string.IsNullOrWhiteSpace(specificationSchema))
                throw new ArgumentException("A shape template requires a specification schema.", nameof(specificationSchema));

            Id                  = id;
            Summary             = summary ?? string.Empty;
            Category            = category ?? string.Empty;
            SpecificationSchema = specificationSchema;
            this.requiredShapeTypes = CopyDistinct(requiredShapeTypes, nameof(requiredShapeTypes));
            this.parameters         = CopyParameters(parameters);
            this.tags               = CopyDistinct(tags, nameof(tags));
        }

        /// <summary>Gets the stable template identifier.</summary>
        public string Id { get; }

        /// <summary>Gets the concise semantic purpose.</summary>
        public string Summary { get; }

        /// <summary>Gets the broad asset category, such as character or building.</summary>
        public string Category { get; }

        /// <summary>Gets the versioned schema identifier for the accepted specification.</summary>
        public string SpecificationSchema { get; }

        /// <summary>Gets the backend shape types required to compile this template.</summary>
        public IReadOnlyList<string> RequiredShapeTypes => requiredShapeTypes;

        /// <summary>Gets bounded numeric controls exposed for tools and LLM authoring.</summary>
        public IReadOnlyList<ShapeTemplateParameterDescriptor> Parameters => parameters;

        /// <summary>Gets compact discovery terms intended for tools and LLM selection.</summary>
        public IReadOnlyList<string> Tags => tags;

        private static IReadOnlyList<string> CopyDistinct(string[] values, string parameterName)
        {
            if (values == null || values.Length == 0)
                return Array.AsReadOnly(Array.Empty<string>());

            HashSet<string> seen = new(StringComparer.Ordinal);
            string[]        copy = new string[values.Length];
            for (int index = 0; index < values.Length; index++)
            {
                string value = values[index];
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Template metadata cannot contain empty values.", parameterName);

                if (!seen.Add(value))
                    throw new ArgumentException($"Template metadata contains duplicate value '{value}'.", parameterName);

                copy[index] = value;
            }

            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<ShapeTemplateParameterDescriptor> CopyParameters(
            ShapeTemplateParameterDescriptor[] values)
        {
            if (values == null || values.Length == 0)
                return Array.AsReadOnly(Array.Empty<ShapeTemplateParameterDescriptor>());

            HashSet<string> seen = new(StringComparer.Ordinal);
            ShapeTemplateParameterDescriptor[] copy = new ShapeTemplateParameterDescriptor[values.Length];
            for (int index = 0; index < values.Length; index++)
            {
                ShapeTemplateParameterDescriptor value = values[index] ??
                    throw new ArgumentException("Template parameters cannot contain null values.", nameof(values));
                if (!seen.Add(value.Name))
                    throw new ArgumentException($"Template parameters contain duplicate name '{value.Name}'.", nameof(values));
                copy[index] = value;
            }
            return Array.AsReadOnly(copy);
        }
    }
}
