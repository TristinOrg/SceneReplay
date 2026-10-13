using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Describes how authors and external tools should use one shape type.
    /// </summary>
    [Serializable]
    public sealed class ShapeCapability
    {
        private readonly IReadOnlyList<ShapeParameterCapability> parameters;

        /// <summary>Initializes an immutable shape capability description.</summary>
        public ShapeCapability(
            string                     type,
            string                     summary,
            string                     bestFor,
            string                     limitations,
            ShapeGenerationCost        cost,
            int                        minimumProfilePoints   = 0,
            int                        minimumPathPoints      = 0,
            int                        minimumProfileSections = 0,
            params ShapeParameterCapability[] parameters)
            : this(
                type,
                summary,
                bestFor,
                limitations,
                cost,
                minimumProfilePoints,
                minimumPathPoints,
                minimumProfileSections,
                0,
                parameters)
        {
        }

        /// <summary>Initializes a capability that consumes independently authored profile-cage sections.</summary>
        public ShapeCapability(
            string                     type,
            string                     summary,
            string                     bestFor,
            string                     limitations,
            ShapeGenerationCost        cost,
            int                        minimumProfilePoints,
            int                        minimumPathPoints,
            int                        minimumProfileSections,
            int                        minimumProfileCageSections,
            params ShapeParameterCapability[] parameters)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("A shape capability requires a type.", nameof(type));

            if (minimumProfilePoints < 0 || minimumPathPoints < 0 || minimumProfileSections < 0 ||
                minimumProfileCageSections < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumProfilePoints));

            if (!Enum.IsDefined(typeof(ShapeGenerationCost), cost))
                throw new ArgumentOutOfRangeException(nameof(cost));

            Type                       = type;
            Summary                    = summary ?? string.Empty;
            BestFor                    = bestFor ?? string.Empty;
            Limitations                = limitations ?? string.Empty;
            Cost                       = cost;
            MinimumProfilePoints       = minimumProfilePoints;
            MinimumPathPoints          = minimumPathPoints;
            MinimumProfileSections     = minimumProfileSections;
            MinimumProfileCageSections = minimumProfileCageSections;
            ShapeParameterCapability[] parameterArray = parameters == null
                ? Array.Empty<ShapeParameterCapability>()
                : (ShapeParameterCapability[])parameters.Clone();
            HashSet<string> parameterNames = new(StringComparer.Ordinal);
            foreach (ShapeParameterCapability parameter in parameterArray)
            {
                if (parameter == null)
                    throw new ArgumentException("Shape capabilities cannot contain null parameters.", nameof(parameters));

                if (!parameterNames.Add(parameter.Name))
                    throw new ArgumentException(
                        $"Shape capability '{type}' contains duplicate parameter '{parameter.Name}'.",
                        nameof(parameters));
            }

            this.parameters = Array.AsReadOnly(parameterArray);
        }

        /// <summary>Gets the exact extensible shape type identifier.</summary>
        public string Type { get; }

        /// <summary>Gets the concise geometric description.</summary>
        public string Summary { get; }

        /// <summary>Gets the recommended modelling uses.</summary>
        public string BestFor { get; }

        /// <summary>Gets the important visual or authoring limitations.</summary>
        public string Limitations { get; }

        /// <summary>Gets the expected mesh-generation cost behavior.</summary>
        public ShapeGenerationCost Cost { get; }

        /// <summary>Gets the minimum required profile point count.</summary>
        public int MinimumProfilePoints { get; }

        /// <summary>Gets the minimum required path point count.</summary>
        public int MinimumPathPoints { get; }

        /// <summary>Gets the minimum required profile-section count.</summary>
        public int MinimumProfileSections { get; }

        /// <summary>Gets the minimum required independent profile-cage section count.</summary>
        public int MinimumProfileCageSections { get; }

        /// <summary>Gets the supported numeric parameters.</summary>
        public IReadOnlyList<ShapeParameterCapability> Parameters => parameters;
    }
}
