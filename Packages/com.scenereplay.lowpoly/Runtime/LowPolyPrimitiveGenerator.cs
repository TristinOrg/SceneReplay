using System;
using System.Collections.Generic;
using SceneReplay.Unity;
using UnityEngine;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Generates official primitive and parameterized Low Poly shapes from cached render resources.
    /// </summary>
    public sealed class LowPolyPrimitiveGenerator : IUnityShapeGenerator
    {
        private static readonly Dictionary<string, PrimitiveType> PrimitiveTypes = new(StringComparer.Ordinal)
        {
            [LowPolyShapeTypes.Cube]     = PrimitiveType.Cube,
            [LowPolyShapeTypes.Sphere]   = PrimitiveType.Sphere,
            [LowPolyShapeTypes.Cylinder] = PrimitiveType.Cylinder,
            [LowPolyShapeTypes.Capsule]  = PrimitiveType.Capsule
        };

        /// <inheritdoc />
        public bool CanGenerate(ShapeNode node)
        {
            return node != null &&
                   (PrimitiveTypes.ContainsKey(node.Type) ||
                    node.Type == LowPolyShapeTypes.Wedge ||
                    node.Type == LowPolyShapeTypes.Frustum ||
                    node.Type == LowPolyShapeTypes.ExtrudedProfile ||
                    node.Type == LowPolyShapeTypes.ProfileLoft ||
                    node.Type == LowPolyShapeTypes.ProfileCage ||
                    node.Type == LowPolyShapeTypes.LatheProfile ||
                    node.Type == LowPolyShapeTypes.ProfileSweep);
        }

        /// <inheritdoc />
        public GameObject Generate(ShapeNode node, ShapeGenerationContext context)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            Mesh             mesh;
            Material         material;
            if (PrimitiveTypes.TryGetValue(node.Type, out PrimitiveType primitiveType))
            {
                PrimitiveTemplate template = UnityPrimitiveTemplateCache.Get(primitiveType);
                mesh     = template.Mesh;
                material = template.Material;
            }
            else
            {
                mesh     = GetProceduralMesh(node);
                material = UnityPrimitiveTemplateCache.Get(PrimitiveType.Cube).Material;
            }

            GameObject        generated    = new();
            MeshFilter        meshFilter   = generated.AddComponent<MeshFilter>();
            MeshRenderer      meshRenderer = generated.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh       = mesh;
            meshRenderer.sharedMaterial = material;
            return generated;
        }

        private static Mesh GetProceduralMesh(ShapeNode node)
        {
            if (node.Type == LowPolyShapeTypes.Wedge)
                return LowPolyProceduralMeshCache.GetWedge();

            if (node.Type == LowPolyShapeTypes.ExtrudedProfile)
            {
                float depth         = GetPositiveParameter(node, LowPolyShapeParameters.ProfileDepth, 0.2f);
                float bevel         = GetNonNegativeParameter(node, LowPolyShapeParameters.ProfileBevel, 0f);
                int   bevelSegments = GetIntegerParameter(node, LowPolyShapeParameters.ProfileBevelSegments, 1, 1, 8);
                int   smoothing     = GetIntegerParameter(node, LowPolyShapeParameters.ProfileSmoothing, 0, 0, 4);
                return LowPolyProceduralMeshCache.GetExtrudedProfile(
                    node.Profile,
                    depth,
                    bevel,
                    bevelSegments,
                    smoothing);
            }

            if (node.Type == LowPolyShapeTypes.ProfileLoft)
            {
                int  subdivisions  = GetIntegerParameter(node, LowPolyShapeParameters.LoftSubdivisions, 0, 0, 8);
                int  smoothing     = GetIntegerParameter(node, LowPolyShapeParameters.ProfileSmoothing, 0, 0, 4);
                bool smoothNormals = GetNonNegativeParameter(node, LowPolyShapeParameters.SmoothNormals, 0f) > 0f;
                return LowPolyProceduralMeshCache.GetProfileLoft(
                    node.Profile,
                    node.ProfileSections,
                    subdivisions,
                    smoothNormals,
                    smoothing);
            }

            if (node.Type == LowPolyShapeTypes.LatheProfile)
            {
                int  radialSegments = GetIntegerParameter(node, LowPolyShapeParameters.RadialSegments, 12, 3, 64);
                int  smoothing      = GetIntegerParameter(node, LowPolyShapeParameters.ProfileSmoothing, 0, 0, 4);
                bool smoothNormals  = GetNonNegativeParameter(node, LowPolyShapeParameters.SmoothNormals, 0f) > 0f;
                return LowPolyProceduralMeshCache.GetLatheProfile(
                    node.Profile,
                    radialSegments,
                    smoothNormals,
                    smoothing);
            }

            if (node.Type == LowPolyShapeTypes.ProfileCage)
            {
                int  subdivisions  = GetIntegerParameter(node, LowPolyShapeParameters.CageSubdivisions, 0, 0, 8);
                int  smoothing     = GetIntegerParameter(node, LowPolyShapeParameters.ProfileSmoothing, 0, 0, 4);
                bool smoothNormals = GetNonNegativeParameter(node, LowPolyShapeParameters.SmoothNormals, 0f) > 0f;
                return LowPolyProceduralMeshCache.GetProfileCage(
                    node.ProfileCageSections,
                    subdivisions,
                    smoothNormals,
                    smoothing);
            }

            if (node.Type == LowPolyShapeTypes.ProfileSweep)
            {
                int  profileSmoothing = GetIntegerParameter(node, LowPolyShapeParameters.ProfileSmoothing, 0, 0, 4);
                int  pathSmoothing    = GetIntegerParameter(node, LowPolyShapeParameters.PathSmoothing, 0, 0, 4);
                bool smoothNormals    = GetNonNegativeParameter(node, LowPolyShapeParameters.SmoothNormals, 0f) > 0f;
                return LowPolySweepMeshCache.Get(
                    node.Profile,
                    node.Path,
                    profileSmoothing,
                    pathSmoothing,
                    smoothNormals);
            }

            if (node.Type != LowPolyShapeTypes.Frustum)
                throw new ArgumentException($"Unsupported Low Poly shape type '{node.Type}'.", nameof(node));

            float topWidth    = GetPositiveParameter(node, LowPolyShapeParameters.TopWidth, 0.65f);
            float topDepth    = GetPositiveParameter(node, LowPolyShapeParameters.TopDepth, 0.65f);
            float bottomWidth = GetPositiveParameter(node, LowPolyShapeParameters.BottomWidth, 1f);
            float bottomDepth = GetPositiveParameter(node, LowPolyShapeParameters.BottomDepth, 1f);
            return LowPolyProceduralMeshCache.GetFrustum(topWidth, topDepth, bottomWidth, bottomDepth);
        }

        private static float GetPositiveParameter(ShapeNode node, string name, float defaultValue)
        {
            if (!node.Parameters.TryGetValue(name, out float value))
                return defaultValue;

            if (value <= 0f)
                throw new ArgumentOutOfRangeException(name, value, "Procedural dimensions must be positive.");

            return value;
        }

        private static float GetNonNegativeParameter(ShapeNode node, string name, float defaultValue)
        {
            if (!node.Parameters.TryGetValue(name, out float value))
                return defaultValue;

            if (value < 0f)
                throw new ArgumentOutOfRangeException(name, value, "Procedural dimensions cannot be negative.");

            return value;
        }

        private static int GetIntegerParameter(
            ShapeNode node,
            string    name,
            int       defaultValue,
            int       minimum,
            int       maximum)
        {
            if (!node.Parameters.TryGetValue(name, out float value))
                return defaultValue;

            int result = Mathf.RoundToInt(value);
            if (result < minimum || result > maximum || !Mathf.Approximately(value, result))
                throw new ArgumentOutOfRangeException(
                    name,
                    value,
                    $"Parameter must be an integer from {minimum} to {maximum}.");

            return result;
        }
    }
}
