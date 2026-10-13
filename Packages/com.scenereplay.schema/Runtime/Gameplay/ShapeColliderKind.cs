namespace SceneReplay
{
    /// <summary>
    /// Identifies an engine-neutral collider approximation.
    /// </summary>
    public enum ShapeColliderKind
    {
        /// <summary>Uses a box approximation.</summary>
        Box,
        /// <summary>Uses a sphere approximation.</summary>
        Sphere,
        /// <summary>Uses a capsule approximation.</summary>
        Capsule,
        /// <summary>Uses generated mesh geometry where supported.</summary>
        Mesh
    }
}
