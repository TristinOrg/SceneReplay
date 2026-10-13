using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Describes the shapes supported by one generation backend.
    /// </summary>
    public interface IShapeCapabilityCatalog
    {
        /// <summary>Gets the supported shapes in stable documentation order.</summary>
        IReadOnlyList<ShapeCapability> Shapes { get; }

        /// <summary>Attempts to find the capability for one exact shape type.</summary>
        bool TryGet(string type, out ShapeCapability capability);
    }
}
