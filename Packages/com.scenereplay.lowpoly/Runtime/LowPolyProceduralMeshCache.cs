using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Creates and shares immutable procedural meshes for equivalent Low Poly parameters.
    /// </summary>
    internal static class LowPolyProceduralMeshCache
    {
        private static readonly Dictionary<MeshKey, Mesh>                    Meshes        = new();
        private static readonly Dictionary<int, List<ProfileMeshEntry>>      ProfileMeshes = new();
        private static readonly Dictionary<int, List<LoftMeshEntry>>         LoftMeshes    = new();
        private static readonly Dictionary<int, List<LatheMeshEntry>>        LatheMeshes   = new();
        private static readonly Dictionary<int, List<CageMeshEntry>>         CageMeshes    = new();

        public static Mesh GetWedge()
        {
            MeshKey key = new(LowPolyShapeTypes.Wedge, 0f, 0f, 0f, 0f);
            return GetOrCreate(key, CreateWedge);
        }

        public static Mesh GetFrustum(float topWidth, float topDepth, float bottomWidth, float bottomDepth)
        {
            MeshKey key = new(
                LowPolyShapeTypes.Frustum,
                topWidth,
                topDepth,
                bottomWidth,
                bottomDepth);
            return GetOrCreate(key, () => CreateFrustum(topWidth, topDepth, bottomWidth, bottomDepth));
        }

        public static Mesh GetExtrudedProfile(
            IList<ForgeVector2> profile,
            float                depth,
            float                bevel,
            int                  bevelSegments,
            int                  smoothing)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (profile.Count < 3)
                throw new ArgumentException("Extruded profiles require at least three points.", nameof(profile));

            if (depth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Profile depth must be positive.");

            if (bevel < 0f || bevel * 2f >= depth)
                throw new ArgumentOutOfRangeException(
                    nameof(bevel),
                    bevel,
                    "Profile bevel must be non-negative and less than half the profile depth.");

            if (bevelSegments < 1 || bevelSegments > 8)
                throw new ArgumentOutOfRangeException(nameof(bevelSegments));

            ValidateSmoothing(smoothing);

            int hash = GetProfileHash(profile, depth, bevel, bevelSegments, smoothing);
            if (ProfileMeshes.TryGetValue(hash, out List<ProfileMeshEntry> entries))
            {
                for (int index = entries.Count - 1; index >= 0; index--)
                {
                    ProfileMeshEntry entry = entries[index];
                    if (entry.Mesh == null)
                    {
                        entries.RemoveAt(index);
                        continue;
                    }

                    if (entry.Matches(profile, depth, bevel, bevelSegments, smoothing))
                        return entry.Mesh;
                }
            }
            else
            {
                entries = new();
                ProfileMeshes.Add(hash, entries);
            }

            ForgeVector2[] points = CopyProfile(profile);
            Mesh           mesh   = CreateExtrudedProfile(
                SmoothProfile(points, smoothing, true),
                depth,
                bevel,
                bevelSegments);
            entries.Add(new(points, depth, bevel, bevelSegments, smoothing, mesh));
            return mesh;
        }

        public static Mesh GetProfileLoft(
            IList<ForgeVector2>       profile,
            IList<ShapeProfileSection> sections,
            int                        subdivisions,
            bool                       smoothNormals,
            int                        smoothing)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (profile.Count < 3)
                throw new ArgumentException("Profile lofts require at least three points.", nameof(profile));

            ValidateLoftSections(sections);
            if (subdivisions < 0 || subdivisions > 8)
                throw new ArgumentOutOfRangeException(nameof(subdivisions));

            ValidateSmoothing(smoothing);

            int hash = GetLoftHash(profile, sections, subdivisions, smoothNormals, smoothing);
            if (LoftMeshes.TryGetValue(hash, out List<LoftMeshEntry> entries))
            {
                for (int index = entries.Count - 1; index >= 0; index--)
                {
                    LoftMeshEntry entry = entries[index];
                    if (entry.Mesh == null)
                    {
                        entries.RemoveAt(index);
                        continue;
                    }

                    if (entry.Matches(profile, sections, subdivisions, smoothNormals, smoothing))
                        return entry.Mesh;
                }
            }
            else
            {
                entries = new();
                LoftMeshes.Add(hash, entries);
            }

            ForgeVector2[] points   = CopyProfile(profile);
            LoftSection[] loft      = CopySections(sections);
            Mesh          mesh      = CreateProfileLoft(
                SmoothProfile(points, smoothing, true),
                loft,
                subdivisions,
                smoothNormals);
            entries.Add(new(points, loft, subdivisions, smoothNormals, smoothing, mesh));
            return mesh;
        }

        public static Mesh GetLatheProfile(
            IList<ForgeVector2> profile,
            int                  radialSegments,
            bool                 smoothNormals,
            int                  smoothing)
        {
            ValidateLatheProfile(profile, radialSegments);
            ValidateSmoothing(smoothing);
            int hash = GetLatheHash(profile, radialSegments, smoothNormals, smoothing);
            if (LatheMeshes.TryGetValue(hash, out List<LatheMeshEntry> entries))
            {
                for (int index = entries.Count - 1; index >= 0; index--)
                {
                    LatheMeshEntry entry = entries[index];
                    if (entry.Mesh == null)
                    {
                        entries.RemoveAt(index);
                        continue;
                    }

                    if (entry.Matches(profile, radialSegments, smoothNormals, smoothing))
                        return entry.Mesh;
                }
            }
            else
            {
                entries = new();
                LatheMeshes.Add(hash, entries);
            }

            ForgeVector2[] points = CopyProfile(profile);
            Mesh           mesh   = CreateLatheProfile(
                SmoothProfile(points, smoothing, false),
                radialSegments,
                smoothNormals);
            entries.Add(new(points, radialSegments, smoothNormals, smoothing, mesh));
            return mesh;
        }

        public static Mesh GetProfileCage(
            IList<ShapeProfileCageSection> sections,
            int                             subdivisions,
            bool                            smoothNormals,
            int                             smoothing)
        {
            ValidateCageSections(sections);
            if (subdivisions < 0 || subdivisions > 8)
                throw new ArgumentOutOfRangeException(nameof(subdivisions));

            ValidateSmoothing(smoothing);

            int hash = GetCageHash(sections, subdivisions, smoothNormals, smoothing);
            if (CageMeshes.TryGetValue(hash, out List<CageMeshEntry> entries))
            {
                for (int index = entries.Count - 1; index >= 0; index--)
                {
                    CageMeshEntry entry = entries[index];
                    if (entry.Mesh == null)
                    {
                        entries.RemoveAt(index);
                        continue;
                    }

                    if (entry.Matches(sections, subdivisions, smoothNormals, smoothing))
                        return entry.Mesh;
                }
            }
            else
            {
                entries = new();
                CageMeshes.Add(hash, entries);
            }

            CageSection[] snapshot = CopyCageSections(sections);
            Mesh          mesh     = CreateProfileCage(snapshot, subdivisions, smoothNormals, smoothing);
            entries.Add(new(snapshot, subdivisions, smoothNormals, smoothing, mesh));
            return mesh;
        }

        private static void ValidateCageSections(IList<ShapeProfileCageSection> sections)
        {
            if (sections == null)
                throw new ArgumentNullException(nameof(sections));

            if (sections.Count < 2)
                throw new ArgumentException("Profile cages require at least two sections.", nameof(sections));

            int pointCount = -1;
            for (int index = 0; index < sections.Count; index++)
            {
                ShapeProfileCageSection section = sections[index] ??
                                                  throw new ArgumentException(
                                                      "Profile cage sections cannot contain null entries.",
                                                      nameof(sections));
                if (section.Profile == null || section.Profile.Count < 3)
                    throw new ArgumentException(
                        "Profile cage sections require at least three points.", nameof(sections));

                if (index > 0 && section.Z <= sections[index - 1].Z)
                    throw new ArgumentException(
                        "Profile cage sections must be ordered by increasing depth.", nameof(sections));

                if (pointCount >= 0 && section.Profile.Count != pointCount)
                    throw new ArgumentException(
                        "Profile cage sections must use the same point count.", nameof(sections));

                pointCount = section.Profile.Count;
            }
        }

        private static void ValidateLatheProfile(IList<ForgeVector2> profile, int radialSegments)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (profile.Count < 2)
                throw new ArgumentException("Lathe profiles require at least two radius-height points.", nameof(profile));

            if (radialSegments < 3 || radialSegments > 64)
                throw new ArgumentOutOfRangeException(nameof(radialSegments));

            float previousHeight = float.NegativeInfinity;
            foreach (ForgeVector2 point in profile)
            {
                if (point.X < 0f)
                    throw new ArgumentOutOfRangeException(nameof(profile), "Lathe profile radii cannot be negative.");

                if (point.Y <= previousHeight)
                    throw new ArgumentException("Lathe profile heights must be strictly increasing.", nameof(profile));

                previousHeight = point.Y;
            }
        }

        private static int GetLatheHash(
            IList<ForgeVector2> profile,
            int                  radialSegments,
            bool                 smoothNormals,
            int                  smoothing)
        {
            int hash = (GetProfileHash(profile, 0f, 0f, 1, smoothing) * 397) ^ radialSegments;
            return (hash * 397) ^ smoothNormals.GetHashCode();
        }

        private static void ValidateLoftSections(IList<ShapeProfileSection> sections)
        {
            if (sections == null)
                throw new ArgumentNullException(nameof(sections));

            if (sections.Count < 2)
                throw new ArgumentException("Profile lofts require at least two sections.", nameof(sections));

            float previousZ = float.NegativeInfinity;
            foreach (ShapeProfileSection section in sections)
            {
                if (section == null)
                    throw new ArgumentException("Profile loft sections cannot contain null entries.", nameof(sections));

                if (section.Scale.X <= 0f || section.Scale.Y <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(sections), "Profile loft section scales must be positive.");

                if (section.Z <= previousZ)
                    throw new ArgumentException("Profile loft sections must be ordered by increasing depth.", nameof(sections));

                previousZ = section.Z;
            }
        }

        private static int GetProfileHash(
            IList<ForgeVector2> profile,
            float                depth,
            float                bevel,
            int                  bevelSegments,
            int                  smoothing)
        {
            int hash = (depth.GetHashCode() * 397) ^ bevel.GetHashCode();
            hash     = (hash * 397) ^ bevelSegments;
            hash     = (hash * 397) ^ smoothing;
            for (int index = 0; index < profile.Count; index++)
                hash = (hash * 397) ^ profile[index].GetHashCode();

            return hash;
        }

        private static ForgeVector2[] CopyProfile(IList<ForgeVector2> profile)
        {
            ForgeVector2[] points = new ForgeVector2[profile.Count];
            for (int index = 0; index < profile.Count; index++)
                points[index] = profile[index];

            return points;
        }

        private static void ValidateSmoothing(int smoothing)
        {
            if (smoothing < 0 || smoothing > 4)
                throw new ArgumentOutOfRangeException(nameof(smoothing));
        }

        private static ForgeVector2[] SmoothProfile(
            ForgeVector2[] points,
            int            iterations,
            bool           closed)
        {
            ForgeVector2[] result = points;
            for (int iteration = 0; iteration < iterations; iteration++)
                result = SmoothProfileOnce(result, closed);

            return result;
        }

        private static ForgeVector2[] SmoothProfileOnce(ForgeVector2[] points, bool closed)
        {
            int            spanCount = closed ? points.Length : points.Length - 1;
            int            offset    = closed ? 0 : 1;
            ForgeVector2[] result    = new ForgeVector2[(spanCount * 2) + (offset * 2)];
            int            write     = 0;
            if (!closed)
                result[write++] = points[0];

            for (int index = 0; index < spanCount; index++)
            {
                ForgeVector2 current = points[index];
                ForgeVector2 next    = points[(index + 1) % points.Length];
                result[write++] = Lerp(current, next, 0.25f);
                result[write++] = Lerp(current, next, 0.75f);
            }

            if (!closed)
                result[write] = points[points.Length - 1];

            return result;
        }

        private static ForgeVector2 Lerp(ForgeVector2 first, ForgeVector2 second, float time)
        {
            return new(
                Mathf.Lerp(first.X, second.X, time),
                Mathf.Lerp(first.Y, second.Y, time));
        }

        private static int GetLoftHash(
            IList<ForgeVector2>       profile,
            IList<ShapeProfileSection> sections,
            int                        subdivisions,
            bool                       smoothNormals,
            int                        smoothing)
        {
            int hash = (GetProfileHash(profile, 0f, 0f, 1, smoothing) * 397) ^ subdivisions;
            hash     = (hash * 397) ^ smoothNormals.GetHashCode();
            foreach (ShapeProfileSection section in sections)
            {
                hash = (hash * 397) ^ section.Z.GetHashCode();
                hash = (hash * 397) ^ section.Scale.GetHashCode();
                hash = (hash * 397) ^ section.Offset.GetHashCode();
            }

            return hash;
        }

        private static LoftSection[] CopySections(IList<ShapeProfileSection> sections)
        {
            LoftSection[] result = new LoftSection[sections.Count];
            for (int index = 0; index < sections.Count; index++)
            {
                ShapeProfileSection section = sections[index];
                result[index] = new(
                    section.Z,
                    section.Scale.X,
                    section.Scale.Y,
                    section.Offset.X,
                    section.Offset.Y);
            }

            return result;
        }

        private static int GetCageHash(
            IList<ShapeProfileCageSection> sections,
            int                             subdivisions,
            bool                            smoothNormals,
            int                             smoothing)
        {
            int hash = (subdivisions * 397) ^ smoothNormals.GetHashCode();
            hash     = (hash * 397) ^ smoothing;
            foreach (ShapeProfileCageSection section in sections)
            {
                hash = (hash * 397) ^ section.Z.GetHashCode();
                foreach (ForgeVector2 point in section.Profile)
                    hash = (hash * 397) ^ point.GetHashCode();
            }

            return hash;
        }

        private static CageSection[] CopyCageSections(IList<ShapeProfileCageSection> sections)
        {
            CageSection[] result = new CageSection[sections.Count];
            for (int index = 0; index < sections.Count; index++)
                result[index] = new(sections[index].Z, CopyProfile(sections[index].Profile));

            return result;
        }

        private static Mesh GetOrCreate(MeshKey key, Func<Mesh> create)
        {
            if (Meshes.TryGetValue(key, out Mesh mesh))
            {
                if (mesh != null)
                    return mesh;

                Meshes.Remove(key);
            }

            mesh = create();
            Meshes.Add(key, mesh);
            return mesh;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Meshes.Clear();
            ProfileMeshes.Clear();
            LoftMeshes.Clear();
            LatheMeshes.Clear();
            CageMeshes.Clear();
        }

        private static Mesh CreateWedge()
        {
            Vector3 bottomFrontLeft  = new(-0.5f, -0.5f, -0.5f);
            Vector3 bottomFrontRight = new(0.5f, -0.5f, -0.5f);
            Vector3 bottomBackRight  = new(0.5f, -0.5f, 0.5f);
            Vector3 bottomBackLeft   = new(-0.5f, -0.5f, 0.5f);
            Vector3 topBackRight     = new(0.5f, 0.5f, 0.5f);
            Vector3 topBackLeft      = new(-0.5f, 0.5f, 0.5f);
            MeshBuilder builder      = new();

            builder.AddQuad(bottomFrontLeft, bottomFrontRight, bottomBackRight, bottomBackLeft);
            builder.AddQuad(bottomFrontRight, bottomFrontLeft, topBackLeft, topBackRight);
            builder.AddQuad(bottomBackLeft, bottomBackRight, topBackRight, topBackLeft);
            builder.AddTriangle(bottomFrontLeft, bottomBackLeft, topBackLeft);
            builder.AddTriangle(bottomBackRight, bottomFrontRight, topBackRight);
            return builder.Build("Low Poly Wedge");
        }

        private static Mesh CreateFrustum(
            float topWidth,
            float topDepth,
            float bottomWidth,
            float bottomDepth)
        {
            float topHalfWidth    = topWidth * 0.5f;
            float topHalfDepth    = topDepth * 0.5f;
            float bottomHalfWidth = bottomWidth * 0.5f;
            float bottomHalfDepth = bottomDepth * 0.5f;
            Vector3 bottomFrontLeft  = new(-bottomHalfWidth, -0.5f, -bottomHalfDepth);
            Vector3 bottomFrontRight = new(bottomHalfWidth, -0.5f, -bottomHalfDepth);
            Vector3 bottomBackRight  = new(bottomHalfWidth, -0.5f, bottomHalfDepth);
            Vector3 bottomBackLeft   = new(-bottomHalfWidth, -0.5f, bottomHalfDepth);
            Vector3 topFrontLeft     = new(-topHalfWidth, 0.5f, -topHalfDepth);
            Vector3 topFrontRight    = new(topHalfWidth, 0.5f, -topHalfDepth);
            Vector3 topBackRight     = new(topHalfWidth, 0.5f, topHalfDepth);
            Vector3 topBackLeft      = new(-topHalfWidth, 0.5f, topHalfDepth);
            MeshBuilder builder      = new();

            builder.AddQuad(bottomFrontLeft, bottomFrontRight, bottomBackRight, bottomBackLeft);
            builder.AddQuad(topBackLeft, topBackRight, topFrontRight, topFrontLeft);
            builder.AddQuad(bottomFrontRight, bottomFrontLeft, topFrontLeft, topFrontRight);
            builder.AddQuad(bottomBackLeft, bottomBackRight, topBackRight, topBackLeft);
            builder.AddQuad(bottomFrontLeft, bottomBackLeft, topBackLeft, topFrontLeft);
            builder.AddQuad(bottomBackRight, bottomFrontRight, topFrontRight, topBackRight);
            return builder.Build("Low Poly Frustum");
        }

        private static Mesh CreateExtrudedProfile(
            ForgeVector2[] profile,
            float          depth,
            float          bevel,
            int            bevelSegments)
        {
            ForgeVector2[] points       = EnsureCounterClockwise(profile);
            ForgeVector2[] facePoints   = bevel > 0f ? InsetProfile(points, bevel) : points;
            List<int>     triangles     = Triangulate(facePoints);
            float         halfDepth     = depth * 0.5f;
            float         frontRim      = -halfDepth + bevel;
            float         backRim       = halfDepth - bevel;
            MeshBuilder   builder       = new();

            for (int index = 0; index < triangles.Count; index += 3)
            {
                Vector3 first  = ToVector3(facePoints[triangles[index]], -halfDepth);
                Vector3 second = ToVector3(facePoints[triangles[index + 1]], -halfDepth);
                Vector3 third  = ToVector3(facePoints[triangles[index + 2]], -halfDepth);
                builder.AddTriangle(third, second, first);

                first  = ToVector3(facePoints[triangles[index]], halfDepth);
                second = ToVector3(facePoints[triangles[index + 1]], halfDepth);
                third  = ToVector3(facePoints[triangles[index + 2]], halfDepth);
                builder.AddTriangle(first, second, third);
            }

            if (bevel > 0f)
                AddRoundedBevel(builder, points, halfDepth, bevel, bevelSegments);

            for (int index = 0; index < points.Length; index++)
            {
                int next = (index + 1) % points.Length;
                builder.AddQuad(
                    ToVector3(points[index], frontRim),
                    ToVector3(points[next], frontRim),
                    ToVector3(points[next], backRim),
                    ToVector3(points[index], backRim));
            }

            return builder.Build("Low Poly Extruded Profile");
        }

        private static void AddRoundedBevel(
            MeshBuilder   builder,
            ForgeVector2[] points,
            float         halfDepth,
            float         bevel,
            int           segments)
        {
            ForgeVector2[][] rings = new ForgeVector2[segments + 1][];
            float[]         offsets = new float[segments + 1];
            for (int ringIndex = 0; ringIndex <= segments; ringIndex++)
            {
                float angle       = ringIndex * Mathf.PI * 0.5f / segments;
                float inset       = bevel * Mathf.Cos(angle);
                offsets[ringIndex] = bevel * Mathf.Sin(angle);
                rings[ringIndex]   = inset > 0.00001f ? InsetProfile(points, inset) : points;
            }

            for (int ringIndex = 0; ringIndex < segments; ringIndex++)
            {
                ForgeVector2[] inner = rings[ringIndex];
                ForgeVector2[] outer = rings[ringIndex + 1];
                float frontInnerZ    = -halfDepth + offsets[ringIndex];
                float frontOuterZ    = -halfDepth + offsets[ringIndex + 1];
                float backInnerZ     = halfDepth - offsets[ringIndex];
                float backOuterZ     = halfDepth - offsets[ringIndex + 1];
                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    int next = (pointIndex + 1) % points.Length;
                    builder.AddQuad(
                        ToVector3(inner[pointIndex], frontInnerZ),
                        ToVector3(inner[next], frontInnerZ),
                        ToVector3(outer[next], frontOuterZ),
                        ToVector3(outer[pointIndex], frontOuterZ));
                    builder.AddQuad(
                        ToVector3(outer[pointIndex], backOuterZ),
                        ToVector3(outer[next], backOuterZ),
                        ToVector3(inner[next], backInnerZ),
                        ToVector3(inner[pointIndex], backInnerZ));
                }
            }
        }

        private static Mesh CreateProfileLoft(
            ForgeVector2[] profile,
            LoftSection[] sections,
            int           subdivisions,
            bool          smoothNormals)
        {
            ForgeVector2[] points    = EnsureCounterClockwise(profile);
            sections                 = InterpolateSections(sections, subdivisions);
            ForgeVector2[][] rings   = new ForgeVector2[sections.Length][];
            MeshBuilder    builder   = new();
            for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            {
                LoftSection section = sections[sectionIndex];
                ForgeVector2[] ring = new ForgeVector2[points.Length];
                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    ForgeVector2 point = points[pointIndex];
                    ring[pointIndex] = new(
                        (point.X * section.ScaleX) + section.OffsetX,
                        (point.Y * section.ScaleY) + section.OffsetY);
                }

                rings[sectionIndex] = ring;
            }

            List<int> frontTriangles = Triangulate(rings[0]);
            List<int> backTriangles  = Triangulate(rings[rings.Length - 1]);
            AddLoftCap(builder, rings[0], sections[0].Z, frontTriangles, true);
            AddLoftCap(
                builder,
                rings[rings.Length - 1],
                sections[sections.Length - 1].Z,
                backTriangles,
                false);

            for (int sectionIndex = 0; sectionIndex < rings.Length - 1; sectionIndex++)
            {
                ForgeVector2[] front = rings[sectionIndex];
                ForgeVector2[] back  = rings[sectionIndex + 1];
                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    int next = (pointIndex + 1) % points.Length;
                    builder.AddQuad(
                        ToVector3(front[pointIndex], sections[sectionIndex].Z),
                        ToVector3(front[next], sections[sectionIndex].Z),
                        ToVector3(back[next], sections[sectionIndex + 1].Z),
                        ToVector3(back[pointIndex], sections[sectionIndex + 1].Z));
                }
            }

            return builder.Build("Low Poly Profile Loft", smoothNormals);
        }

        private static Mesh CreateProfileCage(
            CageSection[] sections,
            int           subdivisions,
            bool          smoothNormals,
            int           smoothing)
        {
            CageSection[] prepared = new CageSection[sections.Length];
            for (int index = 0; index < sections.Length; index++)
                prepared[index] = new(
                    sections[index].Z,
                    EnsureCounterClockwise(SmoothProfile(sections[index].Profile, smoothing, true)));

            prepared = InterpolateCageSections(prepared, subdivisions);
            ForgeVector2[][] rings = new ForgeVector2[prepared.Length][];
            for (int index = 0; index < prepared.Length; index++)
                rings[index] = prepared[index].Profile;

            MeshBuilder builder = new();
            AddLoftCap(builder, rings[0], prepared[0].Z, Triangulate(rings[0]), true);
            AddLoftCap(
                builder,
                rings[rings.Length - 1],
                prepared[prepared.Length - 1].Z,
                Triangulate(rings[rings.Length - 1]),
                false);

            int pointCount = rings[0].Length;
            for (int sectionIndex = 0; sectionIndex < prepared.Length - 1; sectionIndex++)
            {
                ForgeVector2[] front = rings[sectionIndex];
                ForgeVector2[] back  = rings[sectionIndex + 1];
                for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
                {
                    int next = (pointIndex + 1) % pointCount;
                    builder.AddQuad(
                        ToVector3(front[pointIndex], prepared[sectionIndex].Z),
                        ToVector3(front[next], prepared[sectionIndex].Z),
                        ToVector3(back[next], prepared[sectionIndex + 1].Z),
                        ToVector3(back[pointIndex], prepared[sectionIndex + 1].Z));
                }
            }

            return builder.Build("Low Poly Profile Cage", smoothNormals);
        }

        private static CageSection[] InterpolateCageSections(CageSection[] sections, int subdivisions)
        {
            if (subdivisions == 0)
                return sections;

            int           stride = subdivisions + 1;
            CageSection[] result = new CageSection[((sections.Length - 1) * stride) + 1];
            int           write  = 0;
            for (int sectionIndex = 0; sectionIndex < sections.Length - 1; sectionIndex++)
            {
                CageSection first  = sections[sectionIndex];
                CageSection second = sections[sectionIndex + 1];
                result[write++] = first;
                for (int step = 1; step <= subdivisions; step++)
                {
                    float time       = step / (float)stride;
                    float smoothTime = time * time * (3f - (2f * time));
                    result[write++] = CageSection.Lerp(first, second, smoothTime);
                }
            }

            result[write] = sections[sections.Length - 1];
            return result;
        }

        private static Mesh CreateLatheProfile(
            ForgeVector2[] profile,
            int            radialSegments,
            bool           smoothNormals)
        {
            MeshBuilder builder = new();
            for (int profileIndex = 0; profileIndex < profile.Length - 1; profileIndex++)
            {
                ForgeVector2 lower = profile[profileIndex];
                ForgeVector2 upper = profile[profileIndex + 1];
                for (int segment = 0; segment < radialSegments; segment++)
                {
                    int     next         = (segment + 1) % radialSegments;
                    Vector3 lowerCurrent = Revolve(lower, segment, radialSegments);
                    Vector3 lowerNext    = Revolve(lower, next, radialSegments);
                    Vector3 upperCurrent = Revolve(upper, segment, radialSegments);
                    Vector3 upperNext    = Revolve(upper, next, radialSegments);
                    if (Mathf.Approximately(lower.X, 0f))
                        builder.AddTriangle(lowerCurrent, upperCurrent, upperNext);
                    else if (Mathf.Approximately(upper.X, 0f))
                        builder.AddTriangle(lowerCurrent, upperCurrent, lowerNext);
                    else
                        builder.AddQuad(lowerNext, lowerCurrent, upperCurrent, upperNext);
                }
            }

            AddLatheCap(builder, profile[0], radialSegments, false);
            AddLatheCap(builder, profile[profile.Length - 1], radialSegments, true);
            return builder.Build("Low Poly Lathe Profile", smoothNormals);
        }

        private static Vector3 Revolve(ForgeVector2 point, int segment, int radialSegments)
        {
            float angle = segment * Mathf.PI * 2f / radialSegments;
            return new(point.X * Mathf.Cos(angle), point.Y, point.X * Mathf.Sin(angle));
        }

        private static void AddLatheCap(
            MeshBuilder  builder,
            ForgeVector2 point,
            int          radialSegments,
            bool         top)
        {
            if (Mathf.Approximately(point.X, 0f))
                return;

            Vector3 center = new(0f, point.Y, 0f);
            for (int segment = 0; segment < radialSegments; segment++)
            {
                int     next      = (segment + 1) % radialSegments;
                Vector3 current   = Revolve(point, segment, radialSegments);
                Vector3 following = Revolve(point, next, radialSegments);
                if (top)
                    builder.AddTriangle(center, following, current);
                else
                    builder.AddTriangle(center, current, following);
            }
        }

        private static LoftSection[] InterpolateSections(LoftSection[] sections, int subdivisions)
        {
            if (subdivisions == 0)
                return sections;

            int           stride = subdivisions + 1;
            LoftSection[] result = new LoftSection[((sections.Length - 1) * stride) + 1];
            int           write  = 0;
            for (int index = 0; index < sections.Length - 1; index++)
            {
                LoftSection first  = sections[index];
                LoftSection second = sections[index + 1];
                for (int step = 0; step < stride; step++)
                {
                    float time       = step / (float)stride;
                    float smoothTime = time * time * (3f - (2f * time));
                    result[write++]  = LoftSection.Lerp(first, second, smoothTime);
                }
            }

            result[write] = sections[sections.Length - 1];
            return result;
        }

        private static void AddLoftCap(
            MeshBuilder         builder,
            IList<ForgeVector2> ring,
            float               z,
            IList<int>          triangles,
            bool                reverse)
        {
            for (int index = 0; index < triangles.Count; index += 3)
            {
                Vector3 first  = ToVector3(ring[triangles[index]], z);
                Vector3 second = ToVector3(ring[triangles[index + 1]], z);
                Vector3 third  = ToVector3(ring[triangles[index + 2]], z);
                if (reverse)
                    builder.AddTriangle(third, second, first);
                else
                    builder.AddTriangle(first, second, third);
            }
        }

        private static ForgeVector2[] InsetProfile(IList<ForgeVector2> points, float inset)
        {
            ForgeVector2[] result = new ForgeVector2[points.Count];
            for (int index = 0; index < points.Count; index++)
            {
                ForgeVector2 previous = points[(index + points.Count - 1) % points.Count];
                ForgeVector2 current  = points[index];
                ForgeVector2 next     = points[(index + 1) % points.Count];
                Vector2      before   = new(current.X - previous.X, current.Y - previous.Y);
                Vector2      after    = new(next.X - current.X, next.Y - current.Y);
                before.Normalize();
                after.Normalize();
                Vector2 beforeNormal = new(-before.y, before.x);
                Vector2 afterNormal  = new(-after.y, after.x);
                Vector2 miter        = (beforeNormal + afterNormal).normalized;
                float   denominator  = Vector2.Dot(miter, afterNormal);
                float   distance     = Mathf.Abs(denominator) > 0.0001f ? inset / denominator : inset;
                distance             = Mathf.Clamp(distance, -inset * 4f, inset * 4f);
                result[index]        = new(current.X + (miter.x * distance), current.Y + (miter.y * distance));
            }

            if (Mathf.Approximately(SignedArea(result), 0f))
                throw new ArgumentException("Profile bevel collapses the supplied outline.", nameof(points));

            return result;
        }

        private static ForgeVector2[] EnsureCounterClockwise(ForgeVector2[] profile)
        {
            ForgeVector2[] points = (ForgeVector2[])profile.Clone();
            float          area   = SignedArea(points);
            if (Mathf.Approximately(area, 0f))
                throw new ArgumentException("Extruded profile points must enclose an area.", nameof(profile));

            if (area < 0f)
                Array.Reverse(points);

            return points;
        }

        private static float SignedArea(IList<ForgeVector2> points)
        {
            float area = 0f;
            for (int index = 0; index < points.Count; index++)
            {
                ForgeVector2 current = points[index];
                ForgeVector2 next    = points[(index + 1) % points.Count];
                area += (current.X * next.Y) - (next.X * current.Y);
            }

            return area * 0.5f;
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
                    if (!IsEar(points, remaining, previous, current, next))
                        continue;

                    result.Add(previous);
                    result.Add(current);
                    result.Add(next);
                    remaining.RemoveAt(index);
                    clipped = true;
                    break;
                }

                if (!clipped)
                    throw new ArgumentException("Extruded profiles must be simple non-self-intersecting polygons.", nameof(points));
            }

            result.Add(remaining[0]);
            result.Add(remaining[1]);
            result.Add(remaining[2]);
            return result;
        }

        private static bool IsEar(
            IList<ForgeVector2> points,
            IList<int>          remaining,
            int                 previous,
            int                 current,
            int                 next)
        {
            ForgeVector2 first  = points[previous];
            ForgeVector2 second = points[current];
            ForgeVector2 third  = points[next];
            if (Cross(first, second, third) <= 0f)
                return false;

            foreach (int candidate in remaining)
            {
                if (candidate == previous || candidate == current || candidate == next)
                    continue;

                if (IsInsideTriangle(points[candidate], first, second, third))
                    return false;
            }

            return true;
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

        private static Vector3 ToVector3(ForgeVector2 point, float z)
        {
            return new(point.X, point.Y, z);
        }

        /// <summary>
        /// Builds flat-shaded meshes with duplicated face vertices.
        /// </summary>
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> vertices  = new();
            private readonly List<Vector3> normals   = new();
            private readonly List<int>     triangles = new();

            public void AddTriangle(Vector3 first, Vector3 second, Vector3 third)
            {
                int start = vertices.Count;
                vertices.Add(first);
                vertices.Add(second);
                vertices.Add(third);
                Vector3 normal = Vector3.Cross(second - first, third - first).normalized;
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }

            public void AddQuad(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth)
            {
                int start = vertices.Count;
                vertices.Add(first);
                vertices.Add(second);
                vertices.Add(third);
                vertices.Add(fourth);
                Vector3 normal = Vector3.Cross(second - first, third - first).normalized;
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }

            public Mesh Build(string name, bool smoothNormals = false)
            {
                Mesh mesh = new() { name = name };
                if (smoothNormals)
                    SmoothNormals();

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

                for (int index = 0; index < vertices.Count; index++)
                    normals[index] = sums[vertices[index]].normalized;
            }
        }

        private readonly struct MeshKey : IEquatable<MeshKey>
        {
            private readonly string type;
            private readonly float  first;
            private readonly float  second;
            private readonly float  third;
            private readonly float  fourth;

            public MeshKey(string type, float first, float second, float third, float fourth)
            {
                this.type   = type;
                this.first  = first;
                this.second = second;
                this.third  = third;
                this.fourth = fourth;
            }

            public bool Equals(MeshKey other)
            {
                return string.Equals(type, other.type, StringComparison.Ordinal) &&
                       first.Equals(other.first) &&
                       second.Equals(other.second) &&
                       third.Equals(other.third) &&
                       fourth.Equals(other.fourth);
            }

            public override bool Equals(object obj)
            {
                return obj is MeshKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = StringComparer.Ordinal.GetHashCode(type);
                    hash = (hash * 397) ^ first.GetHashCode();
                    hash = (hash * 397) ^ second.GetHashCode();
                    hash = (hash * 397) ^ third.GetHashCode();
                    return (hash * 397) ^ fourth.GetHashCode();
                }
            }
        }

        /// <summary>
        /// Owns an immutable profile snapshot so cache keys cannot change after insertion.
        /// </summary>
        private sealed class ProfileMeshEntry
        {
            private readonly float depth;
            private readonly float bevel;
            private readonly int   bevelSegments;
            private readonly int   smoothing;

            public ProfileMeshEntry(
                ForgeVector2[] points,
                float          depth,
                float          bevel,
                int            bevelSegments,
                int            smoothing,
                Mesh           mesh)
            {
                this.depth         = depth;
                this.bevel         = bevel;
                this.bevelSegments = bevelSegments;
                this.smoothing     = smoothing;
                Points             = points;
                Mesh               = mesh;
            }

            public ForgeVector2[] Points { get; }

            public Mesh Mesh { get; }

            public bool Matches(
                IList<ForgeVector2> profile,
                float                candidateDepth,
                float                candidateBevel,
                int                  candidateBevelSegments,
                int                  candidateSmoothing)
            {
                if (!depth.Equals(candidateDepth) ||
                    !bevel.Equals(candidateBevel) ||
                    bevelSegments != candidateBevelSegments ||
                    smoothing != candidateSmoothing ||
                    Points.Length != profile.Count)
                    return false;

                for (int index = 0; index < Points.Length; index++)
                {
                    if (!Points[index].Equals(profile[index]))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Owns immutable profile-loft data and its generated Unity mesh.
        /// </summary>
        private sealed class LoftMeshEntry
        {
            private readonly LoftSection[] sections;
            private readonly int           subdivisions;
            private readonly bool          smoothNormals;
            private readonly int           smoothing;

            public LoftMeshEntry(
                ForgeVector2[] points,
                LoftSection[] sections,
                int           subdivisions,
                bool          smoothNormals,
                int           smoothing,
                Mesh          mesh)
            {
                Points              = points;
                this.sections       = sections;
                this.subdivisions   = subdivisions;
                this.smoothNormals  = smoothNormals;
                this.smoothing      = smoothing;
                Mesh                = mesh;
            }

            public ForgeVector2[] Points { get; }

            public Mesh Mesh { get; }

            public bool Matches(
                IList<ForgeVector2>       profile,
                IList<ShapeProfileSection> candidateSections,
                int                        candidateSubdivisions,
                bool                       candidateSmoothNormals,
                int                        candidateSmoothing)
            {
                if (Points.Length != profile.Count ||
                    sections.Length != candidateSections.Count ||
                    subdivisions != candidateSubdivisions ||
                    smoothNormals != candidateSmoothNormals)
                    return false;

                if (smoothing != candidateSmoothing)
                    return false;

                for (int index = 0; index < Points.Length; index++)
                {
                    if (!Points[index].Equals(profile[index]))
                        return false;
                }

                for (int index = 0; index < sections.Length; index++)
                {
                    if (!sections[index].Matches(candidateSections[index]))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Owns immutable lathe-profile data and its generated Unity mesh.
        /// </summary>
        private sealed class LatheMeshEntry
        {
            private readonly int  radialSegments;
            private readonly bool smoothNormals;
            private readonly int  smoothing;

            public LatheMeshEntry(
                ForgeVector2[] points,
                int            radialSegments,
                bool           smoothNormals,
                int            smoothing,
                Mesh           mesh)
            {
                Points              = points;
                this.radialSegments = radialSegments;
                this.smoothNormals  = smoothNormals;
                this.smoothing      = smoothing;
                Mesh                = mesh;
            }

            public ForgeVector2[] Points { get; }

            public Mesh Mesh { get; }

            public bool Matches(
                IList<ForgeVector2> profile,
                int                  candidateRadialSegments,
                bool                 candidateSmoothNormals,
                int                  candidateSmoothing)
            {
                if (Points.Length != profile.Count ||
                    radialSegments != candidateRadialSegments ||
                    smoothNormals != candidateSmoothNormals ||
                    smoothing != candidateSmoothing)
                    return false;

                for (int index = 0; index < Points.Length; index++)
                {
                    if (!Points[index].Equals(profile[index]))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Owns immutable profile-cage data and its generated Unity mesh.
        /// </summary>
        private sealed class CageMeshEntry
        {
            private readonly CageSection[] sections;
            private readonly int           subdivisions;
            private readonly bool          smoothNormals;
            private readonly int           smoothing;

            public CageMeshEntry(
                CageSection[] sections,
                int           subdivisions,
                bool          smoothNormals,
                int           smoothing,
                Mesh          mesh)
            {
                this.sections      = sections;
                this.subdivisions  = subdivisions;
                this.smoothNormals = smoothNormals;
                this.smoothing     = smoothing;
                Mesh               = mesh;
            }

            public Mesh Mesh { get; }

            public bool Matches(
                IList<ShapeProfileCageSection> candidateSections,
                int                             candidateSubdivisions,
                bool                            candidateSmoothNormals,
                int                             candidateSmoothing)
            {
                if (sections.Length != candidateSections.Count ||
                    subdivisions != candidateSubdivisions ||
                    smoothNormals != candidateSmoothNormals ||
                    smoothing != candidateSmoothing)
                    return false;

                for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
                {
                    if (!sections[sectionIndex].Matches(candidateSections[sectionIndex]))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Stores one immutable profile-cage section without retaining mutable schema objects.
        /// </summary>
        private readonly struct CageSection
        {
            public CageSection(float z, ForgeVector2[] profile)
            {
                Z       = z;
                Profile = profile;
            }

            public float Z { get; }

            public ForgeVector2[] Profile { get; }

            public bool Matches(ShapeProfileCageSection section)
            {
                if (section == null || !Z.Equals(section.Z) || Profile.Length != section.Profile.Count)
                    return false;

                for (int index = 0; index < Profile.Length; index++)
                {
                    if (!Profile[index].Equals(section.Profile[index]))
                        return false;
                }

                return true;
            }

            public static CageSection Lerp(CageSection first, CageSection second, float time)
            {
                ForgeVector2[] profile = new ForgeVector2[first.Profile.Length];
                for (int index = 0; index < profile.Length; index++)
                    profile[index] = LowPolyProceduralMeshCache.Lerp(
                        first.Profile[index],
                        second.Profile[index],
                        time);

                return new(Mathf.Lerp(first.Z, second.Z, time), profile);
            }
        }

        /// <summary>
        /// Stores one immutable numeric loft section without retaining mutable schema objects.
        /// </summary>
        private readonly struct LoftSection
        {
            public LoftSection(float z, float scaleX, float scaleY, float offsetX, float offsetY)
            {
                Z       = z;
                ScaleX  = scaleX;
                ScaleY  = scaleY;
                OffsetX = offsetX;
                OffsetY = offsetY;
            }

            public float Z { get; }

            public float ScaleX { get; }

            public float ScaleY { get; }

            public float OffsetX { get; }

            public float OffsetY { get; }

            public bool Matches(ShapeProfileSection section)
            {
                return section != null &&
                       Z.Equals(section.Z) &&
                       ScaleX.Equals(section.Scale.X) &&
                       ScaleY.Equals(section.Scale.Y) &&
                       OffsetX.Equals(section.Offset.X) &&
                       OffsetY.Equals(section.Offset.Y);
            }

            public static LoftSection Lerp(LoftSection first, LoftSection second, float time)
            {
                return new(
                    Mathf.Lerp(first.Z, second.Z, time),
                    Mathf.Lerp(first.ScaleX, second.ScaleX, time),
                    Mathf.Lerp(first.ScaleY, second.ScaleY, time),
                    Mathf.Lerp(first.OffsetX, second.OffsetX, time),
                    Mathf.Lerp(first.OffsetY, second.OffsetY, time));
            }
        }
    }
}
