namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Exposes official Low Poly semantic templates through the engine-agnostic Core catalog.
    /// </summary>
    public static class LowPolyShapeTemplateCatalog
    {
        /// <summary>Gets the cached catalog of official Low Poly semantic compilers.</summary>
        public static ShapeTemplateCatalog Instance { get; } = new(
            LowPolyHairTemplate.Instance,
            LowPolyArmorTemplate.Instance,
            LowPolyWeaponTemplate.Instance,
            LowPolyBuildingTemplate.Instance,
            LowPolyVehicleTemplate.Instance);
    }
}
