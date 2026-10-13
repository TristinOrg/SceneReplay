using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Stores an immutable set of semantic-template compilers with cached exact-ID lookup.
    /// </summary>
    public sealed class ShapeTemplateCatalog : IShapeTemplateCatalog
    {
        private readonly IReadOnlyList<IShapeTemplate>       templates;
        private readonly Dictionary<string, IShapeTemplate> templateMap;

        /// <summary>Initializes a catalog and rejects invalid or duplicate template IDs.</summary>
        public ShapeTemplateCatalog(params IShapeTemplate[] templates)
        {
            IShapeTemplate[] templateArray = templates == null
                ? Array.Empty<IShapeTemplate>()
                : (IShapeTemplate[])templates.Clone();
            templateMap = new(StringComparer.Ordinal);

            foreach (IShapeTemplate template in templateArray)
            {
                if (template?.Descriptor == null)
                    throw new ArgumentException("Template catalogs cannot contain null metadata.", nameof(templates));

                if (!templateMap.TryAdd(template.Descriptor.Id, template))
                    throw new ArgumentException(
                        $"Duplicate shape template ID '{template.Descriptor.Id}'.",
                        nameof(templates));
            }

            this.templates = Array.AsReadOnly(templateArray);
        }

        /// <inheritdoc />
        public IReadOnlyList<IShapeTemplate> Templates => templates;

        /// <inheritdoc />
        public bool TryGet(string id, out IShapeTemplate template)
        {
            if (id == null)
            {
                template = null;
                return false;
            }

            return templateMap.TryGetValue(id, out template);
        }

        /// <summary>Creates a versioned discovery document without exposing compiler instances.</summary>
        public ShapeTemplateCatalogDocument CreateDocument(string id)
        {
            ShapeTemplateDescriptor[] descriptors = new ShapeTemplateDescriptor[templates.Count];
            for (int index = 0; index < templates.Count; index++)
                descriptors[index] = templates[index].Descriptor;

            return new(id, descriptors);
        }
    }
}
