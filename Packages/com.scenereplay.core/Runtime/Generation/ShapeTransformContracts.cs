namespace SceneReplay
{
    /// <summary>
    /// Exposes one engine-agnostic local transform target for motion systems.
    /// </summary>
    public interface IShapeTransformTarget
    {
        /// <summary>
        /// Gets the stable SceneReplay node identifier.
        /// </summary>
        string NodeId { get; }

        /// <summary>
        /// Gets or sets the local position in meters.
        /// </summary>
        ForgeVector3 LocalPosition { get; set; }

        /// <summary>
        /// Gets or sets the local Euler rotation in degrees.
        /// </summary>
        ForgeVector3 LocalEulerAngles { get; set; }

        /// <summary>
        /// Gets or sets the local scale.
        /// </summary>
        ForgeVector3 LocalScale { get; set; }
    }

    /// <summary>
    /// Resolves stable SceneReplay node identifiers to cacheable motion targets.
    /// </summary>
    public interface IShapeTransformResolver
    {
        /// <summary>
        /// Resolves one transform target without traversing an engine hierarchy.
        /// </summary>
        bool TryGetTarget(string nodeId, out IShapeTransformTarget target);
    }
}
