namespace SceneReplay
{
    /// <summary>
    /// Identifies one standard asset-construction stage.
    /// </summary>
    public enum ShapeConstructionPassKind
    {
        /// <summary>Establishes hierarchy, pivots, and major semantic regions.</summary>
        Structure,
        /// <summary>Builds dominant silhouettes and volumes.</summary>
        PrimaryForms,
        /// <summary>Builds supporting forms.</summary>
        SecondaryForms,
        /// <summary>Builds inventory details and accessories.</summary>
        Details,
        /// <summary>Assigns palette roles and appearance.</summary>
        Appearance,
        /// <summary>Adds sockets, interaction, physics, and gameplay semantics.</summary>
        GameplaySemantics,
        /// <summary>Runs final comparison and quality acceptance.</summary>
        FinalQuality
    }
}
