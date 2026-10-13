using System;
using System.Collections.Generic;

namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Collects optional implementation catalogs without coupling SceneReplay Unity to a concrete style package.
    /// </summary>
    public static class ShapeAutomationCatalogRegistry
    {
        private static readonly Dictionary<string, IShapeAutomationCatalogProvider> Providers =
            new(StringComparer.Ordinal);

        static ShapeAutomationCatalogRegistry()
        {
            Register("core/specification", new CoreCatalogProvider());
        }

        /// <summary>Registers or replaces one implementation-owned discovery provider.</summary>
        public static void Register(string id, IShapeAutomationCatalogProvider provider)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A discovery provider requires a stable ID.", nameof(id));
            Providers[id] = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>Gets providers in stable identifier order.</summary>
        public static IReadOnlyList<IShapeAutomationCatalogProvider> GetProviders()
        {
            List<string> ids = new(Providers.Keys);
            ids.Sort(StringComparer.Ordinal);
            IShapeAutomationCatalogProvider[] result = new IShapeAutomationCatalogProvider[ids.Count];
            for (int index = 0; index < ids.Count; index++)
                result[index] = Providers[ids[index]];
            return result;
        }

        /// <summary>Gets registered temporary model compilers in stable provider order.</summary>
        public static IReadOnlyList<IShapeAutomationModelCompiler> GetModelCompilers()
        {
            IReadOnlyList<IShapeAutomationCatalogProvider> providers = GetProviders();
            List<IShapeAutomationModelCompiler> result = new();
            foreach (IShapeAutomationCatalogProvider provider in providers)
            {
                if (provider is IShapeAutomationModelCompiler compiler)
                    result.Add(compiler);
            }
            return result;
        }

        /// <summary>Publishes the always-available engine-neutral Core catalog.</summary>
        private sealed class CoreCatalogProvider : IShapeAutomationCatalogProvider
        {
            /// <inheritdoc />
            public ShapeCapabilityCatalogDocument CreateCapabilities() =>
                new("core/specification", CoreShapeCapabilityCatalog.Instance.Shapes);

            /// <inheritdoc />
            public ShapeTemplateCatalogDocument CreateTemplates() =>
                new ShapeTemplateCatalog().CreateDocument("core/specification");
        }
    }
}
