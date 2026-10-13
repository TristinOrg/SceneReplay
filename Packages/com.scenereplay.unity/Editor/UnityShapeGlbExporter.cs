using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Exports generated Unity hierarchies to self-contained glTF 2.0 binary assets.
    /// </summary>
    public static class UnityShapeGlbExporter
    {
        private const uint Magic     = 0x46546C67;
        private const uint JsonChunk = 0x4E4F534A;
        private const uint BinChunk  = 0x004E4942;

        /// <summary>Exports hierarchy, local transforms, geometry, UVs, normals, colors, and stable node IDs.</summary>
        public static UnityShapeGlbExportResult Export(GameObject root, string path)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A GLB output path is required.", nameof(path));
            if (!root.TryGetComponent(out UnityShapeModel model))
                throw new ShapeValidationException("GLB export requires a generated UnityShapeModel root.");

            ExportContext context = new(model);
            int rootNode = context.AddNode(root.transform);
            JObject document = context.CreateDocument(rootNode);
            byte[] json = Pad(Encoding.UTF8.GetBytes(document.ToString(Formatting.None)), 0x20);
            byte[] binary = Pad(context.GetBinary(), 0x00);
            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            using (BinaryWriter writer = new(File.Create(fullPath)))
            {
                writer.Write(Magic);
                writer.Write(2u);
                writer.Write((uint)(12 + 8 + json.Length + 8 + binary.Length));
                writer.Write((uint)json.Length);
                writer.Write(JsonChunk);
                writer.Write(json);
                writer.Write((uint)binary.Length);
                writer.Write(BinChunk);
                writer.Write(binary);
            }
            return new(fullPath, context.NodeCount, context.MeshCount, context.VertexCount, context.TriangleCount);
        }

        private static byte[] Pad(byte[] source, byte padding)
        {
            int length = (source.Length + 3) & ~3;
            if (length == source.Length)
                return source;
            byte[] result = new byte[length];
            Buffer.BlockCopy(source, 0, result, 0, source.Length);
            for (int index = source.Length; index < result.Length; index++)
                result[index] = padding;
            return result;
        }

        /// <summary>Owns one isolated export and all glTF table indices.</summary>
        private sealed class ExportContext
        {
            private readonly UnityShapeModel model;
            private readonly MemoryStream    binary      = new();
            private readonly JArray          accessors   = new();
            private readonly JArray          bufferViews = new();
            private readonly JArray          materials   = new();
            private readonly JArray          meshes      = new();
            private readonly JArray          nodes       = new();
            private readonly Dictionary<Material, int> materialIndices = new();

            public ExportContext(UnityShapeModel model)
            {
                this.model = model;
            }

            public int NodeCount => nodes.Count;
            public int MeshCount => meshes.Count;
            public int VertexCount { get; private set; }
            public int TriangleCount { get; private set; }

            public int AddNode(Transform transform)
            {
                JObject node = new()
                {
                    ["name"]   = transform.name,
                    ["matrix"] = Matrix(transform.localToWorldMatrix, transform.parent?.localToWorldMatrix)
                };
                if (model.TryGetNodeId(transform, out string nodeId))
                {
                    node["extras"] = new JObject { ["scenereplayNodeId"] = nodeId };
                    node["extensions"] = new JObject
                    {
                        ["SCENEREPLAY_node"] = new JObject { ["id"] = nodeId }
                    };
                }
                int index = nodes.Count;
                nodes.Add(node);
                MeshFilter filter = transform.GetComponent<MeshFilter>();
                Renderer renderer = transform.GetComponent<Renderer>();
                if (filter != null && renderer != null && filter.sharedMesh != null)
                    node["mesh"] = AddMesh(filter.sharedMesh, renderer.sharedMaterials);
                JArray children = new();
                foreach (Transform child in transform)
                    children.Add(AddNode(child));
                if (children.Count > 0)
                    node["children"] = children;
                return index;
            }

            public JObject CreateDocument(int rootNode) => new()
            {
                ["asset"] = new JObject { ["version"] = "2.0", ["generator"] = "SceneReplay" },
                ["extensionsUsed"] = new JArray("SCENEREPLAY_node"),
                ["scene"] = 0,
                ["scenes"] = new JArray(new JObject { ["nodes"] = new JArray(rootNode) }),
                ["nodes"] = nodes,
                ["meshes"] = meshes,
                ["materials"] = materials,
                ["accessors"] = accessors,
                ["bufferViews"] = bufferViews,
                ["buffers"] = new JArray(new JObject { ["byteLength"] = binary.Length })
            };

            public byte[] GetBinary() => binary.ToArray();

            private int AddMesh(Mesh mesh, Material[] sourceMaterials)
            {
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals  = mesh.normals;
                Vector2[] uv       = mesh.uv;
                int position = AddVectors(vertices, true, out Vector3 minimum, out Vector3 maximum);
                int normal   = normals.Length == vertices.Length ? AddVectors(normals, true, out _, out _) : -1;
                int texcoord = uv.Length == vertices.Length ? AddVectors(uv) : -1;
                accessors[position]["min"] = Vector(minimum);
                accessors[position]["max"] = Vector(maximum);
                JArray primitives = new();
                int subMeshCount = Math.Max(mesh.subMeshCount, 1);
                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    int[] source = mesh.GetTriangles(subMesh);
                    int[] indices = new int[source.Length];
                    for (int index = 0; index < source.Length; index += 3)
                    {
                        indices[index]     = source[index];
                        indices[index + 1] = source[index + 2];
                        indices[index + 2] = source[index + 1];
                    }
                    JObject attributes = new() { ["POSITION"] = position };
                    if (normal >= 0)
                        attributes["NORMAL"] = normal;
                    if (texcoord >= 0)
                        attributes["TEXCOORD_0"] = texcoord;
                    JObject primitive = new()
                    {
                        ["attributes"] = attributes,
                        ["indices"]    = AddIndices(indices),
                        ["mode"]       = 4
                    };
                    if (sourceMaterials.Length > 0)
                        primitive["material"] = AddMaterial(sourceMaterials[Math.Min(subMesh, sourceMaterials.Length - 1)]);
                    primitives.Add(primitive);
                    TriangleCount += indices.Length / 3;
                }
                meshes.Add(new JObject { ["name"] = mesh.name, ["primitives"] = primitives });
                VertexCount += vertices.Length;
                return meshes.Count - 1;
            }

            private int AddMaterial(Material material)
            {
                if (material != null && materialIndices.TryGetValue(material, out int existing))
                    return existing;
                Color color = Color.white;
                if (material != null)
                {
                    if (material.HasProperty("_BaseColor"))
                        color = material.GetColor("_BaseColor");
                    else if (material.HasProperty("_Color"))
                        color = material.color;
                }
                JObject result = new()
                {
                    ["name"] = material?.name ?? "Default",
                    ["pbrMetallicRoughness"] = new JObject
                    {
                        ["baseColorFactor"] = new JArray(color.r, color.g, color.b, color.a),
                        ["metallicFactor"]  = 0f,
                        ["roughnessFactor"] = 0.8f
                    },
                    ["doubleSided"] = true
                };
                if (color.a < 0.999f)
                    result["alphaMode"] = "BLEND";
                materials.Add(result);
                int index = materials.Count - 1;
                if (material != null)
                    materialIndices.Add(material, index);
                return index;
            }

            private int AddVectors(Vector3[] values, bool flipZ, out Vector3 minimum, out Vector3 maximum)
            {
                Align(4);
                int offset = (int)binary.Position;
                minimum = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                maximum = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                using BinaryWriter writer = new(binary, Encoding.UTF8, true);
                foreach (Vector3 source in values)
                {
                    Vector3 value = flipZ ? new(source.x, source.y, -source.z) : source;
                    writer.Write(value.x);
                    writer.Write(value.y);
                    writer.Write(value.z);
                    minimum = Vector3.Min(minimum, value);
                    maximum = Vector3.Max(maximum, value);
                }
                return AddAccessor(offset, values.Length * 12, 5126, values.Length, "VEC3", 34962);
            }

            private int AddVectors(Vector2[] values)
            {
                Align(4);
                int offset = (int)binary.Position;
                using BinaryWriter writer = new(binary, Encoding.UTF8, true);
                foreach (Vector2 value in values)
                {
                    writer.Write(value.x);
                    writer.Write(1f - value.y);
                }
                return AddAccessor(offset, values.Length * 8, 5126, values.Length, "VEC2", 34962);
            }

            private int AddIndices(int[] values)
            {
                Align(4);
                int offset = (int)binary.Position;
                using BinaryWriter writer = new(binary, Encoding.UTF8, true);
                foreach (int value in values)
                    writer.Write((uint)value);
                return AddAccessor(offset, values.Length * 4, 5125, values.Length, "SCALAR", 34963);
            }

            private int AddAccessor(int offset, int length, int componentType, int count, string type, int target)
            {
                bufferViews.Add(new JObject
                {
                    ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = length, ["target"] = target
                });
                accessors.Add(new JObject
                {
                    ["bufferView"] = bufferViews.Count - 1,
                    ["componentType"] = componentType,
                    ["count"] = count,
                    ["type"] = type
                });
                return accessors.Count - 1;
            }

            private void Align(int alignment)
            {
                while (binary.Position % alignment != 0)
                    binary.WriteByte(0);
            }

            private static JArray Matrix(Matrix4x4 world, Matrix4x4? parentWorld)
            {
                Matrix4x4 local = parentWorld.HasValue ? parentWorld.Value.inverse * world : world;
                Matrix4x4 handedness = Matrix4x4.Scale(new(1f, 1f, -1f));
                Matrix4x4 converted = handedness * local * handedness;
                JArray result = new();
                for (int column = 0; column < 4; column++)
                for (int row = 0; row < 4; row++)
                    result.Add(converted[row, column]);
                return result;
            }

            private static JArray Vector(Vector3 value) => new(value.x, value.y, value.z);
        }
    }
}
