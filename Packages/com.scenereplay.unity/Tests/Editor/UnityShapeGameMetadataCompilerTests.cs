using NUnit.Framework;
using UnityEngine;

namespace SceneReplay.Unity.Tests
{
    /// <summary>
    /// Verifies compilation of portable gameplay semantics into Unity-native model data.
    /// </summary>
    public sealed class UnityShapeGameMetadataCompilerTests
    {
        private GameObject generatedRoot;

        [TearDown]
        public void TearDown()
        {
            if (generatedRoot != null)
                Object.DestroyImmediate(generatedRoot);
        }

        [Test]
        public void CompileCreatesQueryableAnchorsZonesAndColliders()
        {
            ShapeNode root = new("hero", "Hero", ShapeTypes.Group);
            root.Add(new("hand", "Hand", TestGenerator.Type));
            ShapeDefinition definition = new("Hero", root);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[] { new TestGenerator() });
            generatedRoot = generator.Generate(definition);
            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            ShapeGameMetadata metadata = CreateMetadata();

            UnityShapeGameMetadataManifest manifest =
                new UnityShapeGameMetadataCompiler().Compile(model, definition, metadata);

            Assert.That(manifest.MetadataId, Is.EqualTo("hero-gameplay"));
            Assert.That(manifest.TryGetAnchor("weapon-grip", out Transform anchor), Is.True);
            Assert.That(anchor.parent.name, Is.EqualTo("Hand"));
            Assert.That(anchor.localPosition, Is.EqualTo(new Vector3(0.1f, 0.2f, 0.3f)));
            Assert.That(manifest.TryGetAnchorRole("weapon-grip", out string role), Is.True);
            Assert.That(role, Is.EqualTo(ShapeSemanticAnchorRoles.HandGrip));
            Assert.That(manifest.TryGetDamageZone("hand-hit", out Transform zone, out float multiplier), Is.True);
            Assert.That(zone.name, Is.EqualTo("Hand"));
            Assert.That(multiplier, Is.EqualTo(1.5f));
            Assert.That(zone.GetComponent<BoxCollider>(), Is.Not.Null);
        }

        [Test]
        public void CompileRejectsDuplicateCompilationWithoutChangingModel()
        {
            ShapeNode root = new("hero", "Hero", ShapeTypes.Group);
            root.Add(new("hand", "Hand", TestGenerator.Type));
            ShapeDefinition definition = new("Hero", root);
            generatedRoot = new UnityShapeModelGenerator(new IUnityShapeGenerator[] { new TestGenerator() })
                .Generate(definition);
            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            UnityShapeGameMetadataCompiler compiler = new();
            compiler.Compile(model, definition, CreateMetadata());

            Assert.Throws<System.InvalidOperationException>(() =>
                compiler.Compile(model, definition, CreateMetadata()));
            Assert.That(generatedRoot.GetComponents<UnityShapeGameMetadataManifest>(), Has.Length.EqualTo(1));
        }

        private static ShapeGameMetadata CreateMetadata()
        {
            ShapeSemanticAnchor anchor = new()
            {
                Id     = "weapon-grip",
                Role   = ShapeSemanticAnchorRoles.HandGrip,
                NodeId = "hand"
            };
            anchor.Transform.Position = new(0.1f, 0.2f, 0.3f);
            return new()
            {
                Id = "hero-gameplay",
                Anchors = { anchor },
                DamageZones =
                {
                    new() { Id = "hand-hit", NodeId = "hand", Multiplier = 1.5f }
                },
                Colliders =
                {
                    new() { Id = "hand-box", NodeId = "hand", Kind = ShapeColliderKind.Box }
                },
                Tags = { "character" }
            };
        }

        private sealed class TestGenerator : IUnityShapeGenerator
        {
            public const string Type = "test/gameplay";

            public bool CanGenerate(ShapeNode node) => node?.Type == Type;

            public GameObject Generate(ShapeNode node, ShapeGenerationContext context) => new();
        }
    }
}
