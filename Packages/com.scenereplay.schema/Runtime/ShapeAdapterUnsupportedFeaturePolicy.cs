namespace SceneReplay
{
    /// <summary>
    /// Defines how an engine adapter handles semantic features it cannot compile natively.
    /// </summary>
    public enum ShapeAdapterUnsupportedFeaturePolicy
    {
        /// <summary>Reject definitions that require an unsupported feature.</summary>
        Reject,

        /// <summary>Preserve unsupported source data as SceneReplay metadata in the host asset.</summary>
        PreserveAsMetadata,

        /// <summary>Skip native compilation and report a warning for unsupported features.</summary>
        WarnAndSkip
    }
}
