using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies deterministic declarative game-asset quality gates.
    /// </summary>
    public sealed class ShapeQualityGateTests
    {
        [Test]
        public void EvaluatePassesCompleteDefinitionAndReportsMetrics()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Add(new("body", "Body", "example/body"));
            ShapeDefinition definition = new("Hero", root)
            {
                Rig = new ShapeRigDefinition
                {
                    Type = "humanoid/basic",
                    Joints = new ShapeRigJoint[]
                    {
                        new(ShapeRigRoles.Root, "root"),
                        new(ShapeRigRoles.Hips, "body")
                    }
                }
            };
            ShapeQualityPolicy policy = new()
            {
                Id                    = "hero/runtime",
                RequiredRigType       = "humanoid/basic",
                MaximumNodeCount      = 4,
                MaximumHierarchyDepth = 2
            };
            policy.RequiredNodeIds.Add("body");
            policy.RequiredShapeTypes.Add("example/body");
            policy.RequiredRigRoles.Add(ShapeRigRoles.Hips);

            ShapeQualityReport report = new ShapeQualityGate().Evaluate(definition, policy);

            Assert.That(report.Passed, Is.True);
            Assert.That(report.PolicyId, Is.EqualTo("hero/runtime"));
            Assert.That(report.Metrics.NodeCount, Is.EqualTo(2));
            Assert.That(report.Metrics.HierarchyDepth, Is.EqualTo(2));
            Assert.That(report.Metrics.RigRoleCount, Is.EqualTo(2));
            Assert.That(report.Diagnostics.Diagnostics, Is.Empty);
        }

        [Test]
        public void EvaluateReportsEveryMissingSemanticRequirementInPolicyOrder()
        {
            ShapeDefinition definition = new("Prop", new ShapeNode("root", "Root", ShapeTypes.Group));
            ShapeQualityPolicy policy = new() { RequiredRigType = "humanoid/basic" };
            policy.RequiredNodeIds.Add("weapon/socket");
            policy.RequiredShapeTypes.Add("example/weapon");
            policy.RequiredRigRoles.Add(ShapeRigRoles.RightHand);

            ShapeQualityReport report = new ShapeQualityGate().Evaluate(definition, policy);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Diagnostics.Diagnostics.Count, Is.EqualTo(4));
            Assert.That(report.Diagnostics.Diagnostics[0].Code, Is.EqualTo("shape.quality.rig.type.required"));
            Assert.That(report.Diagnostics.Diagnostics[1].Code, Is.EqualTo("shape.quality.node.required"));
            Assert.That(report.Diagnostics.Diagnostics[1].Path, Is.EqualTo("/nodes/weapon~1socket"));
            Assert.That(report.Diagnostics.Diagnostics[2].Code, Is.EqualTo("shape.quality.shapeType.required"));
            Assert.That(report.Diagnostics.Diagnostics[3].Code, Is.EqualTo("shape.quality.rig.role.required"));
        }

        [Test]
        public void EvaluateRejectsInvalidPolicyBeforeInspectingDefinition()
        {
            ShapeQualityPolicy policy = new()
            {
                Schema           = "scenereplay.quality/9.0",
                MaximumNodeCount = -1
            };

            ShapeQualityReport report = new ShapeQualityGate().Evaluate(null, policy);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Metrics.NodeCount, Is.Zero);
            Assert.That(report.Diagnostics.Diagnostics.Count, Is.EqualTo(2));
            Assert.That(report.Diagnostics.Diagnostics[0].Code, Is.EqualTo("shape.quality.schema.unsupported"));
            Assert.That(report.Diagnostics.Diagnostics[1].Code, Is.EqualTo("shape.quality.maximumNodeCount.invalid"));
        }

        [Test]
        public void EvaluateReturnsDefinitionDiagnosticBeforeQualityChecks()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Add(new("root", "Duplicate", ShapeTypes.Group));

            ShapeQualityReport report = new ShapeQualityGate().Evaluate(
                new ShapeDefinition("Invalid", root),
                new ShapeQualityPolicy());

            Assert.That(report.Passed, Is.False);
            Assert.That(report.Diagnostics.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(report.Diagnostics.Diagnostics[0].Code, Is.EqualTo("shape.node.id.duplicate"));
        }
    }
}
