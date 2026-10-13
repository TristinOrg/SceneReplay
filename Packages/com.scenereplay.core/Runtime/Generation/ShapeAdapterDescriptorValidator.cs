using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Validates versioned engine adapter capability declarations.
    /// </summary>
    public sealed class ShapeAdapterDescriptorValidator
    {
        /// <summary>Returns deterministic diagnostics for an adapter capability document.</summary>
        public ShapeDiagnosticReport Analyze(ShapeAdapterDescriptor descriptor)
        {
            List<ShapeDiagnostic> diagnostics = new();
            if (descriptor == null)
            {
                diagnostics.Add(Error("shape.adapter.descriptor.required", "An adapter descriptor is required.", "/"));
                return new(diagnostics);
            }

            if (!string.Equals(descriptor.Schema, ShapeAdapterDescriptor.CurrentSchema, StringComparison.Ordinal))
                diagnostics.Add(Error("shape.adapter.schema.unsupported", "Unsupported adapter descriptor schema.", "/schema"));
            if (string.IsNullOrWhiteSpace(descriptor.AdapterId))
                diagnostics.Add(Error("shape.adapter.id.required", "An adapter descriptor requires a stable adapter ID.", "/adapterId"));
            if (string.IsNullOrWhiteSpace(descriptor.EngineId))
                diagnostics.Add(Error("shape.adapter.engine.required", "An adapter descriptor requires a host engine ID.", "/engineId"));
            if (string.IsNullOrWhiteSpace(descriptor.Version))
                diagnostics.Add(Error("shape.adapter.version.required", "An adapter descriptor requires an implementation version.", "/version"));
            if (descriptor.Features == null)
            {
                diagnostics.Add(Error("shape.adapter.features.required", "Adapter features cannot be null.", "/features"));
            }
            else
            {
                HashSet<string> features = new(StringComparer.Ordinal);
                for (int index = 0; index < descriptor.Features.Count; index++)
                {
                    string feature = descriptor.Features[index];
                    string path = $"/features/{index}";
                    if (string.IsNullOrWhiteSpace(feature))
                        diagnostics.Add(Error("shape.adapter.feature.required", "Feature identifiers cannot be empty.", path));
                    else if (!features.Add(feature))
                        diagnostics.Add(Error("shape.adapter.feature.duplicate", $"Feature '{feature}' is declared more than once.", path));
                }

                RequireFeature(features, ShapeAdapterFeatureIds.StableNodeIds, diagnostics);
                RequireFeature(features, ShapeAdapterFeatureIds.Hierarchy, diagnostics);
                RequireFeature(features, ShapeAdapterFeatureIds.LocalTransforms, diagnostics);
            }

            if (!Enum.IsDefined(typeof(ShapeAdapterUnsupportedFeaturePolicy), descriptor.UnsupportedFeaturePolicy))
                diagnostics.Add(Error(
                    "shape.adapter.unsupportedPolicy.invalid",
                    "The unsupported-feature policy is not recognized.",
                    "/unsupportedFeaturePolicy"));

            return new(diagnostics);
        }

        private static void RequireFeature(
            ISet<string> features,
            string requiredFeature,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (!features.Contains(requiredFeature))
                diagnostics.Add(Error(
                    "shape.adapter.feature.required",
                    $"Adapter contract v1 requires feature '{requiredFeature}'.",
                    "/features"));
        }

        private static ShapeDiagnostic Error(string code, string message, string path) =>
            new(code, ShapeDiagnosticSeverity.Error, message, path: path);
    }
}
