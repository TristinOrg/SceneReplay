using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Declares an engine adapter's version, semantic feature support, and unsupported-feature policy.
    /// </summary>
    [Serializable]
    public sealed class ShapeAdapterDescriptor
    {
        /// <summary>Identifies the current adapter descriptor contract.</summary>
        public const string CurrentSchema = "scenereplay.adapter/1.0";

        /// <summary>Initializes an empty adapter descriptor for JSON deserialization.</summary>
        public ShapeAdapterDescriptor()
        {
        }

        /// <summary>Initializes a versioned engine adapter descriptor.</summary>
        public ShapeAdapterDescriptor(
            string adapterId,
            string engineId,
            string version,
            IEnumerable<string> features,
            ShapeAdapterUnsupportedFeaturePolicy unsupportedFeaturePolicy)
        {
            AdapterId = adapterId;
            EngineId = engineId;
            Version = version;
            Features = features == null ? null : new List<string>(features);
            UnsupportedFeaturePolicy = unsupportedFeaturePolicy;
        }

        /// <summary>Gets or sets the adapter descriptor schema identifier.</summary>
        public string Schema { get; set; } = CurrentSchema;

        /// <summary>Gets or sets the stable adapter identifier.</summary>
        public string AdapterId { get; set; } = string.Empty;

        /// <summary>Gets or sets the lowercase host engine identifier.</summary>
        public string EngineId { get; set; } = string.Empty;

        /// <summary>Gets or sets the adapter implementation version.</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Gets or sets supported standard or namespaced feature identifiers.</summary>
        public IList<string> Features { get; set; } = new List<string>();

        /// <summary>Gets or sets the declared behavior for unsupported semantic features.</summary>
        public ShapeAdapterUnsupportedFeaturePolicy UnsupportedFeaturePolicy { get; set; }
    }
}
