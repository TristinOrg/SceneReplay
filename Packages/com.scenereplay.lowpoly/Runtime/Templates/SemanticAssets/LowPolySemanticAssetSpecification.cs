using System;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Defines shared readable dimensions for compact Low Poly semantic asset templates.
    /// </summary>
    [Serializable]
    public sealed class LowPolySemanticAssetSpecification
    {
        /// <summary>Identifies the current shared semantic-asset specification.</summary>
        public const string CurrentSchema = "scenereplay.lowpoly.semantic-asset/1.0";

        /// <summary>Gets or sets the specification schema.</summary>
        public string Schema { get; set; } = CurrentSchema;
        /// <summary>Gets or sets the generated model name.</summary>
        public string Name { get; set; } = "Semantic Asset";
        /// <summary>Gets or sets overall width.</summary>
        public float Width { get; set; } = 1f;
        /// <summary>Gets or sets overall height.</summary>
        public float Height { get; set; } = 1f;
        /// <summary>Gets or sets overall depth.</summary>
        public float Depth { get; set; } = 1f;
        /// <summary>Gets or sets the secondary-detail scale.</summary>
        public float DetailScale { get; set; } = 1f;
    }
}
