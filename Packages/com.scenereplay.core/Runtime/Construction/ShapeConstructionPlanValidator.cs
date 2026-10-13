using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Validates construction-plan identity, dependencies, persisted state, and acyclicity.
    /// </summary>
    public sealed class ShapeConstructionPlanValidator
    {
        /// <summary>Returns every deterministic plan diagnostic without throwing.</summary>
        public ShapeDiagnosticReport Analyze(ShapeConstructionPlan plan)
        {
            List<ShapeDiagnostic> diagnostics = new();
            if (plan == null)
                return new(new[] { Error("shape.construction.required", "A construction plan is required.") });
            if (!string.Equals(plan.Schema, ShapeConstructionPlan.CurrentSchema, StringComparison.Ordinal))
                diagnostics.Add(Error("shape.construction.schema.unsupported", "Unsupported construction-plan schema.", "/schema"));
            if (string.IsNullOrWhiteSpace(plan.Id))
                diagnostics.Add(Error("shape.construction.id.required", "A construction plan requires a stable ID.", "/id"));
            if (plan.Passes == null || plan.Passes.Count == 0)
            {
                diagnostics.Add(Error("shape.construction.passes.required", "A construction plan requires at least one pass.", "/passes"));
                return new(diagnostics);
            }

            Dictionary<string, ShapeConstructionPass> byId = new(StringComparer.Ordinal);
            int inProgressCount = 0;
            for (int index = 0; index < plan.Passes.Count; index++)
            {
                ShapeConstructionPass pass = plan.Passes[index];
                string path = $"/passes/{index}";
                if (pass == null)
                {
                    diagnostics.Add(Error("shape.construction.pass.required", "Construction passes cannot be null.", path));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(pass.Id))
                    diagnostics.Add(Error("shape.construction.pass.id.required", "Every construction pass requires a stable ID.", $"{path}/id"));
                else if (byId.ContainsKey(pass.Id))
                    diagnostics.Add(Error("shape.construction.pass.id.duplicate", $"Duplicate construction pass ID '{pass.Id}'.", $"{path}/id"));
                else
                    byId.Add(pass.Id, pass);
                if (string.IsNullOrWhiteSpace(pass.Name))
                    diagnostics.Add(Error("shape.construction.pass.name.required", $"Pass '{pass.Id}' requires a name.", $"{path}/name"));
                if (!Enum.IsDefined(typeof(ShapeConstructionPassKind), pass.Kind))
                    diagnostics.Add(Error("shape.construction.pass.kind.invalid", $"Pass '{pass.Id}' has unsupported kind.", $"{path}/kind"));
                if (!Enum.IsDefined(typeof(ShapeConstructionPassState), pass.State))
                    diagnostics.Add(Error("shape.construction.pass.state.invalid", $"Pass '{pass.Id}' has unsupported state.", $"{path}/state"));
                if (pass.State == ShapeConstructionPassState.InProgress)
                    inProgressCount++;
                if (pass.DependsOn == null)
                    diagnostics.Add(Error("shape.construction.pass.dependencies.required", $"Pass '{pass.Id}' requires a dependency collection.", $"{path}/dependsOn"));
                if (pass.Patch == null)
                    diagnostics.Add(Error("shape.construction.pass.patch.required", $"Pass '{pass.Id}' requires a ShapePatch document.", $"{path}/patch"));
            }
            if (inProgressCount > 1)
                diagnostics.Add(Error("shape.construction.state.concurrent", "Only one construction pass may be in progress.", "/passes"));
            ValidateDependencies(plan.Passes, byId, diagnostics);
            return new(diagnostics);
        }

        private static void ValidateDependencies(
            IList<ShapeConstructionPass> passes,
            IReadOnlyDictionary<string, ShapeConstructionPass> byId,
            ICollection<ShapeDiagnostic> diagnostics)
        {
            for (int index = 0; index < passes.Count; index++)
            {
                ShapeConstructionPass pass = passes[index];
                if (pass?.DependsOn == null)
                    continue;
                HashSet<string> unique = new(StringComparer.Ordinal);
                for (int dependencyIndex = 0; dependencyIndex < pass.DependsOn.Count; dependencyIndex++)
                {
                    string dependency = pass.DependsOn[dependencyIndex];
                    string path = $"/passes/{index}/dependsOn/{dependencyIndex}";
                    if (string.IsNullOrWhiteSpace(dependency) || !unique.Add(dependency))
                        diagnostics.Add(Error("shape.construction.dependency.invalid", $"Pass '{pass.Id}' has an empty or duplicate dependency.", path));
                    else if (!byId.ContainsKey(dependency))
                        diagnostics.Add(Error("shape.construction.dependency.unknown", $"Pass '{pass.Id}' targets unknown dependency '{dependency}'.", path));
                    else if (dependency == pass.Id || Reaches(dependency, pass.Id, byId, new HashSet<string>(StringComparer.Ordinal)))
                        diagnostics.Add(Error("shape.construction.dependency.cycle", $"Pass '{pass.Id}' belongs to a dependency cycle.", path));
                }
            }
        }

        private static bool Reaches(
            string current,
            string target,
            IReadOnlyDictionary<string, ShapeConstructionPass> byId,
            ISet<string> visited)
        {
            if (current == target)
                return true;
            if (!visited.Add(current) || !byId.TryGetValue(current, out ShapeConstructionPass pass) || pass.DependsOn == null)
                return false;
            foreach (string dependency in pass.DependsOn)
            {
                if (Reaches(dependency, target, byId, visited))
                    return true;
            }
            return false;
        }

        private static ShapeDiagnostic Error(string code, string message, string path = null) =>
            new(code, ShapeDiagnosticSeverity.Error, message, path: path);
    }
}
