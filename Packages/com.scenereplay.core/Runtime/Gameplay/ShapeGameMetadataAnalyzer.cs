using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Resolves game-semantic metadata against stable ShapeDefinition nodes.
    /// </summary>
    public sealed class ShapeGameMetadataAnalyzer
    {
        /// <summary>Validates metadata and every stable node binding.</summary>
        public ShapeGameMetadataReport Analyze(ShapeDefinition definition, ShapeGameMetadata metadata)
        {
            ShapeDiagnosticReport definitionReport = new ShapeDefinitionValidator().Analyze(definition);
            if (!definitionReport.IsValid)
                return new(0, 0, 0, 0, definitionReport);
            ShapeDiagnosticReport metadataReport = new ShapeGameMetadataValidator().Analyze(metadata);
            if (!metadataReport.IsValid)
                return new(0, 0, 0, 0, metadataReport);

            HashSet<string> nodeIds = new(StringComparer.Ordinal);
            Collect(definition.Root, nodeIds);
            List<ShapeDiagnostic> diagnostics = new();
            for (int index = 0; index < metadata.Anchors.Count; index++)
                RequireNode(metadata.Anchors[index].NodeId, $"/anchors/{index}/nodeId", nodeIds, diagnostics);
            for (int index = 0; index < metadata.DamageZones.Count; index++)
                RequireNode(metadata.DamageZones[index].NodeId, $"/damageZones/{index}/nodeId", nodeIds, diagnostics);
            for (int index = 0; index < metadata.Colliders.Count; index++)
                RequireNode(metadata.Colliders[index].NodeId, $"/colliders/{index}/nodeId", nodeIds, diagnostics);
            for (int lodIndex = 0; lodIndex < metadata.Lods.Count; lodIndex++)
            {
                ShapeLodRule lod = metadata.Lods[lodIndex];
                for (int nodeIndex = 0; nodeIndex < lod.NodeIds.Count; nodeIndex++)
                    RequireNode(lod.NodeIds[nodeIndex], $"/lods/{lodIndex}/nodeIds/{nodeIndex}", nodeIds, diagnostics);
            }
            return new(
                metadata.Anchors.Count,
                metadata.DamageZones.Count,
                metadata.Colliders.Count,
                metadata.Lods.Count,
                new ShapeDiagnosticReport(diagnostics));
        }

        private static void RequireNode(
            string nodeId,
            string path,
            ISet<string> nodeIds,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (!nodeIds.Contains(nodeId))
                diagnostics.Add(new(
                    "shape.game.node.unknown",
                    ShapeDiagnosticSeverity.Error,
                    $"Game metadata targets unknown node '{nodeId}'.",
                    nodeId,
                    path));
        }

        private static void Collect(ShapeNode node, ISet<string> nodeIds)
        {
            nodeIds.Add(node.Id);
            foreach (ShapeNode child in node.Children)
                Collect(child, nodeIds);
        }
    }
}
