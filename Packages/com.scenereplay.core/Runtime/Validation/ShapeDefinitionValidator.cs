using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Validates engine-agnostic SceneReplay definitions before generation.
    /// </summary>
    public sealed class ShapeDefinitionValidator
    {
        /// <summary>
        /// Analyzes a definition without throwing for authored validation failures.
        /// </summary>
        public ShapeDiagnosticReport Analyze(ShapeDefinition definition)
        {
            return Analyze(definition, ShapeValidationLimits.Default);
        }

        /// <summary>
        /// Analyzes a definition against explicit authored-complexity limits.
        /// </summary>
        public ShapeDiagnosticReport Analyze(ShapeDefinition definition, ShapeValidationLimits limits)
        {
            if (limits == null)
                throw new ArgumentNullException(nameof(limits));

            if (definition == null)
                return Error("shape.definition.required", "A shape definition is required.");

            try
            {
                Validate(definition, limits);
                return ShapeDiagnosticReport.Success;
            }
            catch (ShapeValidationException exception)
            {
                return Error(exception.Code, exception.Message, exception.NodeId, exception.Path);
            }
        }

        /// <summary>
        /// Validates schema compatibility, node identity, and required node data.
        /// </summary>
        public void Validate(ShapeDefinition definition)
        {
            Validate(definition, ShapeValidationLimits.Default);
        }

        /// <summary>
        /// Validates a definition against explicit authored-complexity limits.
        /// </summary>
        public void Validate(ShapeDefinition definition, ShapeValidationLimits limits)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (limits == null)
                throw new ArgumentNullException(nameof(limits));

            if (!string.Equals(definition.Schema, ShapeDefinition.CurrentSchema, StringComparison.Ordinal))
                throw new ShapeValidationException(
                    "shape.schema.unsupported",
                    $"Unsupported shape schema '{definition.Schema}'.",
                    path: "/schema");

            if (definition.Root != null && definition.Root.MirrorAxis != ShapeMirrorAxis.None)
                throw new ShapeValidationException(
                    "shape.root.mirror",
                    "The root node cannot create a mirrored sibling.",
                    definition.Root.Id,
                    "/root/mirrorAxis");

            HashSet<string> nodeIds = new(StringComparer.Ordinal);
            ValidationState state   = new(limits);
            ValidateNode(definition.Root, nodeIds, state, 1, "/root");
            ValidateRig(definition.Rig, nodeIds);
        }

        private static void ValidateRig(ShapeRigDefinition rig, HashSet<string> nodeIds)
        {
            if (rig == null)
                return;

            if (string.IsNullOrWhiteSpace(rig.Type))
                throw new ShapeValidationException("A semantic rig requires a type.");

            if (rig.Joints == null)
                throw new ShapeValidationException("A semantic rig requires a joint collection.");

            HashSet<string> roles = new(StringComparer.Ordinal);
            foreach (ShapeRigJoint joint in rig.Joints)
            {
                if (joint == null || string.IsNullOrWhiteSpace(joint.Role))
                    throw new ShapeValidationException("Every semantic rig joint requires a role.");

                if (!roles.Add(joint.Role))
                    throw new ShapeValidationException($"Duplicate semantic rig role '{joint.Role}'.");

                if (string.IsNullOrWhiteSpace(joint.NodeId) || !nodeIds.Contains(joint.NodeId))
                    throw new ShapeValidationException(
                        $"Semantic rig role '{joint.Role}' targets unknown node '{joint.NodeId}'.");

                ValidateRotationConstraint(joint);
            }
        }

        private static void ValidateRotationConstraint(ShapeRigJoint joint)
        {
            ShapeRigRotationConstraint constraint = joint.RotationConstraint;
            if (constraint == null)
                return;

            ValidateFinite(constraint.Minimum, joint.Role);
            ValidateFinite(constraint.Maximum, joint.Role);
            if (constraint.Minimum.X > constraint.Maximum.X ||
                constraint.Minimum.Y > constraint.Maximum.Y ||
                constraint.Minimum.Z > constraint.Maximum.Z)
                throw new ShapeValidationException(
                    $"Semantic rig role '{joint.Role}' rotation minimum cannot exceed its maximum.");
        }

        private static void ValidateFinite(ForgeVector3 value, string role)
        {
            if (float.IsNaN(value.X) || float.IsInfinity(value.X) ||
                float.IsNaN(value.Y) || float.IsInfinity(value.Y) ||
                float.IsNaN(value.Z) || float.IsInfinity(value.Z))
                throw new ShapeValidationException(
                    $"Semantic rig role '{role}' rotation limits must be finite.");
        }

        private static void ValidateNode(
            ShapeNode       node,
            HashSet<string> nodeIds,
            ValidationState state,
            int             depth,
            string          path)
        {
            if (node == null)
                throw new ShapeValidationException(
                    "shape.node.required",
                    "Shape definitions cannot contain null nodes.",
                    path: path);

            state.AddNode(depth);

            if (string.IsNullOrWhiteSpace(node.Id))
                throw new ShapeValidationException(
                    "shape.node.id.required",
                    "Every shape node requires a stable ID.",
                    path: $"{path}/id");

            if (!nodeIds.Add(node.Id))
                throw new ShapeValidationException(
                    "shape.node.id.duplicate",
                    $"Duplicate shape node ID '{node.Id}'.",
                    node.Id,
                    $"{path}/id");

            if (string.IsNullOrWhiteSpace(node.Type))
                throw new ShapeValidationException($"Shape node '{node.Id}' requires a type.");

            if (node.Transform == null)
                throw new ShapeValidationException($"Shape node '{node.Id}' requires a transform.");

            ValidateTransform(node);

            if (node.Appearance == null)
                throw new ShapeValidationException($"Shape node '{node.Id}' requires appearance data.");

            if (!Enum.IsDefined(typeof(ShapeMirrorAxis), node.MirrorAxis))
                throw new ShapeValidationException($"Shape node '{node.Id}' has an unsupported mirror axis.");

            if (node.Appearance.HasColorOverride)
                ForgeColorValidator.Validate(node.Appearance.Color, $"Shape node '{node.Id}'");

            if (node.Parameters == null)
                throw new ShapeValidationException($"Shape node '{node.Id}' requires a parameter collection.");

            foreach (KeyValuePair<string, float> parameter in node.Parameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.Key))
                    throw new ShapeValidationException($"Shape node '{node.Id}' has an empty parameter name.");

                if (float.IsNaN(parameter.Value) || float.IsInfinity(parameter.Value))
                    throw new ShapeValidationException(
                        $"Shape node '{node.Id}' parameter '{parameter.Key}' must be finite.");
            }

            if (node.Profile == null)
                throw new ShapeValidationException($"Shape node '{node.Id}' requires a profile collection.");

            state.AddPoints(node.Profile.Count);

            foreach (ForgeVector2 point in node.Profile)
            {
                if (float.IsNaN(point.X) || float.IsInfinity(point.X) ||
                    float.IsNaN(point.Y) || float.IsInfinity(point.Y))
                    throw new ShapeValidationException($"Shape node '{node.Id}' profile points must be finite.");
            }

            if (node.Path == null)
                throw new ShapeValidationException($"Shape node '{node.Id}' requires a path collection.");

            state.AddPoints(node.Path.Count);

            foreach (ForgeVector3 point in node.Path)
            {
                if (float.IsNaN(point.X) || float.IsInfinity(point.X) ||
                    float.IsNaN(point.Y) || float.IsInfinity(point.Y) ||
                    float.IsNaN(point.Z) || float.IsInfinity(point.Z))
                    throw new ShapeValidationException($"Shape node '{node.Id}' path points must be finite.");
            }

            if (node.ProfileSections == null)
                throw new ShapeValidationException($"Shape node '{node.Id}' requires a profile section collection.");

            state.AddPoints(node.ProfileSections.Count);

            foreach (ShapeProfileSection section in node.ProfileSections)
            {
                if (section == null)
                    throw new ShapeValidationException($"Shape node '{node.Id}' cannot contain null profile sections.");

                if (float.IsNaN(section.Z) || float.IsInfinity(section.Z) ||
                    float.IsNaN(section.Scale.X) || float.IsInfinity(section.Scale.X) ||
                    float.IsNaN(section.Scale.Y) || float.IsInfinity(section.Scale.Y) ||
                    float.IsNaN(section.Offset.X) || float.IsInfinity(section.Offset.X) ||
                    float.IsNaN(section.Offset.Y) || float.IsInfinity(section.Offset.Y))
                    throw new ShapeValidationException($"Shape node '{node.Id}' profile sections must be finite.");
            }

            if (node.ProfileCageSections == null)
                throw new ShapeValidationException($"Shape node '{node.Id}' requires a profile cage section collection.");

            foreach (ShapeProfileCageSection section in node.ProfileCageSections)
            {
                if (section == null)
                    throw new ShapeValidationException(
                        $"Shape node '{node.Id}' cannot contain null profile cage sections.");

                if (float.IsNaN(section.Z) || float.IsInfinity(section.Z) || section.Profile == null)
                    throw new ShapeValidationException(
                        $"Shape node '{node.Id}' profile cage sections must be finite and contain profiles.");

                state.AddPoints(section.Profile.Count);

                foreach (ForgeVector2 point in section.Profile)
                {
                    if (float.IsNaN(point.X) || float.IsInfinity(point.X) ||
                        float.IsNaN(point.Y) || float.IsInfinity(point.Y))
                        throw new ShapeValidationException(
                            $"Shape node '{node.Id}' profile cage points must be finite.");
                }
            }

            if (node.Children == null)
                throw new ShapeValidationException($"Shape node '{node.Id}' requires a child collection.");

            for (int index = 0; index < node.Children.Count; index++)
                ValidateNode(node.Children[index], nodeIds, state, depth + 1, $"{path}/children/{index}");
        }

        private static ShapeDiagnosticReport Error(
            string code,
            string message,
            string nodeId = null,
            string path = null)
        {
            return new(new[] { new ShapeDiagnostic(code, ShapeDiagnosticSeverity.Error, message, nodeId, path) });
        }

        private static void ValidateTransform(ShapeNode node)
        {
            ShapeTransform transform = node.Transform;
            if (!IsFinite(transform.Position) ||
                !IsFinite(transform.EulerAngles) ||
                !IsFinite(transform.Scale))
                throw new ShapeValidationException($"Shape node '{node.Id}' transform values must be finite.");
        }

        private static bool IsFinite(ForgeVector3 value)
        {
            return !float.IsNaN(value.X) && !float.IsInfinity(value.X) &&
                   !float.IsNaN(value.Y) && !float.IsInfinity(value.Y) &&
                   !float.IsNaN(value.Z) && !float.IsInfinity(value.Z);
        }

        /// <summary>
        /// Tracks aggregate definition cost without exposing mutable counters.
        /// </summary>
        private sealed class ValidationState
        {
            private readonly ShapeValidationLimits limits;
            private int                            nodeCount;
            private int                            pointCount;

            public ValidationState(ShapeValidationLimits limits)
            {
                this.limits = limits;
            }

            public void AddNode(int depth)
            {
                if (depth > limits.MaximumHierarchyDepth)
                    throw new ShapeValidationException(
                        $"Shape hierarchy exceeds the maximum depth of {limits.MaximumHierarchyDepth}.");

                nodeCount++;
                if (nodeCount > limits.MaximumNodeCount)
                    throw new ShapeValidationException(
                        $"Shape definition exceeds the maximum node count of {limits.MaximumNodeCount}.");
            }

            public void AddPoints(int count)
            {
                if (count > limits.MaximumAuthoredPoints - pointCount)
                    throw new ShapeValidationException(
                        $"Shape definition exceeds the maximum authored point count of {limits.MaximumAuthoredPoints}.");

                pointCount += count;
            }
        }
    }
}
