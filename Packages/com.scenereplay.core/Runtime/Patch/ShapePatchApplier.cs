using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Applies ordered engine-neutral ShapePatch operations without mutating the source definition.
    /// </summary>
    public sealed class ShapePatchApplier
    {
        private readonly ShapeDefinitionValidator validator = new();

        /// <summary>
        /// Applies every operation to a deep copy and returns it only after final validation succeeds.
        /// </summary>
        public ShapeDefinition Apply(ShapeDefinition source, ShapePatchDocument patch)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (patch == null)
                throw new ArgumentNullException(nameof(patch));

            validator.Validate(source);
            ValidateDocument(patch);

            ShapeDefinition result = Clone(source);
            for (int index = 0; index < patch.Operations.Count; index++)
                ApplyOperation(result, patch.Operations[index], index);

            validator.Validate(result);
            return result;
        }

        /// <summary>
        /// Applies a patch atomically and returns a reverse patch that restores the source definition.
        /// </summary>
        public ShapePatchTransaction ApplyWithInverse(ShapeDefinition source, ShapePatchDocument patch)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (patch == null)
                throw new ArgumentNullException(nameof(patch));

            validator.Validate(source);
            ValidateDocument(patch);

            ShapeDefinition result = Clone(source);
            List<ShapePatchOperation> inverseOperations = new();
            for (int index = 0; index < patch.Operations.Count; index++)
            {
                ShapePatchOperation operation = patch.Operations[index];
                ShapePatchOperation inverse = CreateInverse(result, operation, index);
                ApplyOperation(result, operation, index);
                inverseOperations.Add(inverse);
            }

            validator.Validate(result);
            inverseOperations.Reverse();
            ShapePatchDocument inversePatch = new();
            foreach (ShapePatchOperation operation in inverseOperations)
                inversePatch.Operations.Add(operation);

            return new ShapePatchTransaction(result, inversePatch);
        }

        /// <summary>
        /// Attempts to apply a patch and converts authored failures into structured diagnostics.
        /// </summary>
        public ShapePatchResult TryApply(ShapeDefinition source, ShapePatchDocument patch)
        {
            try
            {
                return new(Apply(source, patch), ShapeDiagnosticReport.Success);
            }
            catch (ShapePatchException exception)
            {
                return Failure(exception.Code, exception.Message, exception.NodeId, exception.Path);
            }
            catch (ShapeValidationException exception)
            {
                return Failure(exception.Code, exception.Message, exception.NodeId, exception.Path);
            }
        }

        private static void ValidateDocument(ShapePatchDocument patch)
        {
            if (!string.Equals(patch.Schema, ShapePatchDocument.CurrentSchema, StringComparison.Ordinal))
                throw new ShapePatchException(
                    "shape.patch.schema.unsupported",
                    $"Unsupported patch schema '{patch.Schema}'.");

            if (patch.Operations == null)
                throw new ShapePatchException("shape.patch.operations.required", "A patch requires an operation collection.");
        }

        private static void ApplyOperation(ShapeDefinition definition, ShapePatchOperation operation, int index)
        {
            if (operation == null)
                throw Error("shape.patch.operation.required", "Patch operations cannot be null.", index);

            switch (operation.Kind)
            {
                case ShapePatchOperationKind.AddNode:
                    Add(definition, operation, index);
                    break;
                case ShapePatchOperationKind.RemoveNode:
                    Remove(definition, operation, index);
                    break;
                case ShapePatchOperationKind.MoveNode:
                    Move(definition, operation, index);
                    break;
                case ShapePatchOperationKind.UpdateNode:
                    Update(definition, operation, index);
                    break;
                default:
                    throw Error(
                        "shape.patch.operation.kind.unsupported",
                        $"Unsupported patch operation kind '{operation.Kind}'.",
                        index,
                        operation.NodeId);
            }
        }

        private static ShapePatchOperation CreateInverse(ShapeDefinition definition, ShapePatchOperation operation, int index)
        {
            if (operation == null)
                throw Error("shape.patch.operation.required", "Patch operations cannot be null.", index);

            switch (operation.Kind)
            {
                case ShapePatchOperationKind.AddNode:
                    if (operation.Node == null)
                        throw Error("shape.patch.node.required", "An add operation requires a node subtree.", index);
                    return new ShapePatchOperation
                    {
                        Kind = ShapePatchOperationKind.RemoveNode,
                        NodeId = operation.Node.Id
                    };

                case ShapePatchOperationKind.RemoveNode:
                {
                    ShapeNode target = RequireNode(definition.Root, operation.NodeId, index, "shape.patch.node");
                    if (ReferenceEquals(target, definition.Root))
                        throw Error("shape.patch.root.remove", "The root node cannot be removed.", index, operation.NodeId);

                    ShapeNode parent = FindParent(definition.Root, target);
                    return new ShapePatchOperation
                    {
                        Kind = ShapePatchOperationKind.AddNode,
                        ParentId = parent.Id,
                        SiblingIndex = parent.Children.IndexOf(target),
                        Node = Clone(target)
                    };
                }

                case ShapePatchOperationKind.MoveNode:
                {
                    ShapeNode target = RequireNode(definition.Root, operation.NodeId, index, "shape.patch.node");
                    if (ReferenceEquals(target, definition.Root))
                        throw Error("shape.patch.root.move", "The root node cannot be moved.", index, operation.NodeId);

                    ShapeNode parent = FindParent(definition.Root, target);
                    return new ShapePatchOperation
                    {
                        Kind = ShapePatchOperationKind.MoveNode,
                        NodeId = target.Id,
                        ParentId = parent.Id,
                        SiblingIndex = parent.Children.IndexOf(target)
                    };
                }

                case ShapePatchOperationKind.UpdateNode:
                {
                    ShapeNode target = RequireNode(definition.Root, operation.NodeId, index, "shape.patch.node");
                    if (operation.Update == null)
                        throw Error("shape.patch.update.required", "An update operation requires authored values.", index, target.Id);

                    return new ShapePatchOperation
                    {
                        Kind = ShapePatchOperationKind.UpdateNode,
                        NodeId = target.Id,
                        Update = CaptureUpdate(target, operation.Update)
                    };
                }

                default:
                    throw Error(
                        "shape.patch.operation.kind.unsupported",
                        $"Unsupported patch operation kind '{operation.Kind}'.",
                        index,
                        operation.NodeId);
            }
        }

        private static ShapeNodeUpdate CaptureUpdate(ShapeNode node, ShapeNodeUpdate requested)
        {
            ShapeNodeUpdate previous = new();
            if (requested.Name != null)
                previous.Name = node.Name;
            if (requested.Type != null)
                previous.Type = node.Type;
            if (requested.Transform != null)
                previous.Transform = Clone(node.Transform);
            if (requested.Appearance != null)
                previous.Appearance = Clone(node.Appearance);
            if (requested.MirrorAxis.HasValue)
                previous.MirrorAxis = node.MirrorAxis;
            if (requested.Parameters != null)
                previous.Parameters = new Dictionary<string, float>(node.Parameters, StringComparer.Ordinal);
            if (requested.Profile != null)
                previous.Profile = new List<ForgeVector2>(node.Profile);
            if (requested.Path != null)
                previous.Path = new List<ForgeVector3>(node.Path);
            if (requested.ProfileSections != null)
                previous.ProfileSections = Clone(node.ProfileSections);
            if (requested.ProfileCageSections != null)
                previous.ProfileCageSections = Clone(node.ProfileCageSections);
            return previous;
        }

        private static void Add(ShapeDefinition definition, ShapePatchOperation operation, int index)
        {
            if (operation.Node == null)
                throw Error("shape.patch.node.required", "An add operation requires a node subtree.", index);

            ShapeNode parent = RequireNode(definition.Root, operation.ParentId, index, "shape.patch.parent");
            if (Find(definition.Root, operation.Node.Id) != null)
                throw Error(
                    "shape.patch.node.duplicate",
                    $"Shape node ID '{operation.Node.Id}' already exists.",
                    index,
                    operation.Node.Id);

            Insert(parent.Children, Clone(operation.Node), operation.SiblingIndex, index, operation.Node.Id);
        }

        private static void Remove(ShapeDefinition definition, ShapePatchOperation operation, int index)
        {
            ShapeNode target = RequireNode(definition.Root, operation.NodeId, index, "shape.patch.node");
            if (ReferenceEquals(target, definition.Root))
                throw Error("shape.patch.root.remove", "The root node cannot be removed.", index, operation.NodeId);

            FindParent(definition.Root, target).Children.Remove(target);
        }

        private static void Move(ShapeDefinition definition, ShapePatchOperation operation, int index)
        {
            ShapeNode target = RequireNode(definition.Root, operation.NodeId, index, "shape.patch.node");
            if (ReferenceEquals(target, definition.Root))
                throw Error("shape.patch.root.move", "The root node cannot be moved.", index, operation.NodeId);

            ShapeNode destination = RequireNode(definition.Root, operation.ParentId, index, "shape.patch.parent");
            if (ReferenceEquals(target, destination) || Find(target, destination.Id) != null)
                throw Error(
                    "shape.patch.move.cycle",
                    $"Moving node '{target.Id}' below '{destination.Id}' would create a cycle.",
                    index,
                    target.Id);

            ShapeNode source = FindParent(definition.Root, target);
            source.Children.Remove(target);
            Insert(destination.Children, target, operation.SiblingIndex, index, target.Id);
        }

        private static void Update(ShapeDefinition definition, ShapePatchOperation operation, int index)
        {
            ShapeNode target = RequireNode(definition.Root, operation.NodeId, index, "shape.patch.node");
            ShapeNodeUpdate update = operation.Update;
            if (update == null)
                throw Error("shape.patch.update.required", "An update operation requires authored values.", index, target.Id);

            if (update.Name != null)
                target.Name = update.Name;
            if (update.Type != null)
                target.Type = update.Type;
            if (update.Transform != null)
                target.Transform = Clone(update.Transform);
            if (update.Appearance != null)
                target.Appearance = Clone(update.Appearance);
            if (update.MirrorAxis.HasValue)
                target.MirrorAxis = update.MirrorAxis.Value;
            if (update.Parameters != null)
                Replace(target.Parameters, update.Parameters);
            if (update.Profile != null)
                Replace(target.Profile, update.Profile);
            if (update.Path != null)
                Replace(target.Path, update.Path);
            if (update.ProfileSections != null)
                Replace(target.ProfileSections, Clone(update.ProfileSections));
            if (update.ProfileCageSections != null)
                Replace(target.ProfileCageSections, Clone(update.ProfileCageSections));
        }

        private static ShapeNode RequireNode(
            ShapeNode root,
            string    nodeId,
            int       index,
            string    codePrefix)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw Error($"{codePrefix}.required", "A stable node ID is required.", index, nodeId);

            ShapeNode node = Find(root, nodeId);
            if (node == null)
                throw Error($"{codePrefix}.unknown", $"Unknown shape node ID '{nodeId}'.", index, nodeId);

            return node;
        }

        private static void Insert(IList<ShapeNode> nodes, ShapeNode node, int siblingIndex, int index, string nodeId)
        {
            int destination = siblingIndex < 0 ? nodes.Count : siblingIndex;
            if (destination < 0 || destination > nodes.Count)
                throw Error(
                    "shape.patch.index.invalid",
                    $"Sibling index '{siblingIndex}' is outside the destination range.",
                    index,
                    nodeId);

            nodes.Insert(destination, node);
        }

        private static ShapeNode Find(ShapeNode root, string nodeId)
        {
            if (root == null)
                return null;
            if (string.Equals(root.Id, nodeId, StringComparison.Ordinal))
                return root;

            foreach (ShapeNode child in root.Children)
            {
                ShapeNode result = Find(child, nodeId);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static ShapeNode FindParent(ShapeNode root, ShapeNode target)
        {
            foreach (ShapeNode child in root.Children)
            {
                if (ReferenceEquals(child, target))
                    return root;

                ShapeNode parent = FindParent(child, target);
                if (parent != null)
                    return parent;
            }

            return null;
        }

        private static ShapePatchException Error(string code, string message, int index, string nodeId = null)
        {
            return new(code, message, index, nodeId);
        }

        private static ShapePatchResult Failure(string code, string message, string nodeId, string path)
        {
            ShapeDiagnostic diagnostic = new(code, ShapeDiagnosticSeverity.Error, message, nodeId, path);
            return new(null, new ShapeDiagnosticReport(new[] { diagnostic }));
        }

        private static ShapeDefinition Clone(ShapeDefinition source)
        {
            return new()
            {
                Schema = source.Schema,
                Name   = source.Name,
                Style  = source.Style,
                Rig    = Clone(source.Rig),
                Root   = Clone(source.Root)
            };
        }

        private static ShapeRigDefinition Clone(ShapeRigDefinition source)
        {
            if (source == null)
                return null;

            ShapeRigDefinition result = new() { Type = source.Type };
            foreach (ShapeRigJoint joint in source.Joints)
                result.Joints.Add(new ShapeRigJoint(joint.Role, joint.NodeId, Clone(joint.RotationConstraint)));
            return result;
        }

        private static ShapeRigRotationConstraint Clone(ShapeRigRotationConstraint source)
        {
            return source == null ? null : new(source.Minimum, source.Maximum);
        }

        private static ShapeNode Clone(ShapeNode source)
        {
            if (source == null)
                return null;

            ShapeNode result = new(source.Id, source.Name, source.Type)
            {
                Transform  = Clone(source.Transform),
                Appearance = Clone(source.Appearance),
                MirrorAxis = source.MirrorAxis
            };
            Replace(result.Parameters, source.Parameters);
            Replace(result.Profile, source.Profile);
            Replace(result.Path, source.Path);
            Replace(result.ProfileSections, Clone(source.ProfileSections));
            Replace(result.ProfileCageSections, Clone(source.ProfileCageSections));
            foreach (ShapeNode child in source.Children)
                result.Children.Add(Clone(child));
            return result;
        }

        private static ShapeTransform Clone(ShapeTransform source)
        {
            return source == null
                ? null
                : new ShapeTransform
                {
                    Position    = source.Position,
                    EulerAngles = source.EulerAngles,
                    Scale       = source.Scale
                };
        }

        private static ShapeAppearance Clone(ShapeAppearance source)
        {
            return source == null
                ? null
                : new ShapeAppearance
                {
                    ColorRole        = source.ColorRole,
                    HasColorOverride = source.HasColorOverride,
                    Color            = source.Color
                };
        }

        private static List<ShapeProfileSection> Clone(IEnumerable<ShapeProfileSection> source)
        {
            List<ShapeProfileSection> result = new();
            foreach (ShapeProfileSection section in source)
                result.Add(section == null ? null : new ShapeProfileSection(section.Z, section.Scale, section.Offset));
            return result;
        }

        private static List<ShapeProfileCageSection> Clone(IEnumerable<ShapeProfileCageSection> source)
        {
            List<ShapeProfileCageSection> result = new();
            foreach (ShapeProfileCageSection section in source)
                result.Add(section == null ? null : new ShapeProfileCageSection(section.Z, section.Profile));
            return result;
        }

        private static void Replace<T>(ICollection<T> destination, IEnumerable<T> source)
        {
            destination.Clear();
            foreach (T item in source)
                destination.Add(item);
        }

        private static void Replace<TKey, TValue>(
            IDictionary<TKey, TValue> destination,
            IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            destination.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in source)
                destination.Add(pair.Key, pair.Value);
        }
    }
}
