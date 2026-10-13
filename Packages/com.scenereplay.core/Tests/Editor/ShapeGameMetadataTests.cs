using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies engine-neutral gameplay metadata validation and stable-node coverage.
    /// </summary>
    public sealed class ShapeGameMetadataTests
    {
        [Test]
        public void AnalyzerAcceptsCompleteGameReadyMetadata()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Add(new("hand", "Hand", ShapeTypes.Group));
            ShapeGameMetadata metadata = Metadata();
            metadata.Anchors.Add(new()
            {
                Id = "weapon-grip", Role = ShapeSemanticAnchorRoles.HandGrip, NodeId = "hand"
            });
            metadata.DamageZones.Add(new() { Id = "body", NodeId = "root", Multiplier = 1f });
            metadata.Colliders.Add(new() { Id = "body", NodeId = "root", Kind = ShapeColliderKind.Capsule });
            ShapeLodRule lod = new() { Level = 0, ScreenRelativeHeight = 0.6f };
            lod.NodeIds.Add("root");
            metadata.Lods.Add(lod);

            ShapeGameMetadataReport report = new ShapeGameMetadataAnalyzer().Analyze(
                new ShapeDefinition("Hero", root), metadata);

            Assert.That(report.IsValid, Is.True);
            Assert.That(report.AnchorCount, Is.EqualTo(1));
            Assert.That(report.ColliderCount, Is.EqualTo(1));
        }

        [Test]
        public void AnalyzerReportsEveryUnknownStableNode()
        {
            ShapeGameMetadata metadata = Metadata();
            metadata.Anchors.Add(new()
            {
                Id = "grip", Role = ShapeSemanticAnchorRoles.HandGrip, NodeId = "missing-hand"
            });
            metadata.Colliders.Add(new() { Id = "hitbox", NodeId = "missing-body" });

            ShapeGameMetadataReport report = new ShapeGameMetadataAnalyzer().Analyze(
                new ShapeDefinition("Hero", new ShapeNode("root", "Root", ShapeTypes.Group)), metadata);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics.Diagnostics.Count, Is.EqualTo(2));
            Assert.That(report.Diagnostics.Diagnostics[0].Code, Is.EqualTo("shape.game.node.unknown"));
        }

        [Test]
        public void ValidatorRejectsInvalidColliderAndLodOrdering()
        {
            ShapeGameMetadata metadata = Metadata();
            metadata.Colliders.Add(new()
            {
                Id = "invalid", NodeId = "root", Radius = 0f
            });
            metadata.Lods.Add(new() { Level = 1, ScreenRelativeHeight = 0.5f });
            metadata.Lods.Add(new() { Level = 2, ScreenRelativeHeight = 0.8f });

            ShapeDiagnosticReport report = new ShapeGameMetadataValidator().Analyze(metadata);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics.Count, Is.EqualTo(4));
            Assert.That(report.Diagnostics[0].Code, Is.EqualTo("shape.game.collider.dimensions.invalid"));
        }

        private static ShapeGameMetadata Metadata() => new() { Id = "hero/gameplay" };
    }
}
