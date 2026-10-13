using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies semantic detail inventory validation and definition coverage.
    /// </summary>
    public sealed class ShapeDetailInventoryTests
    {
        [Test]
        public void AnalyzerPassesImplementedRequiredDetailsAndMeasuresCoverage()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Add(new("armor/shoulder", "Shoulder Armor", "example/armor"));
            ShapeDetailInventory inventory = Inventory();
            inventory.Details.Add(new()
            {
                Id           = "shoulder-armor",
                Name         = "Shoulder Armor",
                Category     = "armor",
                TargetNodeId = "armor/shoulder",
                Required     = true
            });

            ShapeDetailCoverageReport report = new ShapeDetailCoverageAnalyzer().Analyze(
                new ShapeDefinition("Hero", root), inventory);

            Assert.That(report.Passed, Is.True);
            Assert.That(report.DetailCount, Is.EqualTo(1));
            Assert.That(report.RequiredCoverage, Is.EqualTo(1f));
        }

        [Test]
        public void AnalyzerReportsAllMissingDetailsWithRequiredSeverity()
        {
            ShapeDetailInventory inventory = Inventory();
            inventory.Details.Add(new()
            {
                Id = "weapon", Name = "Weapon", TargetNodeId = "weapon", Required = true
            });
            inventory.Details.Add(new()
            {
                Id = "cape", Name = "Cape", TargetNodeId = "cape", Required = false
            });

            ShapeDetailCoverageReport report = new ShapeDetailCoverageAnalyzer().Analyze(
                new ShapeDefinition("Hero", new ShapeNode("root", "Root", ShapeTypes.Group)), inventory);

            Assert.That(report.Passed, Is.False);
            Assert.That(report.RequiredCoverage, Is.Zero);
            Assert.That(report.Diagnostics.Diagnostics.Count, Is.EqualTo(2));
            Assert.That(report.Diagnostics.Diagnostics[0].Code, Is.EqualTo("shape.inventory.detail.missing"));
            Assert.That(report.Diagnostics.Diagnostics[1].Severity, Is.EqualTo(ShapeDiagnosticSeverity.Warning));
        }

        [Test]
        public void ValidatorRejectsDuplicateIdsUnknownParentsAndUnsafeValues()
        {
            ShapeDetailInventory inventory = Inventory();
            inventory.Details.Add(new()
            {
                Id = "belt", Name = "Belt", ParentId = "missing", RepeatCount = 0, Confidence = 2f
            });
            inventory.Details.Add(new() { Id = "belt", Name = "Duplicate" });

            ShapeDiagnosticReport report = new ShapeDetailInventoryValidator().Analyze(inventory);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics.Count, Is.EqualTo(4));
            Assert.That(report.Diagnostics[0].Code, Is.EqualTo("shape.inventory.detail.repeat.invalid"));
            Assert.That(report.Diagnostics[3].Code, Is.EqualTo("shape.inventory.detail.parent.unknown"));
        }

        private static ShapeDetailInventory Inventory() => new() { Subject = "fantasy hero" };
    }
}
