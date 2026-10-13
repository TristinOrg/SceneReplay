using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Evaluates validated ShapeDefinitions against declarative game-asset quality policies.
    /// </summary>
    public sealed class ShapeQualityGate
    {
        private readonly ShapeDefinitionValidator validator = new();

        /// <summary>
        /// Measures a definition and returns stable diagnostics for every failed policy requirement.
        /// </summary>
        public ShapeQualityReport Evaluate(ShapeDefinition definition, ShapeQualityPolicy policy)
        {
            List<ShapeDiagnostic> diagnostics = ValidatePolicy(policy);
            if (diagnostics.Count > 0)
                return Report(policy?.Id, new(0, 0, 0), diagnostics);

            ShapeDiagnosticReport validation = validator.Analyze(definition);
            if (!validation.IsValid)
                return new(policy.Id, new(0, 0, 0), validation);

            EvaluationState state = new();
            Collect(definition.Root, 1, state);
            CollectRig(definition.Rig, state);
            EvaluateRequirements(definition, policy, state, diagnostics);

            return Report(
                policy.Id,
                new(state.NodeCount, state.HierarchyDepth, state.RigRoles.Count),
                diagnostics);
        }

        private static List<ShapeDiagnostic> ValidatePolicy(ShapeQualityPolicy policy)
        {
            List<ShapeDiagnostic> diagnostics = new();
            if (policy == null)
            {
                diagnostics.Add(Error("shape.quality.policy.required", "A quality policy is required."));
                return diagnostics;
            }

            if (!string.Equals(policy.Schema, ShapeQualityPolicy.CurrentSchema, StringComparison.Ordinal))
                diagnostics.Add(Error(
                    "shape.quality.schema.unsupported",
                    $"Unsupported quality-policy schema '{policy.Schema}'.",
                    path: "/schema"));
            if (string.IsNullOrWhiteSpace(policy.Id))
                diagnostics.Add(Error("shape.quality.id.required", "A quality policy requires a stable ID.", path: "/id"));
            if (policy.MaximumNodeCount < 0)
                diagnostics.Add(Error(
                    "shape.quality.maximumNodeCount.invalid",
                    "Maximum node count cannot be negative.",
                    path: "/maximumNodeCount"));
            if (policy.MaximumHierarchyDepth < 0)
                diagnostics.Add(Error(
                    "shape.quality.maximumHierarchyDepth.invalid",
                    "Maximum hierarchy depth cannot be negative.",
                    path: "/maximumHierarchyDepth"));

            ValidateRequirements(policy.RequiredNodeIds, "requiredNodeIds", diagnostics);
            ValidateRequirements(policy.RequiredShapeTypes, "requiredShapeTypes", diagnostics);
            ValidateRequirements(policy.RequiredRigRoles, "requiredRigRoles", diagnostics);
            return diagnostics;
        }

        private static void ValidateRequirements(
            IList<string>         requirements,
            string                property,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (requirements == null)
            {
                diagnostics.Add(Error(
                    "shape.quality.requirements.required",
                    $"Quality policy property '{property}' requires a collection.",
                    path: $"/{property}"));
                return;
            }

            HashSet<string> values = new(StringComparer.Ordinal);
            for (int index = 0; index < requirements.Count; index++)
            {
                string value = requirements[index];
                if (string.IsNullOrWhiteSpace(value))
                    diagnostics.Add(Error(
                        "shape.quality.requirement.invalid",
                        $"Quality policy property '{property}' cannot contain an empty value.",
                        path: $"/{property}/{index}"));
                else if (!values.Add(value))
                    diagnostics.Add(Error(
                        "shape.quality.requirement.duplicate",
                        $"Quality policy property '{property}' contains duplicate value '{value}'.",
                        path: $"/{property}/{index}"));
            }
        }

        private static void EvaluateRequirements(
            ShapeDefinition       definition,
            ShapeQualityPolicy    policy,
            EvaluationState       state,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            if (!string.IsNullOrWhiteSpace(policy.RequiredRigType) &&
                !string.Equals(definition.Rig?.Type, policy.RequiredRigType, StringComparison.Ordinal))
                diagnostics.Add(Error(
                    "shape.quality.rig.type.required",
                    $"Quality policy requires rig type '{policy.RequiredRigType}'.",
                    path: "/rig/type"));

            foreach (string nodeId in policy.RequiredNodeIds)
            {
                if (!state.NodeIds.Contains(nodeId))
                    diagnostics.Add(Error(
                        "shape.quality.node.required",
                        $"Quality policy requires node '{nodeId}'.",
                        nodeId,
                        $"/nodes/{Escape(nodeId)}"));
            }

            foreach (string shapeType in policy.RequiredShapeTypes)
            {
                if (!state.ShapeTypes.Contains(shapeType))
                    diagnostics.Add(Error(
                        "shape.quality.shapeType.required",
                        $"Quality policy requires shape type '{shapeType}'.",
                        path: $"/shapeTypes/{Escape(shapeType)}"));
            }

            foreach (string role in policy.RequiredRigRoles)
            {
                if (!state.RigRoles.Contains(role))
                    diagnostics.Add(Error(
                        "shape.quality.rig.role.required",
                        $"Quality policy requires semantic rig role '{role}'.",
                        path: $"/rig/roles/{Escape(role)}"));
            }

            if (policy.MaximumNodeCount > 0 && state.NodeCount > policy.MaximumNodeCount)
                diagnostics.Add(Error(
                    "shape.quality.nodeCount.exceeded",
                    $"Shape has {state.NodeCount} nodes; policy maximum is {policy.MaximumNodeCount}.",
                    path: "/root"));
            if (policy.MaximumHierarchyDepth > 0 && state.HierarchyDepth > policy.MaximumHierarchyDepth)
                diagnostics.Add(Error(
                    "shape.quality.hierarchyDepth.exceeded",
                    $"Shape depth is {state.HierarchyDepth}; policy maximum is {policy.MaximumHierarchyDepth}.",
                    path: "/root"));
        }

        private static void Collect(ShapeNode node, int depth, EvaluationState state)
        {
            state.NodeCount++;
            state.HierarchyDepth = Math.Max(state.HierarchyDepth, depth);
            state.NodeIds.Add(node.Id);
            state.ShapeTypes.Add(node.Type);
            foreach (ShapeNode child in node.Children)
                Collect(child, depth + 1, state);
        }

        private static void CollectRig(ShapeRigDefinition rig, EvaluationState state)
        {
            if (rig == null)
                return;

            foreach (ShapeRigJoint joint in rig.Joints)
                state.RigRoles.Add(joint.Role);
        }

        private static ShapeQualityReport Report(
            string                     policyId,
            ShapeQualityMetrics        metrics,
            IEnumerable<ShapeDiagnostic> diagnostics)
        {
            return new(policyId, metrics, new ShapeDiagnosticReport(diagnostics));
        }

        private static ShapeDiagnostic Error(
            string code,
            string message,
            string nodeId = null,
            string path = null)
        {
            return new(code, ShapeDiagnosticSeverity.Error, message, nodeId, path);
        }

        private static string Escape(string value)
        {
            return value.Replace("~", "~0").Replace("/", "~1");
        }

        /// <summary>Collects one validated definition without repeated hierarchy traversal.</summary>
        private sealed class EvaluationState
        {
            public int NodeCount { get; set; }

            public int HierarchyDepth { get; set; }

            public HashSet<string> NodeIds { get; } = new(StringComparer.Ordinal);

            public HashSet<string> ShapeTypes { get; } = new(StringComparer.Ordinal);

            public HashSet<string> RigRoles { get; } = new(StringComparer.Ordinal);
        }
    }
}
