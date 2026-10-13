using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Matches semantic inventory details to stable ShapeDefinition node IDs.
    /// </summary>
    public sealed class ShapeDetailCoverageAnalyzer
    {
        /// <summary>Analyzes required and optional detail implementation.</summary>
        public ShapeDetailCoverageReport Analyze(ShapeDefinition definition, ShapeDetailInventory inventory)
        {
            ShapeDiagnosticReport definitionReport = new ShapeDefinitionValidator().Analyze(definition);
            if (!definitionReport.IsValid)
                return new(0, 0, 0, definitionReport);
            ShapeDiagnosticReport inventoryReport = new ShapeDetailInventoryValidator().Analyze(inventory);
            if (!inventoryReport.IsValid)
                return new(0, 0, 0, inventoryReport);

            HashSet<string> nodeIds = new(StringComparer.Ordinal);
            Collect(definition.Root, nodeIds);
            List<ShapeDiagnostic> diagnostics = new();
            int required = 0;
            int resolved = 0;
            foreach (ShapeDetailItem item in inventory.Details)
            {
                bool implemented = !string.IsNullOrWhiteSpace(item.TargetNodeId) && nodeIds.Contains(item.TargetNodeId);
                if (item.Required)
                {
                    required++;
                    if (implemented)
                        resolved++;
                    else
                        diagnostics.Add(new(
                            "shape.inventory.detail.missing",
                            ShapeDiagnosticSeverity.Error,
                            $"Required detail '{item.Id}' is not mapped to an existing node.",
                            item.TargetNodeId,
                            $"/details/{Escape(item.Id)}/targetNodeId"));
                }
                else if (!implemented)
                    diagnostics.Add(new(
                        "shape.inventory.detail.optional.missing",
                        ShapeDiagnosticSeverity.Warning,
                        $"Optional detail '{item.Id}' is not mapped to an existing node.",
                        item.TargetNodeId,
                        $"/details/{Escape(item.Id)}/targetNodeId"));
            }

            return new(inventory.Details.Count, required, resolved, new ShapeDiagnosticReport(diagnostics));
        }

        private static void Collect(ShapeNode node, ISet<string> nodeIds)
        {
            nodeIds.Add(node.Id);
            foreach (ShapeNode child in node.Children)
                Collect(child, nodeIds);
        }

        private static string Escape(string value) => value.Replace("~", "~0").Replace("/", "~1");
    }
}
