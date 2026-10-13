using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SceneReplay
{
    /// <summary>
    /// Compares validated ShapeDefinitions through stable semantic node IDs.
    /// </summary>
    public sealed class ShapeDefinitionDiffer
    {
        private readonly ShapeDefinitionValidator validator = new();

        /// <summary>
        /// Returns deterministic structural and authored-value differences.
        /// </summary>
        public ShapeDiffReport Compare(ShapeDefinition before, ShapeDefinition after)
        {
            if (before == null)
                throw new ArgumentNullException(nameof(before));
            if (after == null)
                throw new ArgumentNullException(nameof(after));

            validator.Validate(before);
            validator.Validate(after);

            List<ShapeDifference> differences = new();
            CompareValue(differences, null, "/schema", before.Schema, after.Schema);
            CompareValue(differences, null, "/name", before.Name, after.Name);
            CompareValue(differences, null, "/style", before.Style, after.Style);
            CompareValue(differences, null, "/rig", Format(before.Rig), Format(after.Rig));

            List<NodeRecord> beforeNodes = Index(before.Root);
            List<NodeRecord> afterNodes  = Index(after.Root);
            Dictionary<string, NodeRecord> beforeById = beforeNodes.ToDictionary(record => record.Node.Id);
            Dictionary<string, NodeRecord> afterById  = afterNodes.ToDictionary(record => record.Node.Id);

            foreach (NodeRecord record in beforeNodes)
            {
                if (!afterById.ContainsKey(record.Node.Id))
                    differences.Add(new(
                        ShapeDifferenceKind.NodeRemoved,
                        NodePath(record.Node.Id),
                        record.Node.Id,
                        record.Location));
            }

            foreach (NodeRecord record in afterNodes)
            {
                if (!beforeById.ContainsKey(record.Node.Id))
                    differences.Add(new(
                        ShapeDifferenceKind.NodeAdded,
                        NodePath(record.Node.Id),
                        record.Node.Id,
                        afterValue: record.Location));
            }

            foreach (NodeRecord beforeRecord in beforeNodes)
            {
                if (!afterById.TryGetValue(beforeRecord.Node.Id, out NodeRecord afterRecord))
                    continue;

                if (!string.Equals(beforeRecord.Location, afterRecord.Location, StringComparison.Ordinal))
                    differences.Add(new(
                        ShapeDifferenceKind.NodeMoved,
                        $"{NodePath(beforeRecord.Node.Id)}/parent",
                        beforeRecord.Node.Id,
                        beforeRecord.Location,
                        afterRecord.Location));

                CompareNode(differences, beforeRecord.Node, afterRecord.Node);
            }

            return new(differences);
        }

        private static void CompareNode(List<ShapeDifference> differences, ShapeNode before, ShapeNode after)
        {
            string path = NodePath(before.Id);
            CompareValue(differences, before.Id, $"{path}/name", before.Name, after.Name);
            CompareValue(differences, before.Id, $"{path}/type", before.Type, after.Type);
            CompareValue(differences, before.Id, $"{path}/transform/position",
                Format(before.Transform.Position), Format(after.Transform.Position));
            CompareValue(differences, before.Id, $"{path}/transform/eulerAngles",
                Format(before.Transform.EulerAngles), Format(after.Transform.EulerAngles));
            CompareValue(differences, before.Id, $"{path}/transform/scale",
                Format(before.Transform.Scale), Format(after.Transform.Scale));
            CompareValue(differences, before.Id, $"{path}/appearance/colorRole",
                before.Appearance.ColorRole, after.Appearance.ColorRole);
            CompareValue(differences, before.Id, $"{path}/appearance/hasColorOverride",
                Format(before.Appearance.HasColorOverride), Format(after.Appearance.HasColorOverride));
            CompareValue(differences, before.Id, $"{path}/appearance/color",
                Format(before.Appearance.Color), Format(after.Appearance.Color));
            CompareValue(differences, before.Id, $"{path}/mirrorAxis",
                before.MirrorAxis.ToString(), after.MirrorAxis.ToString());
            CompareValue(differences, before.Id, $"{path}/parameters",
                Format(before.Parameters), Format(after.Parameters));
            CompareValue(differences, before.Id, $"{path}/profile",
                Format(before.Profile), Format(after.Profile));
            CompareValue(differences, before.Id, $"{path}/path",
                Format(before.Path), Format(after.Path));
            CompareValue(differences, before.Id, $"{path}/profileSections",
                Format(before.ProfileSections), Format(after.ProfileSections));
            CompareValue(differences, before.Id, $"{path}/profileCageSections",
                Format(before.ProfileCageSections), Format(after.ProfileCageSections));
        }

        private static void CompareValue(
            ICollection<ShapeDifference> differences,
            string                       nodeId,
            string                       path,
            string                       before,
            string                       after)
        {
            if (string.Equals(before, after, StringComparison.Ordinal))
                return;

            differences.Add(new(ShapeDifferenceKind.ValueChanged, path, nodeId, before, after));
        }

        private static List<NodeRecord> Index(ShapeNode root)
        {
            List<NodeRecord> records = new();
            AddNode(records, root, null, 0);
            return records;
        }

        private static void AddNode(List<NodeRecord> records, ShapeNode node, string parentId, int siblingIndex)
        {
            records.Add(new(node, parentId, siblingIndex));
            for (int index = 0; index < node.Children.Count; index++)
                AddNode(records, node.Children[index], node.Id, index);
        }

        private static string NodePath(string nodeId)
        {
            return $"/nodes/{nodeId.Replace("~", "~0").Replace("/", "~1")}";
        }

        private static string Format(bool value)
        {
            return value ? "true" : "false";
        }

        private static string Format(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Format(ForgeVector2 value)
        {
            return $"[{Format(value.X)},{Format(value.Y)}]";
        }

        private static string Format(ForgeVector3 value)
        {
            return $"[{Format(value.X)},{Format(value.Y)},{Format(value.Z)}]";
        }

        private static string Format(ForgeColor value)
        {
            return $"[{Format(value.R)},{Format(value.G)},{Format(value.B)},{Format(value.A)}]";
        }

        private static string Format(IDictionary<string, float> values)
        {
            return string.Join(",", values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={Format(pair.Value)}"));
        }

        private static string Format(IEnumerable<ForgeVector2> values)
        {
            return string.Join(",", values.Select(Format));
        }

        private static string Format(IEnumerable<ForgeVector3> values)
        {
            return string.Join(",", values.Select(Format));
        }

        private static string Format(IEnumerable<ShapeProfileSection> sections)
        {
            return string.Join(",", sections.Select(section =>
                $"[{Format(section.Z)},{Format(section.Scale)},{Format(section.Offset)}]"));
        }

        private static string Format(IEnumerable<ShapeProfileCageSection> sections)
        {
            return string.Join(",", sections.Select(section =>
                $"[{Format(section.Z)}:{Format(section.Profile)}]"));
        }

        private static string Format(ShapeRigDefinition rig)
        {
            if (rig == null)
                return null;

            IEnumerable<string> joints = rig.Joints.Select(joint =>
                $"{joint.Role}={joint.NodeId}:{Format(joint.RotationConstraint)}");
            return $"{rig.Type}|{string.Join(",", joints)}";
        }

        private static string Format(ShapeRigRotationConstraint constraint)
        {
            return constraint == null
                ? "null"
                : $"{Format(constraint.Minimum)}:{Format(constraint.Maximum)}";
        }

        private sealed class NodeRecord
        {
            public NodeRecord(ShapeNode node, string parentId, int siblingIndex)
            {
                Node         = node;
                ParentId     = parentId;
                SiblingIndex = siblingIndex;
            }

            public ShapeNode Node { get; }

            public string ParentId { get; }

            public int SiblingIndex { get; }

            public string Location => $"{ParentId ?? "<root>"}@{SiblingIndex}";
        }
    }
}
