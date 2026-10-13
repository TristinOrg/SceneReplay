namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Supplies capability and template discovery documents to the thin agent surface.
    /// </summary>
    public interface IShapeAutomationCatalogProvider
    {
        /// <summary>Creates a versioned shape-capability document.</summary>
        ShapeCapabilityCatalogDocument CreateCapabilities();
        /// <summary>Creates a versioned semantic-template document.</summary>
        ShapeTemplateCatalogDocument CreateTemplates();
    }
}
