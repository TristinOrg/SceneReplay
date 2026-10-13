using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Verifies that an engine adapter exposes every stable node as a faithful writable transform target.
    /// </summary>
    public sealed class ShapeAdapterConformanceValidator
    {
        /// <summary>
        /// Validates a host hierarchy against the versioned adapter capability declaration.
        /// </summary>
        public ShapeDiagnosticReport Analyze(
            ShapeDefinition definition,
            ShapeAdapterDescriptor descriptor,
            IShapeHierarchyTransformResolver resolver)
        {
            ShapeDiagnosticReport definitionReport = new ShapeDefinitionValidator().Analyze(definition);
            if (!definitionReport.IsValid)
                return definitionReport;

            ShapeDiagnosticReport descriptorReport = new ShapeAdapterDescriptorValidator().Analyze(descriptor);
            List<ShapeDiagnostic> diagnostics = new(descriptorReport.Diagnostics);
            if (!descriptorReport.IsValid)
                return new(diagnostics);
            if (resolver == null)
            {
                diagnostics.Add(Error("shape.adapter.resolver.null", "Adapter resolver cannot be null.", null));
                return new(diagnostics);
            }
            if (!string.Equals(descriptor.AdapterId, resolver.AdapterId, StringComparison.Ordinal))
                diagnostics.Add(Error(
                    "shape.adapter.identity.mismatch",
                    $"Resolver adapter ID '{resolver.AdapterId}' does not match descriptor '{descriptor.AdapterId}'.",
                    null));

            if (definition.Rig != null && !Supports(descriptor, ShapeAdapterFeatureIds.SemanticRig))
                ReportUnsupportedFeature(descriptor, ShapeAdapterFeatureIds.SemanticRig, diagnostics);

            VerifyHierarchy(definition.Root, string.Empty, resolver, diagnostics);
            return new(diagnostics);
        }

        /// <summary>
        /// Validates game-semantic metadata against the source hierarchy and adapter capabilities.
        /// </summary>
        public ShapeDiagnosticReport Analyze(
            ShapeDefinition definition,
            ShapeGameMetadata metadata,
            ShapeAdapterDescriptor descriptor,
            IShapeHierarchyTransformResolver resolver)
        {
            ShapeDiagnosticReport hierarchyReport = Analyze(definition, descriptor, resolver);
            List<ShapeDiagnostic> diagnostics = new(hierarchyReport.Diagnostics);
            if (definition == null || !new ShapeDefinitionValidator().Analyze(definition).IsValid)
                return new(diagnostics);

            ShapeGameMetadataReport metadataReport = new ShapeGameMetadataAnalyzer().Analyze(definition, metadata);
            foreach (ShapeDiagnostic diagnostic in metadataReport.Diagnostics.Diagnostics)
                diagnostics.Add(diagnostic);

            if (descriptor != null && descriptor.Features != null &&
                !Supports(descriptor, ShapeAdapterFeatureIds.GameMetadata))
                ReportUnsupportedFeature(descriptor, ShapeAdapterFeatureIds.GameMetadata, diagnostics);

            return new(diagnostics);
        }

        /// <summary>Checks resolution and reversible local-transform writes for a generated model.</summary>
        public ShapeDiagnosticReport Analyze(ShapeDefinition definition, IShapeTransformResolver resolver)
        {
            List<ShapeDiagnostic> diagnostics = new();
            ShapeDiagnosticReport definitionReport = new ShapeDefinitionValidator().Analyze(definition);
            if (!definitionReport.IsValid)
                return definitionReport;
            if (resolver == null)
            {
                diagnostics.Add(Error("shape.adapter.resolver.null", "Adapter resolver cannot be null.", null));
                return new(diagnostics);
            }
            Verify(definition.Root, resolver, diagnostics);
            return new(diagnostics);
        }

        private static void VerifyHierarchy(
            ShapeNode node,
            string expectedParentId,
            IShapeHierarchyTransformResolver resolver,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (!resolver.TryGetTarget(node.Id, out IShapeTransformTarget target) || target == null)
            {
                diagnostics.Add(Error("shape.adapter.target.missing", $"Adapter does not expose node '{node.Id}'.", node.Id));
            }
            else
            {
                VerifyNode(node, expectedParentId, target, resolver, diagnostics);
            }

            foreach (ShapeNode child in node.Children)
                VerifyHierarchy(child, node.Id, resolver, diagnostics);
        }

        private static void VerifyNode(
            ShapeNode node,
            string expectedParentId,
            IShapeTransformTarget target,
            IShapeHierarchyTransformResolver resolver,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (!string.Equals(target.NodeId, node.Id, StringComparison.Ordinal))
                diagnostics.Add(Error(
                    "shape.adapter.nodeId.mismatch",
                    $"Resolver returned node ID '{target.NodeId}' for requested node '{node.Id}'.",
                    node.Id));

            if (!resolver.TryGetParentNodeId(node.Id, out string actualParentId))
                diagnostics.Add(Error(
                    "shape.adapter.parent.missing",
                    $"Adapter does not expose the semantic parent for node '{node.Id}'.",
                    node.Id));
            else if (!string.Equals(actualParentId ?? string.Empty, expectedParentId, StringComparison.Ordinal))
                diagnostics.Add(Error(
                    "shape.adapter.parent.mismatch",
                    $"Node '{node.Id}' has parent '{actualParentId}' instead of '{expectedParentId}'.",
                    node.Id));

            ForgeVector3 originalPosition;
            ForgeVector3 originalRotation;
            ForgeVector3 originalScale;
            try
            {
                originalPosition = target.LocalPosition;
                originalRotation = target.LocalEulerAngles;
                originalScale = target.LocalScale;
            }
            catch (Exception exception)
            {
                diagnostics.Add(Error("shape.adapter.target.read", $"Adapter target '{node.Id}' could not be read: {exception.Message}", node.Id));
                return;
            }

            try
            {
                if (!NearlyEqual(originalPosition, node.Transform.Position) ||
                    !NearlyEqualAngle(originalRotation, node.Transform.EulerAngles) ||
                    !NearlyEqual(originalScale, node.Transform.Scale))
                    diagnostics.Add(Error(
                        "shape.adapter.transform.mismatch",
                        $"Adapter local transform for node '{node.Id}' differs from the SceneReplay definition.",
                        node.Id));

                ForgeVector3 positionProbe = new(originalPosition.X + 0.125f, originalPosition.Y - 0.25f, originalPosition.Z + 0.5f);
                target.LocalPosition = positionProbe;
                if (!NearlyEqual(target.LocalPosition, positionProbe))
                    diagnostics.Add(Error("shape.adapter.position.write", $"Adapter target '{node.Id}' rejected a position write.", node.Id));

                ForgeVector3 rotationProbe = new(originalRotation.X + 13f, originalRotation.Y - 17f, originalRotation.Z + 23f);
                target.LocalEulerAngles = rotationProbe;
                if (!NearlyEqualAngle(target.LocalEulerAngles, rotationProbe))
                    diagnostics.Add(Error("shape.adapter.rotation.write", $"Adapter target '{node.Id}' rejected a rotation write.", node.Id));

                ForgeVector3 scaleProbe = new(originalScale.X + 0.125f, originalScale.Y + 0.25f, originalScale.Z + 0.5f);
                target.LocalScale = scaleProbe;
                if (!NearlyEqual(target.LocalScale, scaleProbe))
                    diagnostics.Add(Error("shape.adapter.scale.write", $"Adapter target '{node.Id}' rejected a scale write.", node.Id));
            }
            catch (Exception exception)
            {
                diagnostics.Add(Error("shape.adapter.target.exception", $"Adapter target '{node.Id}' failed: {exception.Message}", node.Id));
            }
            finally
            {
                try
                {
                    target.LocalPosition = originalPosition;
                    target.LocalEulerAngles = originalRotation;
                    target.LocalScale = originalScale;
                }
                catch (Exception exception)
                {
                    diagnostics.Add(Error("shape.adapter.target.restore", $"Adapter target '{node.Id}' could not restore state: {exception.Message}", node.Id));
                }
            }
        }

        private static bool Supports(ShapeAdapterDescriptor descriptor, string feature)
        {
            foreach (string supportedFeature in descriptor.Features)
            {
                if (string.Equals(supportedFeature, feature, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void ReportUnsupportedFeature(
            ShapeAdapterDescriptor descriptor,
            string feature,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            ShapeDiagnosticSeverity severity = descriptor.UnsupportedFeaturePolicy == ShapeAdapterUnsupportedFeaturePolicy.Reject
                ? ShapeDiagnosticSeverity.Error
                : descriptor.UnsupportedFeaturePolicy == ShapeAdapterUnsupportedFeaturePolicy.PreserveAsMetadata
                    ? ShapeDiagnosticSeverity.Information
                    : ShapeDiagnosticSeverity.Warning;
            string handling = descriptor.UnsupportedFeaturePolicy switch
            {
                ShapeAdapterUnsupportedFeaturePolicy.PreserveAsMetadata => "must preserve the source data as metadata",
                ShapeAdapterUnsupportedFeaturePolicy.WarnAndSkip => "will skip native compilation",
                _ => "cannot compile"
            };
            diagnostics.Add(new(
                "shape.adapter.feature.unsupported",
                severity,
                $"Adapter '{descriptor.AdapterId}' does not support '{feature}' and {handling}.",
                path: "/features"));
        }

        private static bool NearlyEqual(ForgeVector3 left, ForgeVector3 right) =>
            Math.Abs(left.X - right.X) <= 0.0001f &&
            Math.Abs(left.Y - right.Y) <= 0.0001f &&
            Math.Abs(left.Z - right.Z) <= 0.0001f;

        private static bool NearlyEqualAngle(ForgeVector3 left, ForgeVector3 right) =>
            NearlyEqualAngle(left.X, right.X) && NearlyEqualAngle(left.Y, right.Y) && NearlyEqualAngle(left.Z, right.Z);

        private static bool NearlyEqualAngle(float left, float right)
        {
            float difference = Math.Abs(left - right) % 360f;
            return Math.Min(difference, 360f - difference) <= 0.0001f;
        }

        private static void Verify(
            ShapeNode node,
            IShapeTransformResolver resolver,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (!resolver.TryGetTarget(node.Id, out IShapeTransformTarget target) || target == null)
            {
                diagnostics.Add(Error("shape.adapter.target.missing", $"Adapter does not expose node '{node.Id}'.", node.Id));
            }
            else
            {
                ForgeVector3 position = target.LocalPosition;
                ForgeVector3 rotation = target.LocalEulerAngles;
                ForgeVector3 scale    = target.LocalScale;
                try
                {
                    ForgeVector3 probe = new(position.X + 0.125f, position.Y - 0.25f, position.Z + 0.5f);
                    target.LocalPosition = probe;
                    if (!target.LocalPosition.Equals(probe))
                        diagnostics.Add(Error("shape.adapter.target.write", $"Adapter target '{node.Id}' rejected a local-position write.", node.Id));
                }
                catch (Exception exception)
                {
                    diagnostics.Add(Error("shape.adapter.target.exception", $"Adapter target '{node.Id}' failed: {exception.Message}", node.Id));
                }
                finally
                {
                    try
                    {
                        target.LocalPosition    = position;
                        target.LocalEulerAngles = rotation;
                        target.LocalScale       = scale;
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Add(Error("shape.adapter.target.restore", $"Adapter target '{node.Id}' could not restore state: {exception.Message}", node.Id));
                    }
                }
            }
            foreach (ShapeNode child in node.Children)
                Verify(child, resolver, diagnostics);
        }

        private static ShapeDiagnostic Error(string code, string message, string nodeId) =>
            new(code, ShapeDiagnosticSeverity.Error, message, nodeId);
    }
}
