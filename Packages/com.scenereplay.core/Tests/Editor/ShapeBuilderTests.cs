using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies the engine-agnostic fluent model authoring API.
    /// </summary>
    public sealed class ShapeBuilderTests
    {
        [Test]
        public void BuildCreatesNestedModelWithoutEngineTypes()
        {
            ShapeDefinition definition = ShapeBuilder
                .Create("House")
                .WithStyle("lowpoly/village")
                .Root("house", "House", root => root
                    .Group("walls", "Walls", walls => walls
                        .Shape("front", "Front Wall", "example/cube", shape => shape
                            .Position(0f, 1f, 0f)
                            .Scale(4f, 2f, 0.2f)
                            .Parameter("topWidth", 0.65f)
                            .ColorRole("wall"))))
                .Build();

            ShapeNode wall = definition.Root.Children[0].Children[0];

            Assert.That(definition.Style, Is.EqualTo("lowpoly/village"));
            Assert.That(wall.Id, Is.EqualTo("front"));
            Assert.That(wall.Transform.Position, Is.EqualTo(new ForgeVector3(0f, 1f, 0f)));
            Assert.That(wall.Appearance.ColorRole, Is.EqualTo("wall"));
            Assert.That(wall.Parameters["topWidth"], Is.EqualTo(0.65f));
        }

        [Test]
        public void BuildRejectsDuplicateIds()
        {
            ShapeBuilder builder = ShapeBuilder
                .Create("Invalid")
                .Root("root", "Root", root => root
                    .Shape("part", "Part A", "example/cube")
                    .Shape("part", "Part B", "example/cube"));

            Assert.Throws<ShapeValidationException>(() => builder.Build());
        }

        [Test]
        public void BuildStoresEngineAgnosticMirrorAxis()
        {
            ShapeDefinition definition = ShapeBuilder
                .Create("Mirrored")
                .Root("root", "Root", root => root
                    .Shape("arm-left", "Left Arm", "example/cube", arm => arm
                        .Position(-0.75f, 0f, 0f)
                        .Mirror(ShapeMirrorAxis.X)))
                .Build();

            Assert.That(definition.Root.Children[0].MirrorAxis, Is.EqualTo(ShapeMirrorAxis.X));
        }

        [Test]
        public void BuildRejectsColorOutsideSpecificationRange()
        {
            ShapeBuilder builder = ShapeBuilder
                .Create("Invalid Color")
                .Root("root", "Root", root => root
                    .Shape("part", "Part", "example/cube", shape => shape
                        .Color(1.2f, 0f, 0f)));

            Assert.Throws<ShapeValidationException>(() => builder.Build());
        }

        [Test]
        public void BuildRejectsNonFiniteShapeParameter()
        {
            ShapeBuilder builder = ShapeBuilder
                .Create("Invalid Parameter")
                .Root("root", "Root", root => root
                    .Shape("part", "Part", "example/frustum", shape => shape
                        .Parameter("topWidth", float.NaN)));

            Assert.Throws<ShapeValidationException>(() => builder.Build());
        }

        [Test]
        public void ValidatorRejectsDefinitionsBeyondExplicitComplexityLimits()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Add(new("first", "First", ShapeTypes.Group));
            root.Add(new("second", "Second", ShapeTypes.Group));
            ShapeDefinition definition = new()
            {
                Root = root
            };
            ShapeValidationLimits limits = new(maximumNodeCount: 2);

            Assert.Throws<ShapeValidationException>(() =>
                new ShapeDefinitionValidator().Validate(definition, limits));
        }

        [Test]
        public void ValidatorRejectsDeepHierarchyBeforeGeneration()
        {
            ShapeNode root  = new("root", "Root", ShapeTypes.Group);
            ShapeNode child = new("child", "Child", ShapeTypes.Group);
            root.Add(child);
            child.Add(new("grandchild", "Grandchild", ShapeTypes.Group));
            ShapeDefinition definition = new()
            {
                Root = root
            };
            ShapeValidationLimits limits = new(maximumHierarchyDepth: 2);

            Assert.Throws<ShapeValidationException>(() =>
                new ShapeDefinitionValidator().Validate(definition, limits));
        }

        [Test]
        public void ValidatorRejectsAggregateAuthoredPointBudget()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Profile.Add(new(0f, 0f));
            root.Profile.Add(new(1f, 0f));
            root.Path.Add(new(0f, 0f, 0f));
            ShapeDefinition definition = new()
            {
                Root = root
            };
            ShapeValidationLimits limits = new(maximumAuthoredPoints: 2);

            Assert.Throws<ShapeValidationException>(() =>
                new ShapeDefinitionValidator().Validate(definition, limits));
        }

        [Test]
        public void ValidatorRejectsNonFiniteTransforms()
        {
            ShapeDefinition definition = new()
            {
                Root = new("root", "Root", ShapeTypes.Group)
                {
                    Transform = new()
                    {
                        Position = new(float.PositiveInfinity, 0f, 0f)
                    }
                }
            };

            Assert.Throws<ShapeValidationException>(() =>
                new ShapeDefinitionValidator().Validate(definition));
        }

        [Test]
        public void AnalyzerReturnsSuccessForValidDefinition()
        {
            ShapeDefinition definition = ShapeBuilder
                .Create("Valid")
                .Root("root", "Root")
                .Build();

            ShapeDiagnosticReport report = new ShapeDefinitionValidator().Analyze(definition);

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Diagnostics, Is.Empty);
        }

        [Test]
        public void AnalyzerReturnsStableNodeContextWithoutThrowing()
        {
            ShapeNode root = new("duplicate", "Root", ShapeTypes.Group);
            root.Add(new("duplicate", "Child", ShapeTypes.Group));
            ShapeDefinition definition = new()
            {
                Root = root
            };

            ShapeDiagnosticReport report = new ShapeDefinitionValidator().Analyze(definition);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(report.Diagnostics[0].Code, Is.EqualTo("shape.node.id.duplicate"));
            Assert.That(report.Diagnostics[0].NodeId, Is.EqualTo("duplicate"));
            Assert.That(report.Diagnostics[0].Path, Is.EqualTo("/root/children/0/id"));
        }

        [Test]
        public void BuildCreatesResolvableSemanticRig()
        {
            ShapeDefinition definition = ShapeBuilder
                .Create("Actor")
                .WithRig("humanoid/basic", new ShapeRigJoint(
                    ShapeRigRoles.Head,
                    "head-pivot",
                    new ShapeRigRotationConstraint(new(-20f, -45f, -15f), new(30f, 45f, 15f))))
                .Root("actor", "Actor", root => root
                    .Group("head-pivot", "Head Pivot"))
                .Build();
            ShapeRigIndex index = new(definition.Rig);

            Assert.That(index.TryGetNodeId(ShapeRigRoles.Head, out string nodeId), Is.True);
            Assert.That(nodeId, Is.EqualTo("head-pivot"));
            Assert.That(index.ConstrainRotationOffset(ShapeRigRoles.Head, new(-90f, 10f, 40f)),
                Is.EqualTo(new ForgeVector3(-20f, 10f, 15f)));
            Assert.That(index.ConstrainRotationOffset("custom/unconstrained", new(1f, 2f, 3f)),
                Is.EqualTo(new ForgeVector3(1f, 2f, 3f)));
        }

        [Test]
        public void BuildRejectsSemanticRigTargetingUnknownNode()
        {
            ShapeBuilder builder = ShapeBuilder
                .Create("Actor")
                .WithRig("humanoid/basic", new ShapeRigJoint(ShapeRigRoles.Head, "missing"))
                .Root("actor", "Actor");

            Assert.Throws<ShapeValidationException>(() => builder.Build());
        }

        [Test]
        public void BuildRejectsDuplicateSemanticRigRoles()
        {
            ShapeBuilder builder = ShapeBuilder
                .Create("Actor")
                .WithRig("humanoid/basic",
                    new ShapeRigJoint(ShapeRigRoles.Head, "actor"),
                    new ShapeRigJoint(ShapeRigRoles.Head, "actor"))
                .Root("actor", "Actor");

            Assert.Throws<ShapeValidationException>(() => builder.Build());
        }

        [Test]
        public void BuildRejectsInvertedSemanticRigRotationLimits()
        {
            ShapeBuilder builder = ShapeBuilder
                .Create("Actor")
                .WithRig("humanoid/basic", new ShapeRigJoint(
                    ShapeRigRoles.Head,
                    "actor",
                    new ShapeRigRotationConstraint(new(10f, 0f, 0f), new(-10f, 0f, 0f))))
                .Root("actor", "Actor");

            Assert.Throws<ShapeValidationException>(() => builder.Build());
        }

        [Test]
        public void HumanoidRigRequiresEveryCanonicalRole()
        {
            ShapeRigDefinition rig = new()
            {
                Joints = new ShapeRigJoint[]
                {
                    new(ShapeRigRoles.Hips, "hips")
                }
            };

            ShapeValidationException exception = Assert.Throws<ShapeValidationException>(() =>
                ShapeHumanoidRig.ValidateRequiredRoles(rig));

            Assert.That(exception.Message, Does.Contain(ShapeRigRoles.Spine));
        }

        [Test]
        public void HumanoidRigRejectsMalformedJointCollection()
        {
            ShapeRigDefinition rig = new()
            {
                Joints = null
            };

            Assert.Throws<ShapeValidationException>(() => ShapeHumanoidRig.ValidateRequiredRoles(rig));
        }
    }
}
