using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Atomically applies one ready construction pass and advances resumable plan state.
    /// </summary>
    public sealed class ShapeConstructionPlanExecutor
    {
        /// <summary>Applies a ready pass through ShapePatch and returns new definition and plan instances.</summary>
        public ShapeConstructionStepResult Apply(
            ShapeDefinition       definition,
            ShapeConstructionPlan plan,
            string                passId)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(passId))
                throw new ArgumentException("A construction pass ID is required.", nameof(passId));

            ShapeConstructionPlanReport schedule = new ShapeConstructionPlanEvaluator().Evaluate(plan);
            if (!schedule.Diagnostics.IsValid)
                return new(null, plan, schedule.Diagnostics);
            if (!Contains(schedule.ReadyPassIds, passId))
                return Failure(plan, "shape.construction.pass.notReady", $"Construction pass '{passId}' is not ready.", passId);

            ShapeConstructionPass pass = Find(plan, passId);
            ShapePatchResult patch = new ShapePatchApplier().TryApply(definition, pass.Patch);
            if (!patch.Succeeded)
                return new(null, plan, patch.Diagnostics);

            ShapeConstructionPlan advanced = Clone(plan);
            Find(advanced, passId).State = ShapeConstructionPassState.Completed;
            return new(patch.Definition, advanced, ShapeDiagnosticReport.Success);
        }

        private static ShapeConstructionStepResult Failure(
            ShapeConstructionPlan plan,
            string                code,
            string                message,
            string                passId)
        {
            ShapeDiagnostic diagnostic = new(
                code,
                ShapeDiagnosticSeverity.Error,
                message,
                path: $"/passes/{Escape(passId)}");
            return new(null, plan, new ShapeDiagnosticReport(new[] { diagnostic }));
        }

        private static ShapeConstructionPass Find(ShapeConstructionPlan plan, string passId)
        {
            foreach (ShapeConstructionPass pass in plan.Passes)
            {
                if (string.Equals(pass.Id, passId, StringComparison.Ordinal))
                    return pass;
            }
            return null;
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            foreach (string item in values)
            {
                if (string.Equals(item, value, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static ShapeConstructionPlan Clone(ShapeConstructionPlan source)
        {
            ShapeConstructionPlan result = new()
            {
                Schema       = source.Schema,
                Id           = source.Id,
                BaseRevision = source.BaseRevision
            };
            foreach (ShapeConstructionPass pass in source.Passes)
            {
                ShapeConstructionPass copy = new()
                {
                    Id              = pass.Id,
                    Name            = pass.Name,
                    Kind            = pass.Kind,
                    State           = pass.State,
                    Patch           = pass.Patch,
                    QualityPolicyId = pass.QualityPolicyId
                };
                foreach (string dependency in pass.DependsOn)
                    copy.DependsOn.Add(dependency);
                result.Passes.Add(copy);
            }
            return result;
        }

        private static string Escape(string value) => value.Replace("~", "~0").Replace("/", "~1");
    }
}
