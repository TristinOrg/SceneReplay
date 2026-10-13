namespace SceneReplay.Unity.Editor
{
    /// <summary>
    /// Reports one geometry-preserving binary glTF export.
    /// </summary>
    public sealed class UnityShapeGlbExportResult
    {
        /// <summary>Initializes immutable export statistics.</summary>
        public UnityShapeGlbExportResult(string path, int nodeCount, int meshCount, int vertexCount, int triangleCount)
        {
            Path          = path;
            NodeCount     = nodeCount;
            MeshCount     = meshCount;
            VertexCount   = vertexCount;
            TriangleCount = triangleCount;
        }

        /// <summary>Gets the absolute GLB path.</summary>
        public string Path { get; }
        /// <summary>Gets exported hierarchy-node count.</summary>
        public int NodeCount { get; }
        /// <summary>Gets exported mesh-primitive count.</summary>
        public int MeshCount { get; }
        /// <summary>Gets exported vertex count.</summary>
        public int VertexCount { get; }
        /// <summary>Gets exported triangle count.</summary>
        public int TriangleCount { get; }
    }
}
