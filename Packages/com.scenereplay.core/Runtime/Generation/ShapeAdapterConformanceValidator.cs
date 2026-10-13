using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Verifies that an engine adapter exposes every stable node as a faithful writable transform target.
    /// </summary>
    public sealed class ShapeAdapterConformanceValidator
    {
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
