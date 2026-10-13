using System.Collections.Generic;
using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies reusable engine-adapter conformance diagnostics and state restoration.
    /// </summary>
    public sealed class ShapeAdapterConformanceValidatorTests
    {
        [Test]
        public void CompleteWritableResolverPassesAndRestoresState()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Add(new("child", "Child", ShapeTypes.Group));
            Target rootTarget  = new("root", new(1f, 2f, 3f));
            Target childTarget = new("child", new(4f, 5f, 6f));
            Resolver resolver  = new(rootTarget, childTarget);

            ShapeDiagnosticReport report = new ShapeAdapterConformanceValidator()
                .Analyze(new("Example", root), resolver);

            Assert.That(report.IsValid, Is.True);
            Assert.That(rootTarget.LocalPosition, Is.EqualTo(new ForgeVector3(1f, 2f, 3f)));
            Assert.That(childTarget.LocalPosition, Is.EqualTo(new ForgeVector3(4f, 5f, 6f)));
        }

        [Test]
        public void MissingStableNodeFailsConformance()
        {
            ShapeDefinition definition = new("Example", new("root", "Root", ShapeTypes.Group));

            ShapeDiagnosticReport report = new ShapeAdapterConformanceValidator()
                .Analyze(definition, new Resolver());

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics[0].Code, Is.EqualTo("shape.adapter.target.missing"));
        }

        private sealed class Resolver : IShapeTransformResolver
        {
            private readonly Dictionary<string, IShapeTransformTarget> targets = new();
            public Resolver(params IShapeTransformTarget[] values)
            {
                foreach (IShapeTransformTarget value in values)
                    targets.Add(value.NodeId, value);
            }
            public bool TryGetTarget(string nodeId, out IShapeTransformTarget target) => targets.TryGetValue(nodeId, out target);
        }

        private sealed class Target : IShapeTransformTarget
        {
            public Target(string nodeId, ForgeVector3 position)
            {
                NodeId        = nodeId;
                LocalPosition = position;
                LocalScale    = ForgeVector3.One;
            }
            public string NodeId { get; }
            public ForgeVector3 LocalPosition { get; set; }
            public ForgeVector3 LocalEulerAngles { get; set; }
            public ForgeVector3 LocalScale { get; set; }
        }
    }
}
