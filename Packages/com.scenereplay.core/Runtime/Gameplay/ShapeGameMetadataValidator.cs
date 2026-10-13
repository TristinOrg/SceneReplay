using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Validates engine-neutral game metadata before native compilation.
    /// </summary>
    public sealed class ShapeGameMetadataValidator
    {
        /// <summary>Returns all deterministic metadata diagnostics without throwing.</summary>
        public ShapeDiagnosticReport Analyze(ShapeGameMetadata metadata)
        {
            List<ShapeDiagnostic> diagnostics = new();
            if (metadata == null)
                return new(new[] { Error("shape.game.required", "Game metadata is required.") });
            if (!string.Equals(metadata.Schema, ShapeGameMetadata.CurrentSchema, StringComparison.Ordinal))
                diagnostics.Add(Error("shape.game.schema.unsupported", "Unsupported game-metadata schema.", "/schema"));
            if (string.IsNullOrWhiteSpace(metadata.Id))
                diagnostics.Add(Error("shape.game.id.required", "Game metadata requires a stable ID.", "/id"));
            ValidateAnchors(metadata.Anchors, diagnostics);
            ValidateDamageZones(metadata.DamageZones, diagnostics);
            ValidateColliders(metadata.Colliders, diagnostics);
            ValidateLods(metadata.Lods, diagnostics);
            ValidateTags(metadata.Tags, "/tags", diagnostics);
            return new(diagnostics);
        }

        private static void ValidateAnchors(
            IList<ShapeSemanticAnchor> anchors,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (anchors == null)
            {
                diagnostics.Add(Error("shape.game.anchors.required", "Game metadata requires an anchor collection.", "/anchors"));
                return;
            }
            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int index = 0; index < anchors.Count; index++)
            {
                ShapeSemanticAnchor anchor = anchors[index];
                string path = $"/anchors/{index}";
                if (anchor == null)
                {
                    diagnostics.Add(Error("shape.game.anchor.required", "Semantic anchors cannot be null.", path));
                    continue;
                }
                ValidateIdentity(anchor.Id, "anchor", path, ids, diagnostics);
                if (string.IsNullOrWhiteSpace(anchor.Role))
                    diagnostics.Add(Error("shape.game.anchor.role.required", $"Anchor '{anchor.Id}' requires a role.", $"{path}/role"));
                if (string.IsNullOrWhiteSpace(anchor.NodeId))
                    diagnostics.Add(Error("shape.game.anchor.node.required", $"Anchor '{anchor.Id}' requires a node ID.", $"{path}/nodeId"));
                if (anchor.Transform == null || !Finite(anchor.Transform.Position) || !Finite(anchor.Transform.EulerAngles) || !Finite(anchor.Transform.Scale))
                    diagnostics.Add(Error("shape.game.anchor.transform.invalid", $"Anchor '{anchor.Id}' requires finite transform values.", $"{path}/transform"));
                ValidateTags(anchor.Tags, $"{path}/tags", diagnostics);
            }
        }

        private static void ValidateDamageZones(
            IList<ShapeDamageZone> zones,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (zones == null)
            {
                diagnostics.Add(Error("shape.game.damageZones.required", "Game metadata requires a damage-zone collection.", "/damageZones"));
                return;
            }
            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int index = 0; index < zones.Count; index++)
            {
                ShapeDamageZone zone = zones[index];
                string path = $"/damageZones/{index}";
                if (zone == null)
                {
                    diagnostics.Add(Error("shape.game.damageZone.required", "Damage zones cannot be null.", path));
                    continue;
                }
                ValidateIdentity(zone.Id, "damage zone", path, ids, diagnostics);
                if (string.IsNullOrWhiteSpace(zone.NodeId))
                    diagnostics.Add(Error("shape.game.damageZone.node.required", $"Damage zone '{zone.Id}' requires a node ID.", $"{path}/nodeId"));
                if (!Finite(zone.Multiplier) || zone.Multiplier < 0f)
                    diagnostics.Add(Error("shape.game.damageZone.multiplier.invalid", $"Damage zone '{zone.Id}' multiplier must be finite and non-negative.", $"{path}/multiplier"));
                ValidateTags(zone.Tags, $"{path}/tags", diagnostics);
            }
        }

        private static void ValidateColliders(
            IList<ShapeColliderRule> colliders,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (colliders == null)
            {
                diagnostics.Add(Error("shape.game.colliders.required", "Game metadata requires a collider-rule collection.", "/colliders"));
                return;
            }
            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int index = 0; index < colliders.Count; index++)
            {
                ShapeColliderRule rule = colliders[index];
                string path = $"/colliders/{index}";
                if (rule == null)
                {
                    diagnostics.Add(Error("shape.game.collider.required", "Collider rules cannot be null.", path));
                    continue;
                }
                ValidateIdentity(rule.Id, "collider", path, ids, diagnostics);
                if (string.IsNullOrWhiteSpace(rule.NodeId))
                    diagnostics.Add(Error("shape.game.collider.node.required", $"Collider '{rule.Id}' requires a node ID.", $"{path}/nodeId"));
                if (!Enum.IsDefined(typeof(ShapeColliderKind), rule.Kind))
                    diagnostics.Add(Error("shape.game.collider.kind.invalid", $"Collider '{rule.Id}' has unsupported kind.", $"{path}/kind"));
                if (!Finite(rule.Center) || !Positive(rule.Size) || !Finite(rule.Radius) || rule.Radius <= 0f || !Finite(rule.Height) || rule.Height <= 0f)
                    diagnostics.Add(Error("shape.game.collider.dimensions.invalid", $"Collider '{rule.Id}' requires finite positive dimensions.", path));
            }
        }

        private static void ValidateLods(
            IList<ShapeLodRule> lods,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (lods == null)
            {
                diagnostics.Add(Error("shape.game.lods.required", "Game metadata requires an LOD collection.", "/lods"));
                return;
            }
            float previousHeight = float.PositiveInfinity;
            for (int index = 0; index < lods.Count; index++)
            {
                ShapeLodRule lod = lods[index];
                string path = $"/lods/{index}";
                if (lod == null)
                {
                    diagnostics.Add(Error("shape.game.lod.required", "LOD rules cannot be null.", path));
                    continue;
                }
                if (lod.Level != index)
                    diagnostics.Add(Error("shape.game.lod.level.invalid", "LOD levels must be contiguous and authored in ascending order.", $"{path}/level"));
                if (!Finite(lod.ScreenRelativeHeight) || lod.ScreenRelativeHeight < 0f || lod.ScreenRelativeHeight > 1f || lod.ScreenRelativeHeight >= previousHeight)
                    diagnostics.Add(Error("shape.game.lod.height.invalid", "LOD transition heights must be normalized and strictly descending.", $"{path}/screenRelativeHeight"));
                previousHeight = lod.ScreenRelativeHeight;
                ValidateTags(lod.NodeIds, $"{path}/nodeIds", diagnostics);
            }
        }

        private static void ValidateIdentity(
            string id,
            string label,
            string path,
            ISet<string> ids,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(id))
                diagnostics.Add(Error("shape.game.identity.required", $"Every {label} requires a stable ID.", $"{path}/id"));
            else if (!ids.Add(id))
                diagnostics.Add(Error("shape.game.identity.duplicate", $"Duplicate {label} ID '{id}'.", $"{path}/id"));
        }

        private static void ValidateTags(
            IList<string> values,
            string path,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (values == null)
            {
                diagnostics.Add(Error("shape.game.collection.required", "A game-semantic string collection is required.", path));
                return;
            }
            HashSet<string> unique = new(StringComparer.Ordinal);
            for (int index = 0; index < values.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(values[index]) || !unique.Add(values[index]))
                    diagnostics.Add(Error("shape.game.collection.item.invalid", "Game-semantic collections cannot contain empty or duplicate values.", $"{path}/{index}"));
            }
        }

        private static bool Positive(ForgeVector3 value) =>
            Finite(value) && value.X > 0f && value.Y > 0f && value.Z > 0f;

        private static bool Finite(ForgeVector3 value) =>
            Finite(value.X) && Finite(value.Y) && Finite(value.Z);

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static ShapeDiagnostic Error(string code, string message, string path = null) =>
            new(code, ShapeDiagnosticSeverity.Error, message, path: path);
    }
}
