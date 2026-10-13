namespace SceneReplay
{
    /// <summary>
    /// Contains either an atomically patched definition or structured failure diagnostics.
    /// </summary>
    public sealed class ShapePatchResult
    {
        /// <summary>Initializes a patch result.</summary>
        public ShapePatchResult(ShapeDefinition definition, ShapeDiagnosticReport diagnostics)
        {
            Definition  = definition;
            Diagnostics = diagnostics;
        }

        /// <summary>Gets the patched definition, or null when patching failed.</summary>
        public ShapeDefinition Definition { get; }

        /// <summary>Gets structured patch or validation diagnostics.</summary>
        public ShapeDiagnosticReport Diagnostics { get; }

        /// <summary>Gets whether a valid patched definition was produced.</summary>
        public bool Succeeded => Definition != null && Diagnostics.IsValid;
    }
}
