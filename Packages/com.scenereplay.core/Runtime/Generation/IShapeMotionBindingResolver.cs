namespace SceneReplay
{
    /// <summary>
    /// Defines the stable read/write boundary consumed by an external motion system.
    /// </summary>
    public interface IShapeMotionBindingResolver
    {
        /// <summary>Gets the semantic rig type.</summary>
        string RigType { get; }
        /// <summary>Gets the number of cached semantic targets.</summary>
        int BindingCount { get; }
        /// <summary>Resolves immutable SceneReplay binding metadata by semantic role.</summary>
        bool TryGetBinding(string role, out ShapeMotionBinding binding);
        /// <summary>Resolves a cached writable transform target by semantic role.</summary>
        bool TryGetTarget(string role, out IShapeTransformTarget target);
        /// <summary>Clamps a motion-system rotation offset to authored joint limits.</summary>
        ForgeVector3 ConstrainRotationOffset(string role, ForgeVector3 requestedOffset);
        /// <summary>Restores every resolved target to its authored SceneReplay rest pose.</summary>
        void ResetToRestPose();
    }
}
