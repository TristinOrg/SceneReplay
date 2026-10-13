using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Provides a versioned, serializable capability catalog for external authoring tools.
    /// </summary>
    [Serializable]
    public sealed class ShapeCapabilityCatalogDocument
    {
        private readonly IReadOnlyList<ShapeCapability> shapes;

        /// <summary>Identifies the current SceneReplay capability schema.</summary>
        public const string CurrentSchema = "scenereplay.capabilities/1.0";

        /// <summary>Initializes a catalog document from immutable capability metadata.</summary>
        public ShapeCapabilityCatalogDocument(string id, IReadOnlyList<ShapeCapability> shapes)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A capability catalog requires a stable ID.", nameof(id));

            if (shapes == null)
                throw new ArgumentNullException(nameof(shapes));

            ShapeCapability[] shapeArray = new ShapeCapability[shapes.Count];
            for (int index = 0; index < shapes.Count; index++)
                shapeArray[index] = shapes[index] ?? throw new ArgumentException(
                    "Capability catalogs cannot contain null shapes.", nameof(shapes));

            Id          = id;
            this.shapes = Array.AsReadOnly(shapeArray);
        }

        /// <summary>Gets the versioned capability schema identifier.</summary>
        public string Schema => CurrentSchema;

        /// <summary>Gets the stable backend catalog identifier.</summary>
        public string Id { get; }

        /// <summary>Gets the documented shape capabilities.</summary>
        public IReadOnlyList<ShapeCapability> Shapes => shapes;
    }
}
