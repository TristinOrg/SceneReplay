using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Derives ready, blocked, and completed construction passes from persisted state.
    /// </summary>
    public sealed class ShapeConstructionPlanEvaluator
    {
        /// <summary>Evaluates a construction plan without mutating it.</summary>
        public ShapeConstructionPlanReport Evaluate(ShapeConstructionPlan plan)
        {
            ShapeDiagnosticReport diagnostics = new ShapeConstructionPlanValidator().Analyze(plan);
            if (!diagnostics.IsValid)
                return new(0, 0, Array.Empty<string>(), Array.Empty<string>(), diagnostics);

            Dictionary<string, ShapeConstructionPass> byId = new(StringComparer.Ordinal);
            foreach (ShapeConstructionPass pass in plan.Passes)
                byId.Add(pass.Id, pass);
            List<string> ready = new();
            List<string> blocked = new();
            int completed = 0;
            foreach (ShapeConstructionPass pass in plan.Passes)
            {
                if (Done(pass.State))
                {
                    completed++;
                    continue;
                }
                bool dependencyFailed = false;
                bool dependenciesDone = true;
                foreach (string dependencyId in pass.DependsOn)
                {
                    ShapeConstructionPass dependency = byId[dependencyId];
                    dependencyFailed |= dependency.State == ShapeConstructionPassState.Failed;
                    dependenciesDone &= Done(dependency.State);
                }
                if (dependencyFailed)
                    blocked.Add(pass.Id);
                else if (dependenciesDone)
                    ready.Add(pass.Id);
            }
            return new(plan.Passes.Count, completed, ready, blocked, diagnostics);
        }

        private static bool Done(ShapeConstructionPassState state) =>
            state == ShapeConstructionPassState.Completed || state == ShapeConstructionPassState.Skipped;
    }
}
