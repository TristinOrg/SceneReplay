namespace SceneReplay
{
    /// <summary>
    /// Identifies the impact of a SceneReplay diagnostic.
    /// </summary>
    public enum ShapeDiagnosticSeverity
    {
        /// <summary>Provides non-actionable context.</summary>
        Information,

        /// <summary>Identifies a usable definition with a potential problem.</summary>
        Warning,

        /// <summary>Identifies a definition that cannot be compiled safely.</summary>
        Error
    }
}
