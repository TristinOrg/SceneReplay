using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies deterministic semantic ShapeDefinition comparison.
    /// </summary>
    public sealed class ShapeDefinitionDifferTests
    {
        [Test]
        public void CompareReturnsNoDifferencesForEquivalentDefinitions()
        {
            ShapeDefinition before = CreateDefinition();
            ShapeDefinition after  = CreateDefinition();

            ShapeDiffReport report = new ShapeDefinitionDiffer().Compare(before, after);

            Assert.That(report.HasChanges, Is.False);
            Assert.That(report.Differences, Is.Empty);
        }

        [Test]
        public void CompareReportsRemovedAddedMovedAndChangedNodesInStableOrder()
        {
            ShapeNode beforeRoot      = new("root", "Root", ShapeTypes.Group);
            ShapeNode beforeContainer = new("container", "Container", ShapeTypes.Group);
            ShapeNode beforePart      = new("part", "Part", "example/cube");
            beforeRoot.Add(beforeContainer).Add(beforePart).Add(new("removed", "Removed", ShapeTypes.Group));

            ShapeNode afterRoot      = new("root", "Root", ShapeTypes.Group);
            ShapeNode afterContainer = new("container", "Container", ShapeTypes.Group);
            ShapeNode afterPart      = new("part", "Part", "example/cube");
            afterPart.Transform.Position = new(1f, 2f, 3f);
            afterContainer.Add(afterPart);
            afterRoot.Add(afterContainer).Add(new("added", "Added", ShapeTypes.Group));

            ShapeDiffReport report = new ShapeDefinitionDiffer().Compare(
                new("Before", beforeRoot),
                new("Before", afterRoot));

            Assert.That(report.Differences.Count, Is.EqualTo(4));
            Assert.That(report.Differences[0].Kind, Is.EqualTo(ShapeDifferenceKind.NodeRemoved));
            Assert.That(report.Differences[0].NodeId, Is.EqualTo("removed"));
            Assert.That(report.Differences[1].Kind, Is.EqualTo(ShapeDifferenceKind.NodeAdded));
            Assert.That(report.Differences[1].NodeId, Is.EqualTo("added"));
            Assert.That(report.Differences[2].Kind, Is.EqualTo(ShapeDifferenceKind.NodeMoved));
            Assert.That(report.Differences[2].NodeId, Is.EqualTo("part"));
            Assert.That(report.Differences[3].Path, Is.EqualTo("/nodes/part/transform/position"));
            Assert.That(report.Differences[3].BeforeValue, Is.EqualTo("[0,0,0]"));
            Assert.That(report.Differences[3].AfterValue, Is.EqualTo("[1,2,3]"));
        }

        private static ShapeDefinition CreateDefinition()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            root.Add(new("part", "Part", "example/cube"));
            return new("Example", root);
        }
    }
}
