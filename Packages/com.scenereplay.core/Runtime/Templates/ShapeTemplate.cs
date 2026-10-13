using System;

namespace SceneReplay
{
    /// <summary>
    /// Provides a type-safe semantic-template implementation with safe untyped catalog dispatch.
    /// </summary>
    public abstract class ShapeTemplate<TSpecification> : IShapeTemplate
        where TSpecification : class
    {
        /// <inheritdoc />
        public abstract ShapeTemplateDescriptor Descriptor { get; }

        /// <inheritdoc />
        public Type SpecificationType => typeof(TSpecification);

        /// <summary>Compiles a strongly typed semantic specification.</summary>
        public abstract ShapeDefinition Compile(TSpecification specification);

        /// <inheritdoc />
        public ShapeDefinition Compile(object specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            if (specification is not TSpecification typedSpecification)
                throw new ArgumentException(
                    $"Template '{Descriptor.Id}' requires specification type '{typeof(TSpecification).FullName}'.",
                    nameof(specification));

            return Compile(typedSpecification) ?? throw new InvalidOperationException(
                $"Template '{Descriptor.Id}' returned no shape definition.");
        }
    }
}
