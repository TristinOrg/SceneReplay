using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Describes shape types owned directly by the engine-agnostic SceneReplay specification.
    /// </summary>
    public sealed class CoreShapeCapabilityCatalog : IShapeCapabilityCatalog
    {
        private static readonly ShapeCapability[]                CapabilityArray =
        {
            new(
                ShapeTypes.Group,
                "A hierarchy-only node with no geometry.",
                "Named assemblies, animation pivots, logical parts, and transform inheritance.",
                "It is invisible and must contain child shapes to contribute visible geometry.",
                ShapeGenerationCost.Constant)
        };
        private static readonly IReadOnlyList<ShapeCapability>   CapabilityList =
            Array.AsReadOnly(CapabilityArray);

        /// <summary>Gets the shared stateless Core catalog.</summary>
        public static CoreShapeCapabilityCatalog Instance { get; } = new();

        /// <inheritdoc />
        public IReadOnlyList<ShapeCapability> Shapes => CapabilityList;

        /// <inheritdoc />
        public bool TryGet(string type, out ShapeCapability capability)
        {
            if (string.Equals(type, ShapeTypes.Group, StringComparison.Ordinal))
            {
                capability = CapabilityArray[0];
                return true;
            }

            capability = null;
            return false;
        }
    }
}
