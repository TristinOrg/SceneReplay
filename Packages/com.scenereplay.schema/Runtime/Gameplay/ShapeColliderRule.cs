using System;

namespace SceneReplay
{
    /// <summary>
    /// Defines an engine-neutral collider compilation rule for a stable node.
    /// </summary>
    [Serializable]
    public sealed class ShapeColliderRule
    {
        /// <summary>Gets or sets the stable rule identifier.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets the stable target node ID.</summary>
        public string NodeId { get; set; } = string.Empty;
        /// <summary>Gets or sets the collider approximation.</summary>
        public ShapeColliderKind Kind { get; set; }
        /// <summary>Gets or sets whether the native collider is a trigger.</summary>
        public bool IsTrigger { get; set; }
        /// <summary>Gets or sets node-relative collider center.</summary>
        public ForgeVector3 Center { get; set; } = ForgeVector3.Zero;
        /// <summary>Gets or sets positive box dimensions.</summary>
        public ForgeVector3 Size { get; set; } = ForgeVector3.One;
        /// <summary>Gets or sets positive sphere or capsule radius.</summary>
        public float Radius { get; set; } = 0.5f;
        /// <summary>Gets or sets positive capsule height.</summary>
        public float Height { get; set; } = 1f;
    }
}
