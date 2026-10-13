using System.Collections.Generic;
using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies the allocation-free SceneReplay boundary exposed to external motion systems.
    /// </summary>
    public sealed class ShapeMotionBindingResolverTests
    {
        [Test]
        public void ResolverCachesRoleTargetRestPoseAndConstraint()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Transform.Position = new(1f, 2f, 3f);
            ShapeDefinition definition = new("Actor", root)
            {
                Rig = new()
                {
                    Type = "actor/basic",
                    Joints = new[]
                    {
                        new ShapeRigJoint("actor/root", "root",
                            new ShapeRigRotationConstraint(new(-10f, -20f, -30f), new(10f, 20f, 30f)))
                    }
                }
            };
            Target target = new("root");
            ShapeMotionBindingResolver resolver = new(definition, new TargetResolver(target));

            Assert.That(resolver.TryGetBinding("actor/root", out ShapeMotionBinding binding), Is.True);
            Assert.That(binding.RestPose.Position, Is.EqualTo(new ForgeVector3(1f, 2f, 3f)));
            Assert.That(resolver.ConstrainRotationOffset("actor/root", new(50f, 0f, -50f)),
                Is.EqualTo(new ForgeVector3(10f, 0f, -30f)));

            target.LocalPosition = ForgeVector3.Zero;
            resolver.ResetToRestPose();
            Assert.That(target.LocalPosition, Is.EqualTo(new ForgeVector3(1f, 2f, 3f)));
        }

        [Test]
        public void ResolverRejectsMissingNativeTarget()
        {
            ShapeDefinition definition = new("Actor", new("root", "Root", ShapeTypes.Group))
            {
                Rig = new() { Type = "actor/basic", Joints = new[] { new ShapeRigJoint("actor/root", "root") } }
            };

            Assert.Throws<ShapeValidationException>(() =>
                new ShapeMotionBindingResolver(definition, new TargetResolver()));
        }

        private sealed class TargetResolver : IShapeTransformResolver
        {
            private readonly Dictionary<string, IShapeTransformTarget> targets = new();

            public TargetResolver(params IShapeTransformTarget[] targets)
            {
                foreach (IShapeTransformTarget target in targets)
                    this.targets.Add(target.NodeId, target);
            }

            public bool TryGetTarget(string nodeId, out IShapeTransformTarget target) =>
                targets.TryGetValue(nodeId, out target);
        }

        private sealed class Target : IShapeTransformTarget
        {
            public Target(string nodeId) => NodeId = nodeId;
            public string NodeId { get; }
            public ForgeVector3 LocalPosition { get; set; }
            public ForgeVector3 LocalEulerAngles { get; set; }
            public ForgeVector3 LocalScale { get; set; }
        }
    }
}
