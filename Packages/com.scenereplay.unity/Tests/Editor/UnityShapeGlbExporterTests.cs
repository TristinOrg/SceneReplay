using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using SceneReplay.Unity.Editor;
using UnityEngine;

namespace SceneReplay.Unity.Tests
{
    /// <summary>
    /// Verifies self-contained glTF geometry, hierarchy, and SceneReplay identity export.
    /// </summary>
    public sealed class UnityShapeGlbExporterTests
    {
        private const string OutputPath = "Library/SceneReplayGlbTests/cube.glb";
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
            if (Directory.Exists(Path.GetDirectoryName(OutputPath)))
                Directory.Delete(Path.GetDirectoryName(OutputPath), true);
        }

        [Test]
        public void ExportWritesValidGlbWithGeometryAndStableIds()
        {
            ShapeNode definitionRoot = new("root", "Root", ShapeTypes.Group);
            definitionRoot.Add(new("cube", "Cube", TestGenerator.Type));
            ShapeDefinition definition = new("Cube", definitionRoot);
            root = new UnityShapeModelGenerator(new IUnityShapeGenerator[] { new TestGenerator() }).Generate(definition);
            Assert.That(root.GetComponent<MeshFilter>(), Is.Null);
            Assert.That(root.transform.GetChild(0).GetComponent<MeshFilter>(), Is.Not.Null);

            UnityShapeGlbExportResult result = UnityShapeGlbExporter.Export(root, OutputPath);
            string document = ReadDocument(OutputPath);

            Assert.That(result.VertexCount, Is.EqualTo(24));
            Assert.That(result.TriangleCount, Is.EqualTo(12));
            Assert.That(document, Does.Contain("\"version\":\"2.0\""));
            Assert.That(document, Does.Contain("\"meshes\":[{"));
            Assert.That(document, Does.Contain("\"byteLength\":"));
            Assert.That(document, Does.Contain("SCENEREPLAY_node"));
            Assert.That(document, Does.Contain("\"id\":\"cube\""));
        }

        private static string ReadDocument(string path)
        {
            using BinaryReader reader = new(File.OpenRead(path));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(0x46546C67));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(2));
            reader.ReadUInt32();
            int jsonLength = reader.ReadInt32();
            Assert.That(reader.ReadUInt32(), Is.EqualTo(0x4E4F534A));
            return Encoding.UTF8.GetString(reader.ReadBytes(jsonLength)).TrimEnd(' ');
        }

        private sealed class TestGenerator : IUnityShapeGenerator
        {
            public const string Type = "test/cube";
            public bool CanGenerate(ShapeNode node) => node?.Type == Type;
            public GameObject Generate(ShapeNode node, ShapeGenerationContext context) =>
                GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
    }
}
