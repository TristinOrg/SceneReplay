using NUnit.Framework;

namespace SceneReplay.Tests
{
    /// <summary>
    /// Verifies atomic engine-neutral ShapePatch execution.
    /// </summary>
    public sealed class ShapePatchApplierTests
    {
        [Test]
        public void ApplyExecutesOrderedOperationsWithoutMutatingSource()
        {
            ShapeNode root       = new("root", "Root", ShapeTypes.Group);
            ShapeNode containerA = new("container-a", "A", ShapeTypes.Group);
            ShapeNode containerB = new("container-b", "B", ShapeTypes.Group);
            ShapeNode part       = new("part", "Part", "example/cube");
            containerA.Add(part);
            root.Add(containerA).Add(containerB).Add(new("removed", "Removed", ShapeTypes.Group));
            ShapeDefinition source = new("Example", root);

            ShapePatchDocument patch = new();
            patch.Operations.Add(new()
            {
                Kind   = ShapePatchOperationKind.UpdateNode,
                NodeId = "part",
                Update = new ShapeNodeUpdate
                {
                    Name      = "Updated Part",
                    Transform = new ShapeTransform { Position = new ForgeVector3(1f, 2f, 3f) }
                }
            });
            patch.Operations.Add(new()
            {
                Kind         = ShapePatchOperationKind.MoveNode,
                NodeId       = "part",
                ParentId     = "container-b",
                SiblingIndex = 0
            });
            patch.Operations.Add(new() { Kind = ShapePatchOperationKind.RemoveNode, NodeId = "removed" });
            patch.Operations.Add(new()
            {
                Kind     = ShapePatchOperationKind.AddNode,
                ParentId = "container-a",
                Node     = new ShapeNode("added", "Added", ShapeTypes.Group)
            });

            ShapeDefinition result = new ShapePatchApplier().Apply(source, patch);

            Assert.That(Find(result.Root, "part").Name, Is.EqualTo("Updated Part"));
            Assert.That(Find(result.Root, "part").Transform.Position.X, Is.EqualTo(1f));
            Assert.That(Find(result.Root, "container-b").Children[0].Id, Is.EqualTo("part"));
            Assert.That(Find(result.Root, "added"), Is.Not.Null);
            Assert.That(Find(result.Root, "removed"), Is.Null);
            Assert.That(Find(source.Root, "part").Name, Is.EqualTo("Part"));
            Assert.That(Find(source.Root, "container-a").Children[0].Id, Is.EqualTo("part"));
            Assert.That(Find(source.Root, "removed"), Is.Not.Null);
        }

        [Test]
        public void TryApplyReturnsDiagnosticAndLeavesSourceUnchangedWhenFinalValidationFails()
        {
            ShapeDefinition source = new("Example", new ShapeNode("root", "Root", ShapeTypes.Group));
            ShapePatchDocument patch = new();
            patch.Operations.Add(new()
            {
                Kind   = ShapePatchOperationKind.UpdateNode,
                NodeId = "root",
                Update = new ShapeNodeUpdate { Type = string.Empty }
            });

            ShapePatchResult result = new ShapePatchApplier().TryApply(source, patch);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Definition, Is.Null);
            Assert.That(result.Diagnostics.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(source.Root.Type, Is.EqualTo(ShapeTypes.Group));
        }

        [Test]
        public void TryApplyRejectsMovesThatWouldCreateCycles()
        {
            ShapeNode root   = new("root", "Root", ShapeTypes.Group);
            ShapeNode parent = new("parent", "Parent", ShapeTypes.Group);
            parent.Add(new("child", "Child", ShapeTypes.Group));
            root.Add(parent);
            ShapePatchDocument patch = new();
            patch.Operations.Add(new()
            {
                Kind     = ShapePatchOperationKind.MoveNode,
                NodeId   = "parent",
                ParentId = "child"
            });

            ShapePatchResult result = new ShapePatchApplier().TryApply(new ShapeDefinition("Example", root), patch);

            Assert.That(result.Diagnostics.Diagnostics[0].Code, Is.EqualTo("shape.patch.move.cycle"));
            Assert.That(result.Diagnostics.Diagnostics[0].Path, Is.EqualTo("/operations/0"));
        }

        private static ShapeNode Find(ShapeNode root, string id)
        {
            if (root.Id == id)
                return root;

            foreach (ShapeNode child in root.Children)
            {
                ShapeNode result = Find(child, id);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
