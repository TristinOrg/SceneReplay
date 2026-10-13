using NUnit.Framework;
using UnityEngine;

namespace SceneReplay.Unity.Tests
{
    /// <summary>
    /// Verifies Unity hierarchy adaptation and validation behavior.
    /// </summary>
    public sealed class UnityShapeModelGeneratorTests
    {
        private GameObject generatedRoot;

        [TearDown]
        public void TearDown()
        {
            if (generatedRoot != null)
                Object.DestroyImmediate(generatedRoot);
        }

        [Test]
        public void GenerateBuildsEditableUnityHierarchy()
        {
            ShapeNode root  = new ShapeNode("robot", "Robot", ShapeTypes.Group);
            ShapeNode child = new ShapeNode("body", "Body", TestGenerator.Type);
            child.Transform.Scale = new ForgeVector3(2f, 3f, 4f);
            root.Add(child);

            UnityShapeModelGenerator generator = new UnityShapeModelGenerator(new IUnityShapeGenerator[]
            {
                new TestGenerator()
            });

            ShapeDefinition definition = new("Robot", root);
            generatedRoot = generator.Generate(definition);

            Assert.That(generatedRoot.name, Is.EqualTo("Robot"));
            Assert.That(generatedRoot.transform.childCount, Is.EqualTo(1));
            Assert.That(generatedRoot.transform.GetChild(0).name, Is.EqualTo("Body"));
            Assert.That(generatedRoot.transform.GetChild(0).localScale, Is.EqualTo(new Vector3(2f, 3f, 4f)));

            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            Assert.That(model.BindingCount, Is.EqualTo(2));
            Assert.That(model.TryGetTarget("body", out IShapeTransformTarget target), Is.True);
            Assert.That(model.TryGetParentNodeId("robot", out string rootParentId), Is.True);
            Assert.That(rootParentId, Is.Empty);
            Assert.That(model.TryGetParentNodeId("body", out string bodyParentId), Is.True);
            Assert.That(bodyParentId, Is.EqualTo("robot"));

            ShapeDiagnosticReport conformance = new ShapeAdapterConformanceValidator()
                .Analyze(definition, UnityShapeAdapter.Descriptor, model);
            Assert.That(conformance.IsValid, Is.True);

            target.LocalPosition = new(5f, 6f, 7f);
            Assert.That(generatedRoot.transform.GetChild(0).localPosition, Is.EqualTo(new Vector3(5f, 6f, 7f)));
            Assert.That(model.TryGetTarget("missing", out _), Is.False);
        }

        [Test]
        public void GenerateRejectsDuplicateNodeIdsBeforeCreatingObjects()
        {
            ShapeNode root = new ShapeNode("duplicate", "Root", ShapeTypes.Group)
                .Add(new ShapeNode("duplicate", "Child", TestGenerator.Type));
            UnityShapeModelGenerator generator = new UnityShapeModelGenerator(new IUnityShapeGenerator[]
            {
                new TestGenerator()
            });

            ShapeValidationException exception = Assert.Throws<ShapeValidationException>(() =>
                generator.Generate(new ShapeDefinition("Invalid", root)));

            Assert.That(exception.Message, Does.Contain("Duplicate"));
            Assert.That(GameObject.Find("Root"), Is.Null);
        }

        [Test]
        public void GenerateCreatesMirroredHierarchyWithStableBindings()
        {
            ShapeNode root  = new("character", "Character", ShapeTypes.Group);
            ShapeNode child = new("arm-left", "Left Arm", TestGenerator.Type)
            {
                MirrorAxis = ShapeMirrorAxis.X
            };
            child.Transform.Position    = new(-0.75f, 0.25f, 0.1f);
            child.Transform.EulerAngles = new(10f, 20f, 30f);
            root.Add(child);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[]
            {
                new TestGenerator()
            });

            generatedRoot = generator.Generate(new("Character", root));

            Transform original = generatedRoot.transform.Find("Left Arm");
            Transform mirrored = generatedRoot.transform.Find("Left Arm (Mirror X)");
            Assert.That(original, Is.Not.Null);
            Assert.That(mirrored, Is.Not.Null);
            Assert.That(mirrored.localPosition, Is.EqualTo(new Vector3(0.75f, 0.25f, 0.1f)));
            Assert.That(mirrored.localScale, Is.EqualTo(new Vector3(-1f, 1f, 1f)));

            UnityShapeModel model = generatedRoot.GetComponent<UnityShapeModel>();
            Assert.That(model.BindingCount, Is.EqualTo(3));
            Assert.That(model.TryGetTarget("arm-left", out _), Is.True);
            Assert.That(model.TryGetTarget("arm-left.mirror-x", out _), Is.True);
        }

        [Test]
        public void GenerateAppliesConfiguredComplexityLimitsBeforeAllocatingObjects()
        {
            ShapeNode root = new("root", "Limited Root", ShapeTypes.Group);
            root.Add(new("child", "Child", TestGenerator.Type));
            UnityShapeModelGenerator generator = new(
                new IUnityShapeGenerator[] { new TestGenerator() },
                validationLimits: new(maximumNodeCount: 1));

            Assert.Throws<ShapeValidationException>(() =>
                generator.Generate(new("Limited", root)));
            Assert.That(GameObject.Find("Limited Root"), Is.Null);
        }

        [Test]
        public void RegenerateReplacesModelAfterSuccessfulGeneration()
        {
            GameObject parent = new("Parent");
            ShapeNode originalRoot = new("original", "Original", ShapeTypes.Group);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[] { new TestGenerator() });
            generatedRoot = generator.Generate(new("Original", originalRoot), parent.transform);
            UnityShapeModel existingModel = generatedRoot.GetComponent<UnityShapeModel>();
            ShapeNode replacementRoot = new("replacement", "Replacement", ShapeTypes.Group);

            generatedRoot = generator.Regenerate(existingModel, new("Replacement", replacementRoot));

            Assert.That(parent.transform.childCount, Is.EqualTo(1));
            Assert.That(parent.transform.GetChild(0).name, Is.EqualTo("Replacement"));
            Assert.That(generatedRoot.GetComponent<UnityShapeModel>().TryGetTarget("replacement", out _), Is.True);
            Object.DestroyImmediate(parent);
            generatedRoot = null;
        }

        [Test]
        public void RegenerateKeepsExistingModelWhenReplacementIsInvalid()
        {
            ShapeNode originalRoot = new("original", "Original", ShapeTypes.Group);
            UnityShapeModelGenerator generator = new(new IUnityShapeGenerator[] { new TestGenerator() });
            generatedRoot = generator.Generate(new("Original", originalRoot));
            UnityShapeModel existingModel = generatedRoot.GetComponent<UnityShapeModel>();
            ShapeDefinition invalid = new("Invalid", new("duplicate", "Invalid", ShapeTypes.Group));
            invalid.Root.Add(new("duplicate", "Duplicate", ShapeTypes.Group));

            Assert.Throws<ShapeValidationException>(() => generator.Regenerate(existingModel, invalid));
            Assert.That(generatedRoot, Is.Not.Null);
            Assert.That(generatedRoot.name, Is.EqualTo("Original"));
        }

        private sealed class TestGenerator : IUnityShapeGenerator
        {
            public const string Type = "test/shape";

            public bool CanGenerate(ShapeNode node)
            {
                return node != null && node.Type == Type;
            }

            public GameObject Generate(ShapeNode node, ShapeGenerationContext context)
            {
                return new GameObject();
            }
        }
    }
}
