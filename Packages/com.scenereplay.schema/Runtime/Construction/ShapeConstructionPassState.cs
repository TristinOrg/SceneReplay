namespace SceneReplay
{
    /// <summary>
    /// Identifies persisted construction-pass progress.
    /// </summary>
    public enum ShapeConstructionPassState
    {
        /// <summary>The pass has not started.</summary>
        Pending,
        /// <summary>The pass is currently executing.</summary>
        InProgress,
        /// <summary>The pass completed successfully.</summary>
        Completed,
        /// <summary>The pass failed and may be retried.</summary>
        Failed,
        /// <summary>The pass is intentionally skipped.</summary>
        Skipped
    }
}
