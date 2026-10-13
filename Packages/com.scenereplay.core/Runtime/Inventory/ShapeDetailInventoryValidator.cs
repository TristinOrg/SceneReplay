using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Validates semantic detail inventories before staged model construction.
    /// </summary>
    public sealed class ShapeDetailInventoryValidator
    {
        /// <summary>Returns all deterministic inventory diagnostics without throwing.</summary>
        public ShapeDiagnosticReport Analyze(ShapeDetailInventory inventory)
        {
            List<ShapeDiagnostic> diagnostics = new();
            if (inventory == null)
                return Report(Error("shape.inventory.required", "A detail inventory is required."));
            if (!string.Equals(inventory.Schema, ShapeDetailInventory.CurrentSchema, StringComparison.Ordinal))
                diagnostics.Add(Error("shape.inventory.schema.unsupported", "Unsupported detail-inventory schema.", "/schema"));
            if (string.IsNullOrWhiteSpace(inventory.Subject))
                diagnostics.Add(Error("shape.inventory.subject.required", "A detail inventory requires a subject.", "/subject"));
            if (inventory.Details == null)
            {
                diagnostics.Add(Error("shape.inventory.details.required", "A detail inventory requires a detail collection.", "/details"));
                return new(diagnostics);
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int index = 0; index < inventory.Details.Count; index++)
            {
                ShapeDetailItem item = inventory.Details[index];
                string path = $"/details/{index}";
                if (item == null)
                {
                    diagnostics.Add(Error("shape.inventory.detail.required", "Inventory details cannot be null.", path));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.Id))
                    diagnostics.Add(Error("shape.inventory.detail.id.required", "Every detail requires a stable ID.", $"{path}/id"));
                else if (!ids.Add(item.Id))
                    diagnostics.Add(Error("shape.inventory.detail.id.duplicate", $"Duplicate detail ID '{item.Id}'.", $"{path}/id"));
                if (string.IsNullOrWhiteSpace(item.Name))
                    diagnostics.Add(Error("shape.inventory.detail.name.required", $"Detail '{item.Id}' requires a name.", $"{path}/name"));
                if (item.RepeatCount < 1)
                    diagnostics.Add(Error("shape.inventory.detail.repeat.invalid", $"Detail '{item.Id}' repeat count must be positive.", $"{path}/repeatCount"));
                if (!Finite(item.Confidence) || item.Confidence < 0f || item.Confidence > 1f)
                    diagnostics.Add(Error("shape.inventory.detail.confidence.invalid", $"Detail '{item.Id}' confidence must be between zero and one.", $"{path}/confidence"));
                ValidateTags(item, index, diagnostics);
            }

            ValidateParents(inventory.Details, ids, diagnostics);
            return new(diagnostics);
        }

        private static void ValidateParents(
            IList<ShapeDetailItem> details,
            ISet<string>           ids,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            Dictionary<string, ShapeDetailItem> byId = new(StringComparer.Ordinal);
            foreach (ShapeDetailItem detail in details)
            {
                if (detail != null && !string.IsNullOrWhiteSpace(detail.Id) && !byId.ContainsKey(detail.Id))
                    byId.Add(detail.Id, detail);
            }

            for (int index = 0; index < details.Count; index++)
            {
                ShapeDetailItem item = details[index];
                if (item == null || string.IsNullOrWhiteSpace(item.ParentId))
                    continue;
                if (item.ParentId == item.Id)
                    diagnostics.Add(Error("shape.inventory.detail.parent.self", $"Detail '{item.Id}' cannot parent itself.", $"/details/{index}/parentId"));
                else if (!ids.Contains(item.ParentId))
                    diagnostics.Add(Error("shape.inventory.detail.parent.unknown", $"Detail '{item.Id}' targets unknown parent '{item.ParentId}'.", $"/details/{index}/parentId"));
                else if (HasCycle(item, byId))
                    diagnostics.Add(Error("shape.inventory.detail.parent.cycle", $"Detail '{item.Id}' belongs to a parent cycle.", $"/details/{index}/parentId"));
            }
        }

        private static bool HasCycle(ShapeDetailItem item, IReadOnlyDictionary<string, ShapeDetailItem> byId)
        {
            HashSet<string> visited = new(StringComparer.Ordinal) { item.Id };
            string parentId = item.ParentId;
            while (!string.IsNullOrWhiteSpace(parentId) && byId.TryGetValue(parentId, out ShapeDetailItem parent))
            {
                if (!visited.Add(parentId))
                    return true;
                parentId = parent.ParentId;
            }
            return false;
        }

        private static void ValidateTags(
            ShapeDetailItem item,
            int             itemIndex,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (item.Tags == null)
            {
                diagnostics.Add(Error("shape.inventory.detail.tags.required", $"Detail '{item.Id}' requires a tag collection.", $"/details/{itemIndex}/tags"));
                return;
            }
            HashSet<string> tags = new(StringComparer.Ordinal);
            for (int index = 0; index < item.Tags.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(item.Tags[index]) || !tags.Add(item.Tags[index]))
                    diagnostics.Add(Error("shape.inventory.detail.tag.invalid", $"Detail '{item.Id}' has an empty or duplicate tag.", $"/details/{itemIndex}/tags/{index}"));
            }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static ShapeDiagnostic Error(string code, string message, string path = null) =>
            new(code, ShapeDiagnosticSeverity.Error, message, path: path);

        private static ShapeDiagnosticReport Report(ShapeDiagnostic diagnostic) => new(new[] { diagnostic });
    }
}
