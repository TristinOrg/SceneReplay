using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SceneReplay.Unity.Editor;

namespace SceneReplay.Unity.Tests
{
    /// <summary>Verifies reversible gameplay-semantic edits through the native Prefab asset workflow.</summary>
    public sealed class UnityShapePrefabPatchCompilerTests
    {
        private string outputFolder;

        [SetUp]
        public void SetUp()
        {
            outputFolder = $"Assets/SceneReplayPrefabTests_{Guid.NewGuid():N}";
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(outputFolder) && AssetDatabase.IsValidFolder(outputFolder))
                AssetDatabase.DeleteAsset(outputFolder);
        }

        [Test]
        public void MetadataAndReversibleEditsSurvivePrefabSaveAndReload()
        {
            ShapeNode root = new("hero", "Hero", ShapeTypes.Group);
            ShapeNode hand = new("hand", "Hand", TestGenerator.Type);
            hand.Transform.Position = new(0.25f, 0.5f, 0.75f);
            root.Add(hand);
            ShapeDefinition source = new("Hero", root);
            ShapeGameMetadata metadata = CreateMetadata();
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[] { new TestGenerator() });
            ShapePatchDocument patch = new();
            patch.Operations.Add(new()
            {
                Kind = ShapePatchOperationKind.UpdateNode,
                NodeId = "hand",
                Update = new ShapeNodeUpdate
                {
                    Transform = new ShapeTransform { Position = new(2f, 3f, 4f) }
                }
            });
            patch.Operations.Add(new()
            {
                Kind = ShapePatchOperationKind.AddNode,
                ParentId = "hero",
                Node = new ShapeNode("attachment", "Attachment", ShapeTypes.Group)
            });

            UnityShapePrefabPatchCompilationResult edited = UnityShapePrefabPatchCompiler.Compile(
                source,
                patch,
                metadata,
                generator,
                outputFolder);

            AssertPrefabSemantics(edited.Compilation.PrefabPath, edited.Definition, metadata, new Vector3(2f, 3f, 4f), 3);

            UnityShapePrefabPatchCompilationResult restored = UnityShapePrefabPatchCompiler.Compile(
                edited.Definition,
                edited.InversePatch,
                metadata,
                generator,
                outputFolder);

            Assert.That(restored.Definition.Root.Children, Has.Count.EqualTo(1));
            Assert.That(restored.Definition.Root.Children[0].Id, Is.EqualTo("hand"));
            AssertPrefabSemantics(restored.Compilation.PrefabPath, restored.Definition, metadata, new Vector3(0.25f, 0.5f, 0.75f), 2);
        }

        private static void AssertPrefabSemantics(
            string prefabPath,
            ShapeDefinition definition,
            ShapeGameMetadata sourceMetadata,
            Vector3 expectedHandPosition,
            int expectedBindingCount)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                UnityShapeModel model = prefabRoot.GetComponent<UnityShapeModel>();
                Assert.That(model, Is.Not.Null);
                Assert.That(model.BindingCount, Is.EqualTo(expectedBindingCount));
                Assert.That(model.TryGetTarget("hand", out IShapeTransformTarget hand), Is.True);
                Assert.That(hand.LocalPosition.ToUnity(), Is.EqualTo(expectedHandPosition));

                UnityShapeAssetManifest assetManifest = prefabRoot.GetComponent<UnityShapeAssetManifest>();
                Assert.That(assetManifest, Is.Not.Null);
                Assert.That(assetManifest.DefinitionJson, Is.Not.Empty);
                Assert.That(assetManifest.GameMetadataJson, Is.Not.Empty);
                ShapeGameMetadata persistedMetadata = new ShapeJsonSerializer()
                    .DeserializeGameMetadata(assetManifest.GameMetadataJson);
                Assert.That(persistedMetadata.Id, Is.EqualTo(sourceMetadata.Id));
                Assert.That(persistedMetadata.Tags, Is.EqualTo(sourceMetadata.Tags));
                Assert.That(persistedMetadata.Colliders[0].Kind, Is.EqualTo(ShapeColliderKind.Box));
                Assert.That(persistedMetadata.Colliders[0].Size, Is.EqualTo(new ForgeVector3(1f, 2f, 3f)));

                UnityShapeGameMetadataManifest gameManifest = prefabRoot.GetComponent<UnityShapeGameMetadataManifest>();
                Assert.That(gameManifest, Is.Not.Null);
                Assert.That(gameManifest.TryGetAnchor("primary-grip", out Transform anchor), Is.True);
                Assert.That(anchor.parent.name, Is.EqualTo("Hand"));
                Assert.That(gameManifest.TryGetDamageZone("hand-hit", out Transform damageTarget, out float multiplier), Is.True);
                Assert.That(damageTarget.GetComponent<BoxCollider>(), Is.Not.Null);
                Assert.That(multiplier, Is.EqualTo(1.5f));

                ShapeDiagnosticReport conformance = new ShapeAdapterConformanceValidator().Analyze(
                    definition,
                    persistedMetadata,
                    UnityShapeAdapter.Descriptor,
                    model);
                Assert.That(conformance.IsValid, Is.True);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static ShapeGameMetadata CreateMetadata()
        {
            ShapeSemanticAnchor anchor = new()
            {
                Id = "primary-grip",
                Role = ShapeSemanticAnchorRoles.HandGrip,
                NodeId = "hand"
            };
            anchor.Transform.Position = new(0.1f, 0.2f, 0.3f);
            return new()
            {
                Id = "hero-gameplay",
                Anchors = { anchor },
                DamageZones = { new ShapeDamageZone { Id = "hand-hit", NodeId = "hand", Multiplier = 1.5f } },
                Colliders =
                {
                    new ShapeColliderRule
                    {
                        Id = "hand-box",
                        NodeId = "hand",
                        Kind = ShapeColliderKind.Box,
                        Size = new ForgeVector3(1f, 2f, 3f)
                    }
                },
                Tags = { "character", "hero" }
            };
        }

        private sealed class TestGenerator : IUnityShapeGenerator
        {
            public const string Type = "test/prefab-patch";

            public bool CanGenerate(ShapeNode node) => node?.Type == Type;

            public GameObject Generate(ShapeNode node, ShapeGenerationContext context) => new();
        }
    }
}
