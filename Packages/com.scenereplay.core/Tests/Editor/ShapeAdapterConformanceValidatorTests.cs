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

        [Test]
        public void FullConformanceChecksIdentityHierarchyTransformsAndRestoresAllWrites()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Transform.Position = new(1f, 2f, 3f);
            root.Transform.EulerAngles = new(10f, 20f, 30f);
            root.Transform.Scale = new(2f, 2f, 2f);
            ShapeNode child = new("child", "Child", ShapeTypes.Group);
            child.Transform.Position = new(-1f, 0.5f, 4f);
            root.Add(child);

            Target rootTarget = new("root", root.Transform.Position, root.Transform.EulerAngles, root.Transform.Scale);
            Target childTarget = new("child", child.Transform.Position, child.Transform.EulerAngles, child.Transform.Scale);
            HierarchyResolver resolver = new(
                (rootTarget, string.Empty),
                (childTarget, "root"));

            ShapeDiagnosticReport report = new ShapeAdapterConformanceValidator().Analyze(
                new("Example", root),
                Descriptor(),
                resolver);

            Assert.That(report.IsValid, Is.True);
            Assert.That(rootTarget.LocalPosition, Is.EqualTo(root.Transform.Position));
            Assert.That(rootTarget.LocalEulerAngles, Is.EqualTo(root.Transform.EulerAngles));
            Assert.That(rootTarget.LocalScale, Is.EqualTo(root.Transform.Scale));
            Assert.That(childTarget.LocalPosition, Is.EqualTo(child.Transform.Position));
            Assert.That(childTarget.LocalEulerAngles, Is.EqualTo(child.Transform.EulerAngles));
            Assert.That(childTarget.LocalScale, Is.EqualTo(child.Transform.Scale));
        }

        [Test]
        public void FullConformanceReportsParentAndTransformMismatch()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            ShapeNode child = new("child", "Child", ShapeTypes.Group);
            child.Transform.Position = new(1f, 0f, 0f);
            root.Add(child);
            HierarchyResolver resolver = new(
                (new Target("root", ForgeVector3.Zero, ForgeVector3.Zero, ForgeVector3.One), string.Empty),
                (new Target("child", ForgeVector3.Zero, ForgeVector3.Zero, ForgeVector3.One), "wrong-parent"));

            ShapeDiagnosticReport report = new ShapeAdapterConformanceValidator().Analyze(
                new("Example", root),
                Descriptor(),
                resolver);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics, Has.Some.Matches<ShapeDiagnostic>(d => d.Code == "shape.adapter.parent.mismatch"));
            Assert.That(report.Diagnostics, Has.Some.Matches<ShapeDiagnostic>(d => d.Code == "shape.adapter.transform.mismatch"));
        }

        [Test]
        public void DescriptorRequiresBaseHierarchyFeatures()
        {
            ShapeAdapterDescriptor descriptor = new(
                "example/adapter",
                "example-engine",
                "1.0.0",
                new[] { ShapeAdapterFeatureIds.StableNodeIds, ShapeAdapterFeatureIds.LocalTransforms },
                ShapeAdapterUnsupportedFeaturePolicy.Reject);

            ShapeDiagnosticReport report = new ShapeAdapterDescriptorValidator().Analyze(descriptor);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics, Has.Some.Matches<ShapeDiagnostic>(d =>
                d.Code == "shape.adapter.feature.required" && d.Message.Contains(ShapeAdapterFeatureIds.Hierarchy)));
        }

        private static ShapeAdapterDescriptor Descriptor() => new(
            "example/adapter",
            "example-engine",
            "1.0.0",
            new[]
            {
                ShapeAdapterFeatureIds.StableNodeIds,
                ShapeAdapterFeatureIds.Hierarchy,
                ShapeAdapterFeatureIds.LocalTransforms
            },
            ShapeAdapterUnsupportedFeaturePolicy.Reject);

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

        private sealed class HierarchyResolver : IShapeHierarchyTransformResolver
        {
            private readonly Dictionary<string, IShapeTransformTarget> targets = new();
            private readonly Dictionary<string, string> parentIds = new();

            public HierarchyResolver(params (IShapeTransformTarget target, string parentId)[] values)
            {
                foreach ((IShapeTransformTarget target, string parentId) in values)
                {
                    targets.Add(target.NodeId, target);
                    parentIds.Add(target.NodeId, parentId);
                }
            }

            public string AdapterId => "example/adapter";

            public bool TryGetTarget(string nodeId, out IShapeTransformTarget target) => targets.TryGetValue(nodeId, out target);

            public bool TryGetParentNodeId(string nodeId, out string parentNodeId) => parentIds.TryGetValue(nodeId, out parentNodeId);
        }

        private sealed class Target : IShapeTransformTarget
        {
            public Target(string nodeId, ForgeVector3 position, ForgeVector3 rotation = default, ForgeVector3 scale = default)
            {
                NodeId        = nodeId;
                LocalPosition = position;
                LocalEulerAngles = rotation;
                LocalScale    = scale.Equals(default(ForgeVector3)) ? ForgeVector3.One : scale;
            }
            public string NodeId { get; }
            public ForgeVector3 LocalPosition { get; set; }
            public ForgeVector3 LocalEulerAngles { get; set; }
            public ForgeVector3 LocalScale { get; set; }
        }
    }
}
