namespace SceneReplay
{
    /// <summary>
    /// Contains an atomically advanced definition and construction plan or failure diagnostics.
    /// </summary>
    public sealed class ShapeConstructionStepResult
    {
        /// <summary>Initializes an immutable construction-step result.</summary>
        public ShapeConstructionStepResult(
            ShapeDefinition       definition,
            ShapeConstructionPlan plan,
            ShapeDiagnosticReport diagnostics)
        {
            Definition  = definition;
            Plan        = plan;
            Diagnostics = diagnostics;
        }

        /// <summary>Gets the updated definition, or null when execution failed.</summary>
        public ShapeDefinition Definition { get; }

        /// <summary>Gets the updated plan, or the original plan when execution failed.</summary>
        public ShapeConstructionPlan Plan { get; }

        /// <summary>Gets structured scheduling or patch diagnostics.</summary>
        public ShapeDiagnosticReport Diagnostics { get; }

        /// <summary>Gets whether the pass patch and state transition succeeded.</summary>
        public bool Succeeded => Definition != null && Diagnostics.IsValid;
    }
}
