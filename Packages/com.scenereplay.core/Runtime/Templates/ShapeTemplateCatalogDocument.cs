using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Provides versioned semantic-template discovery data for external authoring tools.
    /// </summary>
    [Serializable]
    public sealed class ShapeTemplateCatalogDocument
    {
        private readonly IReadOnlyList<ShapeTemplateDescriptor> templates;

        /// <summary>Identifies the current semantic-template catalog schema.</summary>
        public const string CurrentSchema = "scenereplay.templates/1.0";

        /// <summary>Initializes an immutable template discovery document.</summary>
        public ShapeTemplateCatalogDocument(string id, IReadOnlyList<ShapeTemplateDescriptor> templates)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A template catalog requires a stable ID.", nameof(id));

            if (templates == null)
                throw new ArgumentNullException(nameof(templates));

            ShapeTemplateDescriptor[] templateArray = new ShapeTemplateDescriptor[templates.Count];
            for (int index = 0; index < templates.Count; index++)
                templateArray[index] = templates[index] ?? throw new ArgumentException(
                    "Template catalog documents cannot contain null descriptors.", nameof(templates));

            Id             = id;
            this.templates = Array.AsReadOnly(templateArray);
        }

        /// <summary>Gets the versioned catalog schema identifier.</summary>
        public string Schema => CurrentSchema;

        /// <summary>Gets the stable catalog identifier.</summary>
        public string Id { get; }

        /// <summary>Gets template discovery metadata without runtime compiler instances.</summary>
        public IReadOnlyList<ShapeTemplateDescriptor> Templates => templates;
    }
}
