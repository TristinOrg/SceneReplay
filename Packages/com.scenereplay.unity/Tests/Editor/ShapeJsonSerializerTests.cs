using NUnit.Framework;

namespace SceneReplay.Unity.Tests
{
    /// <summary>
    /// Verifies the default JSON contract for cross-engine SceneReplay data.
    /// </summary>
    public sealed class ShapeJsonSerializerTests
    {
        [Test]
        public void ShapeRoundTripPreservesReadableVersionedData()
        {
            ShapeNode root  = new ShapeNode("robot", "Robot", ShapeTypes.Group);
            ShapeNode child = new ShapeNode("body", "Body", "example/cube");
            child.Transform.Position          = new ForgeVector3(1f, 2f, 3f);
            child.MirrorAxis                  = ShapeMirrorAxis.X;
            child.Appearance.ColorRole        = "primary";
            child.Appearance.HasColorOverride = true;
            child.Appearance.Color            = new ForgeColor(1f, 0f, 0f);
            child.Parameters["topWidth"]      = 0.6f;
            child.Profile.Add(new(-0.5f, 0.5f));
            child.Profile.Add(new(0.5f, -0.5f));
            child.Path.Add(new(0f, 0f, 0f));
            child.Path.Add(new(0f, 1f, 0.5f));
            child.ProfileSections.Add(new(-0.5f, new(0.8f, 0.9f), ForgeVector2.Zero));
            child.ProfileSections.Add(new(0.5f, ForgeVector2.One, new(0f, 0.1f)));
            child.ProfileCageSections.Add(new(-0.5f, new ForgeVector2[]
            {
                new(-0.4f, -0.5f), new(0.4f, -0.5f), new(0.5f, 0.5f), new(-0.5f, 0.5f)
            }));
            child.ProfileCageSections.Add(new(0.5f, new ForgeVector2[]
            {
                new(-0.3f, -0.4f), new(0.5f, -0.5f), new(0.35f, 0.6f), new(-0.45f, 0.4f)
            }));
            root.Add(child);

            ShapeDefinition source = new ShapeDefinition("Robot", root)
            {
                Style = "example/style",
                Rig = new ShapeRigDefinition
                {
                    Type   = "humanoid/basic",
                    Joints = new ShapeRigJoint[]
                    {
                        new ShapeRigJoint(ShapeRigRoles.Root, "robot"),
                        new ShapeRigJoint(ShapeRigRoles.Head, "body", new ShapeRigRotationConstraint(
                            new(-20f, -45f, -15f),
                            new(30f, 45f, 15f)))
                    }
                }
            };
            ShapeJsonSerializer serializer = new ShapeJsonSerializer();

            string          json   = serializer.Serialize(source);
            ShapeDefinition result = serializer.DeserializeShape(json);

            Assert.That(json, Does.Contain("\"schema\":\"scenereplay.shape/1.0\""));
            Assert.That(json, Does.Contain("\"position\":{\"x\":1.0"));
            Assert.That(json, Does.Contain("\"parameters\":{\"topWidth\":0.6"));
            Assert.That(json, Does.Contain("\"profile\":[{\"x\":-0.5,\"y\":0.5}"));
            Assert.That(json, Does.Contain("\"path\":[{\"x\":0.0,\"y\":0.0,\"z\":0.0}"));
            Assert.That(json, Does.Contain("\"profileSections\":[{\"z\":-0.5"));
            Assert.That(json, Does.Contain("\"profileCageSections\":[{\"z\":-0.5"));
            Assert.That(json, Does.Contain("\"mirrorAxis\":\"x\""));
            Assert.That(json, Does.Contain("\"rig\":{\"type\":\"humanoid/basic\""));
            Assert.That(result.Root.Children, Has.Count.EqualTo(1));
            Assert.That(result.Root.Children[0].Transform.Position, Is.EqualTo(new ForgeVector3(1f, 2f, 3f)));
            Assert.That(result.Root.Children[0].Appearance.Color, Is.EqualTo(new ForgeColor(1f, 0f, 0f)));
            Assert.That(result.Root.Children[0].Parameters["topWidth"], Is.EqualTo(0.6f));
            Assert.That(result.Root.Children[0].Profile[1], Is.EqualTo(new ForgeVector2(0.5f, -0.5f)));
            Assert.That(result.Root.Children[0].Path[1], Is.EqualTo(new ForgeVector3(0f, 1f, 0.5f)));
            Assert.That(result.Root.Children[0].ProfileSections[1].Offset, Is.EqualTo(new ForgeVector2(0f, 0.1f)));
            Assert.That(result.Root.Children[0].ProfileCageSections[1].Profile[2],
                Is.EqualTo(new ForgeVector2(0.35f, 0.6f)));
            Assert.That(result.Root.Children[0].MirrorAxis, Is.EqualTo(ShapeMirrorAxis.X));
            Assert.That(result.Rig.Joints[1].NodeId, Is.EqualTo("body"));
            Assert.That(result.Rig.Joints[1].RotationConstraint.Maximum,
                Is.EqualTo(new ForgeVector3(30f, 45f, 15f)));
        }

        [Test]
        public void StyleRoundTripPreservesPalette()
        {
            ShapeStyleDefinition source = new ShapeStyleDefinition("lowpoly/default")
            {
                BaseStyle = "lowpoly/base"
            };
            source.Palette.Set("primary", new ForgeColor(0f, 0f, 1f));

            ShapeJsonSerializer serializer = new ShapeJsonSerializer();
            string               json       = serializer.Serialize(source);
            ShapeStyleDefinition result     = serializer.DeserializeStyle(json);

            Assert.That(json, Does.Contain("\"schema\":\"scenereplay.style/1.0\""));
            Assert.That(json, Does.Contain("\"baseStyle\":\"lowpoly/base\""));
            Assert.That(result.BaseStyle, Is.EqualTo("lowpoly/base"));
            Assert.That(result.Palette.TryGetColor("primary", out ForgeColor color), Is.True);
            Assert.That(color, Is.EqualTo(new ForgeColor(0f, 0f, 1f)));
        }

        [Test]
        public void PatchRoundTripPreservesReadableOperations()
        {
            ShapePatchDocument source = new();
            source.Operations.Add(new()
            {
                Kind   = ShapePatchOperationKind.UpdateNode,
                NodeId = "body",
                Update = new ShapeNodeUpdate
                {
                    Name       = "Updated Body",
                    MirrorAxis = ShapeMirrorAxis.X,
                    Parameters = new() { ["width"] = 0.8f }
                }
            });
            ShapeJsonSerializer serializer = new();

            string             json   = serializer.Serialize(source);
            ShapePatchDocument result = serializer.DeserializePatch(json);

            Assert.That(json, Does.Contain("\"schema\":\"scenereplay.patch/1.0\""));
            Assert.That(json, Does.Contain("\"kind\":\"updateNode\""));
            Assert.That(json, Does.Contain("\"parameters\":{\"width\":0.8"));
            Assert.That(result.Operations.Count, Is.EqualTo(1));
            Assert.That(result.Operations[0].Update.MirrorAxis, Is.EqualTo(ShapeMirrorAxis.X));
        }

        [Test]
        public void QualityPolicyRoundTripPreservesSemanticRequirements()
        {
            ShapeQualityPolicy source = new()
            {
                Id                    = "hero/runtime",
                RequiredRigType       = "humanoid/basic",
                MaximumNodeCount      = 128,
                MaximumHierarchyDepth = 16
            };
            source.RequiredNodeIds.Add("weapon/socket");
            source.RequiredShapeTypes.Add("lowpoly/grip");
            source.RequiredRigRoles.Add(ShapeRigRoles.RightHand);
            ShapeJsonSerializer serializer = new();

            string             json   = serializer.Serialize(source);
            ShapeQualityPolicy result = serializer.DeserializeQualityPolicy(json);

            Assert.That(json, Does.Contain("\"schema\":\"scenereplay.quality/1.0\""));
            Assert.That(json, Does.Contain("\"requiredNodeIds\":[\"weapon/socket\"]"));
            Assert.That(json, Does.Contain("\"requiredRigRoles\":[\"humanoid/right-hand\"]"));
            Assert.That(result.Id, Is.EqualTo("hero/runtime"));
            Assert.That(result.MaximumHierarchyDepth, Is.EqualTo(16));
        }

        [Test]
        public void DetailInventoryRoundTripPreservesConstructionMetadata()
        {
            ShapeDetailInventory source = new() { Subject = "fantasy hero" };
            source.Details.Add(new()
            {
                Id = "weapon", Name = "Weapon", Category = "accessory",
                TargetNodeId = "weapon/socket", Confidence = 0.8f
            });
            source.Details[0].Tags.Add("accessory-pass");
            ShapeJsonSerializer serializer = new();

            string json = serializer.SerializeSpecification(source);
            ShapeDetailInventory result = serializer.DeserializeDetailInventory(json);

            Assert.That(json, Does.Contain("\"schema\":\"scenereplay.detail-inventory/1.0\""));
            Assert.That(result.Details[0].TargetNodeId, Is.EqualTo("weapon/socket"));
            Assert.That(result.Details[0].Tags[0], Is.EqualTo("accessory-pass"));
        }

        [Test]
        public void ConstructionPlanRoundTripPreservesDependenciesAndPatch()
        {
            ShapeConstructionPlan source = new() { Id = "hero/build" };
            source.Passes.Add(new()
            {
                Id = "structure", Name = "Structure", Kind = ShapeConstructionPassKind.Structure
            });
            ShapeConstructionPass details = new()
            {
                Id = "details", Name = "Details", Kind = ShapeConstructionPassKind.Details
            };
            details.DependsOn.Add("structure");
            source.Passes.Add(details);
            ShapeJsonSerializer serializer = new();

            string json = serializer.SerializeSpecification(source);
            ShapeConstructionPlan result = serializer.DeserializeConstructionPlan(json);

            Assert.That(json, Does.Contain("\"schema\":\"scenereplay.construction-plan/1.0\""));
            Assert.That(json, Does.Contain("\"kind\":\"structure\""));
            Assert.That(result.Passes[1].DependsOn[0], Is.EqualTo("structure"));
            Assert.That(result.Passes[0].Patch.Schema, Is.EqualTo(ShapePatchDocument.CurrentSchema));
        }

        [Test]
        public void TemplateCatalogSerializesReadableDiscoveryData()
        {
            ShapeTemplateDescriptor descriptor = new(
                "example/character/1.0",
                "Builds a semantic character.",
                "character",
                "example.character/1.0",
                new[] { "example/body" },
                "human",
                "stylized");
            ShapeTemplateCatalogDocument document = new(
                "example/templates",
                new[] { descriptor });
            ShapeJsonSerializer serializer = new();

            string json = serializer.Serialize(document);

            Assert.That(json, Does.Contain("\"schema\":\"scenereplay.templates/1.0\""));
            Assert.That(json, Does.Contain("\"specificationSchema\":\"example.character/1.0\""));
            Assert.That(json, Does.Contain("\"requiredShapeTypes\":[\"example/body\"]"));
            Assert.That(json, Does.Contain("\"tags\":[\"human\",\"stylized\"]"));
        }

    }
}
