using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Defines a stable node-relative socket, grip, interaction, mount, or IK anchor.
    /// </summary>
    [Serializable]
    public sealed class ShapeSemanticAnchor
    {
        /// <summary>Gets or sets the stable anchor identifier.</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Gets or sets the anchor's extensible semantic role.</summary>
        public string Role { get; set; } = string.Empty;
        /// <summary>Gets or sets the stable owner node ID.</summary>
        public string NodeId { get; set; } = string.Empty;
        /// <summary>Gets or sets the node-relative anchor transform.</summary>
        public ShapeTransform Transform { get; set; } = new();
        /// <summary>Gets or sets extensible gameplay tags.</summary>
        public IList<string> Tags { get; set; } = new List<string>();
    }
}
