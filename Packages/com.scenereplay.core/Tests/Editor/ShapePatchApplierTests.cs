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

        [Test]
        public void ApplyWithInverseRoundTripsStructuralAndAuthoredEdits()
        {
            ShapeNode root = new("root", "Root", ShapeTypes.Group);
            ShapeNode sourceParent = new("source", "Source", ShapeTypes.Group);
            ShapeNode destinationParent = new("destination", "Destination", ShapeTypes.Group);
            ShapeNode part = new("part", "Part", "example/cube");
            part.Transform.Position = new(1f, 2f, 3f);
            part.Appearance = new ShapeAppearance { ColorRole = "primary", HasColorOverride = true };
            part.Parameters["radius"] = 0.75f;
            part.Profile.Add(new ForgeVector2(0.25f, 0.5f));
            sourceParent.Add(part);
            sourceParent.Add(new("removed", "Removed", ShapeTypes.Group));
            root.Add(sourceParent).Add(destinationParent);
            ShapeDefinition source = new("Example", root) { Style = "example/style" };
            source.Rig = new ShapeRigDefinition { Type = "example/rig" };
            source.Rig.Joints.Add(new ShapeRigJoint("root", "root"));

            ShapePatchDocument patch = new();
            patch.Operations.Add(new()
            {
                Kind = ShapePatchOperationKind.UpdateNode,
                NodeId = "part",
                Update = new ShapeNodeUpdate
                {
                    Name = "Edited Part",
                    Transform = new ShapeTransform { Position = new(9f, 8f, 7f) },
                    Appearance = new ShapeAppearance { ColorRole = "secondary" },
                    Parameters = new System.Collections.Generic.Dictionary<string, float> { ["radius"] = 1.5f },
                    Profile = new System.Collections.Generic.List<ForgeVector2> { new(1f, 1f) }
                }
            });
            patch.Operations.Add(new()
            {
                Kind = ShapePatchOperationKind.MoveNode,
                NodeId = "part",
                ParentId = "destination"
            });
            patch.Operations.Add(new() { Kind = ShapePatchOperationKind.RemoveNode, NodeId = "removed" });
            patch.Operations.Add(new()
            {
                Kind = ShapePatchOperationKind.AddNode,
                ParentId = "source",
                Node = new ShapeNode("added", "Added", ShapeTypes.Group)
            });

            ShapePatchApplier applier = new();
            ShapePatchTransaction transaction = applier.ApplyWithInverse(source, patch);
            ShapeDefinition restored = applier.Apply(transaction.Definition, transaction.InversePatch);

            Assert.That(restored.Name, Is.EqualTo(source.Name));
            Assert.That(restored.Style, Is.EqualTo(source.Style));
            Assert.That(restored.Rig.Type, Is.EqualTo(source.Rig.Type));
            Assert.That(restored.Rig.Joints[0].Role, Is.EqualTo(source.Rig.Joints[0].Role));
            Assert.That(restored.Root.Children[0].Children[0].Id, Is.EqualTo("part"));
            Assert.That(restored.Root.Children[0].Children[0].Name, Is.EqualTo("Part"));
            Assert.That(restored.Root.Children[0].Children[0].Transform.Position, Is.EqualTo(new ForgeVector3(1f, 2f, 3f)));
            Assert.That(restored.Root.Children[0].Children[0].Appearance.ColorRole, Is.EqualTo("primary"));
            Assert.That(restored.Root.Children[0].Children[0].Parameters["radius"], Is.EqualTo(0.75f));
            Assert.That(restored.Root.Children[0].Children[0].Profile[0], Is.EqualTo(new ForgeVector2(0.25f, 0.5f)));
            Assert.That(restored.Root.Children[0].Children[1].Id, Is.EqualTo("removed"));
            Assert.That(restored.Root.Children[1].Children, Is.Empty);
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
