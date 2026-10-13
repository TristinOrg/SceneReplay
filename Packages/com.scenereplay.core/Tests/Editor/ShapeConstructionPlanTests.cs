using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies resumable construction scheduling and atomic pass execution.
    /// </summary>
    public sealed class ShapeConstructionPlanTests
    {
        [Test]
        public void EvaluatorFindsReadyAndBlockedPassesInAuthoredOrder()
        {
            ShapeConstructionPlan plan = Plan();
            plan.Passes.Add(Pass("structure", ShapeConstructionPassState.Completed));
            ShapeConstructionPass details = Pass("details", ShapeConstructionPassState.Pending, "structure");
            plan.Passes.Add(details);
            plan.Passes.Add(Pass("appearance", ShapeConstructionPassState.Pending, "details"));

            ShapeConstructionPlanReport report = new ShapeConstructionPlanEvaluator().Evaluate(plan);

            Assert.That(report.Diagnostics.IsValid, Is.True);
            Assert.That(report.CompletedCount, Is.EqualTo(1));
            Assert.That(report.ReadyPassIds, Is.EqualTo(new[] { "details" }));
            Assert.That(report.BlockedPassIds, Is.Empty);
        }

        [Test]
        public void ExecutorAppliesPatchAndAdvancesPlanWithoutMutatingSources()
        {
            ShapeDefinition definition = new("Hero", new ShapeNode("root", "Root", ShapeTypes.Group));
            ShapeConstructionPlan plan = Plan();
            ShapeConstructionPass pass = Pass("structure", ShapeConstructionPassState.Pending);
            pass.Patch.Operations.Add(new()
            {
                Kind = ShapePatchOperationKind.AddNode,
                ParentId = "root",
                Node = new ShapeNode("body", "Body", ShapeTypes.Group)
            });
            plan.Passes.Add(pass);

            ShapeConstructionStepResult result = new ShapeConstructionPlanExecutor().Apply(definition, plan, "structure");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Definition.Root.Children[0].Id, Is.EqualTo("body"));
            Assert.That(result.Plan.Passes[0].State, Is.EqualTo(ShapeConstructionPassState.Completed));
            Assert.That(definition.Root.Children, Is.Empty);
            Assert.That(plan.Passes[0].State, Is.EqualTo(ShapeConstructionPassState.Pending));
        }

        [Test]
        public void ExecutorRejectsOutOfOrderPassWithoutMutatingDefinition()
        {
            ShapeDefinition definition = new("Hero", new ShapeNode("root", "Root", ShapeTypes.Group));
            ShapeConstructionPlan plan = Plan();
            plan.Passes.Add(Pass("structure", ShapeConstructionPassState.Pending));
            plan.Passes.Add(Pass("details", ShapeConstructionPassState.Pending, "structure"));

            ShapeConstructionStepResult result = new ShapeConstructionPlanExecutor().Apply(definition, plan, "details");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Diagnostics.Diagnostics[0].Code, Is.EqualTo("shape.construction.pass.notReady"));
            Assert.That(definition.Root.Children, Is.Empty);
        }

        [Test]
        public void ValidatorRejectsDependencyCycles()
        {
            ShapeConstructionPlan plan = Plan();
            plan.Passes.Add(Pass("a", ShapeConstructionPassState.Pending, "b"));
            plan.Passes.Add(Pass("b", ShapeConstructionPassState.Pending, "a"));

            ShapeDiagnosticReport report = new ShapeConstructionPlanValidator().Analyze(plan);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics[0].Code, Is.EqualTo("shape.construction.dependency.cycle"));
        }

        private static ShapeConstructionPlan Plan() => new() { Id = "hero/build-1" };

        private static ShapeConstructionPass Pass(
            string id,
            ShapeConstructionPassState state,
            params string[] dependencies)
        {
            ShapeConstructionPass pass = new() { Id = id, Name = id, State = state };
            foreach (string dependency in dependencies)
                pass.DependsOn.Add(dependency);
            return pass;
        }
    }
}
