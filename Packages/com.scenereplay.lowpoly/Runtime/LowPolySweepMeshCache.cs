using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Creates and shares immutable profile-sweep meshes for equivalent path configurations.
    /// </summary>
    internal static class LowPolySweepMeshCache
    {
        private static readonly Dictionary<int, List<SweepMeshEntry>> Meshes = new();

        public static Mesh Get(
            IList<ForgeVector2> profile,
            IList<ForgeVector3> path,
            int                  profileSmoothing,
            int                  pathSmoothing,
            bool                 smoothNormals)
        {
            Validate(profile, path, profileSmoothing, pathSmoothing);
            int hash = GetHash(profile, path, profileSmoothing, pathSmoothing, smoothNormals);
            if (Meshes.TryGetValue(hash, out List<SweepMeshEntry> entries))
            {
                for (int index = entries.Count - 1; index >= 0; index--)
                {
                    SweepMeshEntry entry = entries[index];
                    if (entry.Mesh == null)
                    {
                        entries.RemoveAt(index);
                        continue;
                    }

                    if (entry.Matches(profile, path, profileSmoothing, pathSmoothing, smoothNormals))
                        return entry.Mesh;
                }
            }
            else
            {
                entries = new();
                Meshes.Add(hash, entries);
            }

            ForgeVector2[] profilePoints = Copy(profile);
            ForgeVector3[] pathPoints    = Copy(path);
            Mesh mesh = Create(
                SmoothClosedProfile(profilePoints, profileSmoothing),
                SmoothOpenPath(pathPoints, pathSmoothing),
                smoothNormals);
            entries.Add(new(
                profilePoints,
                pathPoints,
                profileSmoothing,
                pathSmoothing,
                smoothNormals,
                mesh));
            return mesh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Meshes.Clear();
        }

        private static void Validate(
            IList<ForgeVector2> profile,
            IList<ForgeVector3> path,
            int                  profileSmoothing,
            int                  pathSmoothing)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (profile.Count < 3)
                throw new ArgumentException("Profile sweeps require at least three profile points.", nameof(profile));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path.Count < 2)
                throw new ArgumentException("Profile sweeps require at least two path points.", nameof(path));

            if (profileSmoothing < 0 || profileSmoothing > 4)
                throw new ArgumentOutOfRangeException(nameof(profileSmoothing));

            if (pathSmoothing < 0 || pathSmoothing > 4)
                throw new ArgumentOutOfRangeException(nameof(pathSmoothing));

            int  generatedProfilePoints = profile.Count << profileSmoothing;
            int  generatedPathPoints    = path.Count << pathSmoothing;
            long estimatedVertices      = ((long)(generatedPathPoints - 1) * generatedProfilePoints * 4) +
                                          ((generatedProfilePoints - 2L) * 6L);
            if (estimatedVertices > 60000L)
                throw new ArgumentException(
                    "Profile sweep quality exceeds the 60,000 vertex generation budget.",
                    nameof(path));

            for (int index = 1; index < path.Count; index++)
            {
                if (path[index].Equals(path[index - 1]))
                    throw new ArgumentException("Profile sweep path points must not repeat consecutively.", nameof(path));
            }
        }

        private static Mesh Create(
            ForgeVector2[] profile,
            ForgeVector3[] path,
            bool           smoothNormals)
        {
            ForgeVector2[] outline   = EnsureCounterClockwise(profile);
            Vector3[]      centers   = ToUnity(path);
            SweepFrame[]   frames    = CreateFrames(centers);
            Vector3[][]    rings     = CreateRings(outline, centers, frames);
            MeshBuilder    builder   = new();
            for (int pathIndex = 0; pathIndex < rings.Length - 1; pathIndex++)
            {
                Vector3[] current = rings[pathIndex];
                Vector3[] next    = rings[pathIndex + 1];
                for (int profileIndex = 0; profileIndex < outline.Length; profileIndex++)
                {
                    int following = (profileIndex + 1) % outline.Length;
                    builder.AddQuad(
                        current[profileIndex],
                        current[following],
                        next[following],
                        next[profileIndex]);
                }
            }

            List<int> triangles = Triangulate(outline);
            AddCap(builder, rings[0], triangles, true);
            AddCap(builder, rings[rings.Length - 1], triangles, false);
            return builder.Build("Low Poly Profile Sweep", smoothNormals);
        }

        private static SweepFrame[] CreateFrames(IList<Vector3> path)
        {
            SweepFrame[] frames       = new SweepFrame[path.Count];
            Vector3      firstTangent = GetTangent(path, 0);
            Vector3      reference    = Mathf.Abs(Vector3.Dot(firstTangent, Vector3.up)) < 0.95f
                ? Vector3.up
                : Vector3.right;
            Vector3 normal   = Vector3.Cross(firstTangent, reference).normalized;
            Vector3 binormal = Vector3.Cross(firstTangent, normal).normalized;
            frames[0] = new(firstTangent, normal, binormal);

            for (int index = 1; index < path.Count; index++)
            {
                Vector3 tangent = GetTangent(path, index);
                normal = Quaternion.FromToRotation(frames[index - 1].Tangent, tangent) * normal;
                normal = (normal - (tangent * Vector3.Dot(normal, tangent))).normalized;
                if (normal.sqrMagnitude < 0.000001f)
                    normal = Vector3.Cross(tangent, reference).normalized;

                binormal     = Vector3.Cross(tangent, normal).normalized;
                frames[index] = new(tangent, normal, binormal);
            }

            return frames;
        }

        private static Vector3 GetTangent(IList<Vector3> path, int index)
        {
            Vector3 tangent;
            if (index == 0)
                tangent = path[1] - path[0];
            else if (index == path.Count - 1)
                tangent = path[index] - path[index - 1];
            else
                tangent = path[index + 1] - path[index - 1];

            if (tangent.sqrMagnitude < 0.000001f)
                throw new ArgumentException("Profile sweep paths cannot reverse directly through one point.", nameof(path));

            return tangent.normalized;
        }

        private static Vector3[][] CreateRings(
            IList<ForgeVector2> profile,
            IList<Vector3>      centers,
            IList<SweepFrame>   frames)
        {
            Vector3[][] rings = new Vector3[centers.Count][];
            for (int pathIndex = 0; pathIndex < centers.Count; pathIndex++)
            {
                rings[pathIndex] = new Vector3[profile.Count];
                for (int profileIndex = 0; profileIndex < profile.Count; profileIndex++)
                {
                    ForgeVector2 point = profile[profileIndex];
                    rings[pathIndex][profileIndex] = centers[pathIndex] +
                                                     (frames[pathIndex].Normal * point.X) +
                                                     (frames[pathIndex].Binormal * point.Y);
                }
            }

            return rings;
        }

        private static void AddCap(
            MeshBuilder    builder,
            IList<Vector3> ring,
            IList<int>     triangles,
            bool           reverse)
        {
            for (int index = 0; index < triangles.Count; index += 3)
            {
                Vector3 first  = ring[triangles[index]];
                Vector3 second = ring[triangles[index + 1]];
                Vector3 third  = ring[triangles[index + 2]];
                if (reverse)
                    builder.AddTriangle(third, second, first);
                else
                    builder.AddTriangle(first, second, third);
            }
        }

        private static ForgeVector2[] SmoothClosedProfile(ForgeVector2[] points, int iterations)
        {
            ForgeVector2[] result = points;
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                ForgeVector2[] smoothed = new ForgeVector2[result.Length * 2];
                for (int index = 0; index < result.Length; index++)
                {
                    ForgeVector2 current = result[index];
                    ForgeVector2 next    = result[(index + 1) % result.Length];
                    smoothed[index * 2]       = Lerp(current, next, 0.25f);
                    smoothed[(index * 2) + 1] = Lerp(current, next, 0.75f);
                }

                result = smoothed;
            }

            return result;
        }

        private static ForgeVector3[] SmoothOpenPath(ForgeVector3[] points, int iterations)
        {
            ForgeVector3[] result = points;
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                ForgeVector3[] smoothed = new ForgeVector3[result.Length * 2];
                int            write    = 0;
                smoothed[write++] = result[0];
                for (int index = 0; index < result.Length - 1; index++)
                {
                    smoothed[write++] = Lerp(result[index], result[index + 1], 0.25f);
                    smoothed[write++] = Lerp(result[index], result[index + 1], 0.75f);
                }

                smoothed[write] = result[result.Length - 1];
                result          = smoothed;
            }

            return result;
        }

        private static ForgeVector2 Lerp(ForgeVector2 first, ForgeVector2 second, float time)
        {
            return new(
                Mathf.Lerp(first.X, second.X, time),
                Mathf.Lerp(first.Y, second.Y, time));
        }

        private static ForgeVector3 Lerp(ForgeVector3 first, ForgeVector3 second, float time)
        {
            return new(
                Mathf.Lerp(first.X, second.X, time),
                Mathf.Lerp(first.Y, second.Y, time),
                Mathf.Lerp(first.Z, second.Z, time));
        }

        private static ForgeVector2[] Copy(IList<ForgeVector2> source)
        {
            ForgeVector2[] result = new ForgeVector2[source.Count];
            for (int index = 0; index < source.Count; index++)
                result[index] = source[index];

            return result;
        }

        private static ForgeVector3[] Copy(IList<ForgeVector3> source)
        {
            ForgeVector3[] result = new ForgeVector3[source.Count];
            for (int index = 0; index < source.Count; index++)
                result[index] = source[index];

            return result;
        }

        private static Vector3[] ToUnity(IList<ForgeVector3> source)
        {
            Vector3[] result = new Vector3[source.Count];
            for (int index = 0; index < source.Count; index++)
                result[index] = new(source[index].X, source[index].Y, source[index].Z);

            return result;
        }

        private static int GetHash(
            IList<ForgeVector2> profile,
            IList<ForgeVector3> path,
            int                  profileSmoothing,
            int                  pathSmoothing,
            bool                 smoothNormals)
        {
            int hash = profileSmoothing;
            hash = (hash * 397) ^ pathSmoothing;
            hash = (hash * 397) ^ smoothNormals.GetHashCode();
            foreach (ForgeVector2 point in profile)
                hash = (hash * 397) ^ point.GetHashCode();

            foreach (ForgeVector3 point in path)
                hash = (hash * 397) ^ point.GetHashCode();

            return hash;
        }

        private static ForgeVector2[] EnsureCounterClockwise(ForgeVector2[] profile)
        {
            ForgeVector2[] result = (ForgeVector2[])profile.Clone();
            float          area   = 0f;
            for (int index = 0; index < result.Length; index++)
            {
                ForgeVector2 current = result[index];
                ForgeVector2 next    = result[(index + 1) % result.Length];
                area += (current.X * next.Y) - (next.X * current.Y);
            }

            if (Mathf.Approximately(area, 0f))
                throw new ArgumentException("Profile sweep points must enclose an area.", nameof(profile));

            if (area < 0f)
                Array.Reverse(result);

            return result;
        }

        private static List<int> Triangulate(IList<ForgeVector2> points)
        {
            List<int> remaining = new(points.Count);
            List<int> result    = new((points.Count - 2) * 3);
            for (int index = 0; index < points.Count; index++)
                remaining.Add(index);

            while (remaining.Count > 3)
            {
                bool clipped = false;
                for (int index = 0; index < remaining.Count; index++)
                {
                    int previous = remaining[(index + remaining.Count - 1) % remaining.Count];
                    int current  = remaining[index];
                    int next     = remaining[(index + 1) % remaining.Count];
                    if (Cross(points[previous], points[current], points[next]) <= 0f)
                        continue;

                    bool contains = false;
                    foreach (int candidate in remaining)
                    {
                        if (candidate == previous || candidate == current || candidate == next)
                            continue;

                        if (IsInsideTriangle(points[candidate], points[previous], points[current], points[next]))
                        {
                            contains = true;
                            break;
                        }
                    }

                    if (contains)
                        continue;

                    result.Add(previous);
                    result.Add(current);
                    result.Add(next);
                    remaining.RemoveAt(index);
                    clipped = true;
                    break;
                }

                if (!clipped)
                    throw new ArgumentException("Profile sweep points must form a simple polygon.", nameof(points));
            }

            result.Add(remaining[0]);
            result.Add(remaining[1]);
            result.Add(remaining[2]);
            return result;
        }

        private static float Cross(ForgeVector2 first, ForgeVector2 second, ForgeVector2 third)
        {
            return ((second.X - first.X) * (third.Y - first.Y)) -
                   ((second.Y - first.Y) * (third.X - first.X));
        }

        private static bool IsInsideTriangle(
            ForgeVector2 point,
            ForgeVector2 first,
            ForgeVector2 second,
            ForgeVector2 third)
        {
            return Cross(first, second, point) >= 0f &&
                   Cross(second, third, point) >= 0f &&
                   Cross(third, first, point) >= 0f;
        }

        /// <summary>
        /// Stores one stable orientation frame along the sweep path.
        /// </summary>
        private readonly struct SweepFrame
        {
            public SweepFrame(Vector3 tangent, Vector3 normal, Vector3 binormal)
            {
                Tangent  = tangent;
                Normal   = normal;
                Binormal = binormal;
            }

            public Vector3 Tangent { get; }

            public Vector3 Normal { get; }

            public Vector3 Binormal { get; }
        }

        /// <summary>
        /// Builds flat or averaged-normal sweep meshes with duplicated face vertices.
        /// </summary>
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> vertices  = new();
            private readonly List<Vector3> normals   = new();
            private readonly List<int>     triangles = new();

            public void AddTriangle(Vector3 first, Vector3 second, Vector3 third)
            {
                int     start  = vertices.Count;
                Vector3 normal = Vector3.Cross(second - first, third - first).normalized;
                vertices.Add(first);
                vertices.Add(second);
                vertices.Add(third);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }

            public void AddQuad(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth)
            {
                int     start  = vertices.Count;
                Vector3 normal = Vector3.Cross(second - first, third - first).normalized;
                vertices.Add(first);
                vertices.Add(second);
                vertices.Add(third);
                vertices.Add(fourth);
                for (int index = 0; index < 4; index++)
                    normals.Add(normal);

                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }

            public Mesh Build(string name, bool smoothNormals)
            {
                if (smoothNormals)
                    SmoothNormals();

                Mesh mesh = new() { name = name };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTriangles(triangles, 0, true);
                mesh.RecalculateBounds();
                return mesh;
            }

            private void SmoothNormals()
            {
                Dictionary<Vector3, Vector3> sums = new();
                for (int index = 0; index < vertices.Count; index++)
                {
                    Vector3 vertex = vertices[index];
                    sums.TryGetValue(vertex, out Vector3 sum);
                    sums[vertex] = sum + normals[index];
                }

                for (int index = 0; index < normals.Count; index++)
                    normals[index] = sums[vertices[index]].normalized;
            }
        }

        /// <summary>
        /// Owns immutable sweep inputs and their generated Unity mesh.
        /// </summary>
        private sealed class SweepMeshEntry
        {
            private readonly ForgeVector2[] profile;
            private readonly ForgeVector3[] path;
            private readonly int            profileSmoothing;
            private readonly int            pathSmoothing;
            private readonly bool           smoothNormals;

            public SweepMeshEntry(
                ForgeVector2[] profile,
                ForgeVector3[] path,
                int            profileSmoothing,
                int            pathSmoothing,
                bool           smoothNormals,
                Mesh           mesh)
            {
                this.profile          = profile;
                this.path             = path;
                this.profileSmoothing = profileSmoothing;
                this.pathSmoothing    = pathSmoothing;
                this.smoothNormals    = smoothNormals;
                Mesh                  = mesh;
            }

            public Mesh Mesh { get; }

            public bool Matches(
                IList<ForgeVector2> candidateProfile,
                IList<ForgeVector3> candidatePath,
                int                 candidateProfileSmoothing,
                int                 candidatePathSmoothing,
                bool                candidateSmoothNormals)
            {
                if (profile.Length != candidateProfile.Count ||
                    path.Length != candidatePath.Count ||
                    profileSmoothing != candidateProfileSmoothing ||
                    pathSmoothing != candidatePathSmoothing ||
                    smoothNormals != candidateSmoothNormals)
                    return false;

                for (int index = 0; index < profile.Length; index++)
                {
                    if (!profile[index].Equals(candidateProfile[index]))
                        return false;
                }

                for (int index = 0; index < path.Length; index++)
                {
                    if (!path[index].Equals(candidatePath[index]))
                        return false;
                }

                return true;
            }
        }
    }
}
