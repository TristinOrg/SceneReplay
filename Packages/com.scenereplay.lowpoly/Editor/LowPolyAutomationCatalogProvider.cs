using SceneReplay.Unity.Editor;
using UnityEditor;

namespace SceneReplay.LowPoly.Editor
{
    /// <summary>
    /// Publishes Low Poly discovery documents through the style-neutral Unity automation registry.
    /// </summary>
    [InitializeOnLoad]
    public sealed class LowPolyAutomationCatalogProvider :
        IShapeAutomationCatalogProvider,
        IShapeAutomationModelCompiler
    {
        static LowPolyAutomationCatalogProvider()
        {
            ShapeAutomationCatalogRegistry.Register("lowpoly/official", new LowPolyAutomationCatalogProvider());
        }

        /// <inheritdoc />
        public ShapeCapabilityCatalogDocument CreateCapabilities() =>
            LowPolyShapeCapabilityCatalog.Instance.CreateDocument();

        /// <inheritdoc />
        public ShapeTemplateCatalogDocument CreateTemplates() =>
            LowPolyShapeTemplateCatalog.Instance.CreateDocument("lowpoly/official");

        /// <inheritdoc />
        public bool CanCompile(ShapeDefinition definition) =>
            definition != null && Supports(definition.Root);

        /// <inheritdoc />
        public UnityEngine.GameObject Compile(ShapeDefinition definition)
        {
            LowPolyModelGenerator generator = new(new[]
            {
                LowPolyRobotPreset.CreateStyle(),
                LowPolyWorkbenchPreset.CreateStyle(),
                LowPolyJapaneseTownPreset.CreateStyle(),
                LowPolyShibuyaCrossingPreset.CreateStyle()
            });
            return generator.Generate(definition);
        }

        private static bool Supports(ShapeNode node)
        {
            if (!LowPolyShapeCapabilityCatalog.Instance.TryGet(node.Type, out _) && node.Type != ShapeTypes.Group)
                return false;
            foreach (ShapeNode child in node.Children)
            {
                if (!Supports(child))
                    return false;
            }
            return true;
        }
    }
}
