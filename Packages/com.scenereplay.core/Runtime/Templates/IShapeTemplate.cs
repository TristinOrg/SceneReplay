using System;

namespace SceneReplay
{
    /// <summary>
    /// Compiles one semantic specification into an engine-agnostic shape definition.
    /// </summary>
    public interface IShapeTemplate
    {
        /// <summary>Gets the template metadata exposed to authoring tools.</summary>
        ShapeTemplateDescriptor Descriptor { get; }

        /// <summary>Gets the exact specification type accepted by this compiler.</summary>
        Type SpecificationType { get; }

        /// <summary>Compiles a validated specification supplied through an untyped catalog.</summary>
        ShapeDefinition Compile(object specification);
    }
}
