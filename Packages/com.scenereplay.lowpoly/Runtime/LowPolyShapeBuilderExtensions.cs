using System;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Adds concise Low Poly procedural-shape configuration to the engine-neutral builder.
    /// </summary>
    public static class LowPolyShapeBuilderExtensions
    {
        /// <summary>Configures the top and bottom dimensions of a frustum node.</summary>
        public static ShapeNodeBuilder Frustum(
            this ShapeNodeBuilder builder,
            float                 topWidth,
            float                 topDepth,
            float                 bottomWidth,
            float                 bottomDepth)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder
                .Parameter(LowPolyShapeParameters.TopWidth, topWidth)
                .Parameter(LowPolyShapeParameters.TopDepth, topDepth)
                .Parameter(LowPolyShapeParameters.BottomWidth, bottomWidth)
                .Parameter(LowPolyShapeParameters.BottomDepth, bottomDepth);
        }

        /// <summary>Configures a normalized outline and depth for an extruded-profile node.</summary>
        public static ShapeNodeBuilder ExtrudedProfile(
            this ShapeNodeBuilder builder,
            float                 depth,
            params ForgeVector2[] points)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder
                .Profile(points)
                .Parameter(LowPolyShapeParameters.ProfileDepth, depth);
        }

        /// <summary>Configures a normalized outline, depth, and single-segment edge bevel.</summary>
        public static ShapeNodeBuilder ExtrudedProfile(
            this ShapeNodeBuilder builder,
            float                 depth,
            float                 bevel,
            params ForgeVector2[] points)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder
                .Profile(points)
                .Parameter(LowPolyShapeParameters.ProfileDepth, depth)
                .Parameter(LowPolyShapeParameters.ProfileBevel, bevel);
        }

        /// <summary>Configures the curved ring count of an extruded profile bevel.</summary>
        public static ShapeNodeBuilder BevelSegments(this ShapeNodeBuilder builder, int bevelSegments)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (bevelSegments < 1 || bevelSegments > 8)
                throw new ArgumentOutOfRangeException(nameof(bevelSegments));

            return builder.Parameter(LowPolyShapeParameters.ProfileBevelSegments, bevelSegments);
        }

        /// <summary>Configures an outline and ordered depth sections for a profile-loft node.</summary>
        public static ShapeNodeBuilder ProfileLoft(
            this ShapeNodeBuilder        builder,
            ForgeVector2[]               profile,
            params ShapeProfileSection[] sections)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (sections == null)
                throw new ArgumentNullException(nameof(sections));

            builder.Profile(profile);
            foreach (ShapeProfileSection section in sections)
            {
                if (section == null)
                    throw new ArgumentException("Profile loft sections cannot contain null entries.", nameof(sections));

                builder.ProfileSection(
                    section.Z,
                    section.Scale.X,
                    section.Scale.Y,
                    section.Offset.X,
                    section.Offset.Y);
            }

            return builder;
        }

        /// <summary>Configures interpolated rings and optional smooth normals for a profile loft.</summary>
        public static ShapeNodeBuilder LoftQuality(
            this ShapeNodeBuilder builder,
            int                   subdivisions,
            bool                  smoothNormals)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (subdivisions < 0 || subdivisions > 8)
                throw new ArgumentOutOfRangeException(nameof(subdivisions));

            return builder
                .Parameter(LowPolyShapeParameters.LoftSubdivisions, subdivisions)
                .Parameter(LowPolyShapeParameters.SmoothNormals, smoothNormals ? 1f : 0f);
        }

        /// <summary>Configures ordered independent profiles for a profile-cage node.</summary>
        public static ShapeNodeBuilder ProfileCage(
            this ShapeNodeBuilder                 builder,
            params ShapeProfileCageSection[] sections)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (sections == null)
                throw new ArgumentNullException(nameof(sections));

            foreach (ShapeProfileCageSection section in sections)
            {
                if (section == null)
                    throw new ArgumentException("Profile cage sections cannot contain null entries.", nameof(sections));

                ForgeVector2[] profile = new ForgeVector2[section.Profile.Count];
                section.Profile.CopyTo(profile, 0);
                builder.ProfileCageSection(section.Z, profile);
            }

            return builder;
        }

        /// <summary>Configures profile smoothing and optional averaged normals for a profile cage.</summary>
        public static ShapeNodeBuilder CageQuality(
            this ShapeNodeBuilder builder,
            int                   profileSmoothing,
            bool                  smoothNormals)
        {
            return CageQuality(builder, 0, profileSmoothing, smoothNormals);
        }

        /// <summary>Configures interpolated rings, profile smoothing, and normals for a profile cage.</summary>
        public static ShapeNodeBuilder CageQuality(
            this ShapeNodeBuilder builder,
            int                   subdivisions,
            int                   profileSmoothing,
            bool                  smoothNormals)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (subdivisions < 0 || subdivisions > 8)
                throw new ArgumentOutOfRangeException(nameof(subdivisions));

            if (profileSmoothing < 0 || profileSmoothing > 4)
                throw new ArgumentOutOfRangeException(nameof(profileSmoothing));

            return builder
                .Parameter(LowPolyShapeParameters.CageSubdivisions, subdivisions)
                .Parameter(LowPolyShapeParameters.ProfileSmoothing, profileSmoothing)
                .Parameter(LowPolyShapeParameters.SmoothNormals, smoothNormals ? 1f : 0f);
        }

        /// <summary>Configures a radius-height profile revolved around the local Y axis.</summary>
        public static ShapeNodeBuilder LatheProfile(
            this ShapeNodeBuilder builder,
            int                   radialSegments,
            bool                  smoothNormals,
            params ForgeVector2[] profile)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (radialSegments < 3 || radialSegments > 64)
                throw new ArgumentOutOfRangeException(nameof(radialSegments));

            return builder
                .Profile(profile)
                .Parameter(LowPolyShapeParameters.RadialSegments, radialSegments)
                .Parameter(LowPolyShapeParameters.SmoothNormals, smoothNormals ? 1f : 0f);
        }

        /// <summary>Applies bounded curve smoothing to the configured profile control points.</summary>
        public static ShapeNodeBuilder ProfileSmoothing(this ShapeNodeBuilder builder, int iterations)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (iterations < 0 || iterations > 4)
                throw new ArgumentOutOfRangeException(nameof(iterations));

            return builder.Parameter(LowPolyShapeParameters.ProfileSmoothing, iterations);
        }

        /// <summary>Configures a closed profile swept along an ordered three-dimensional path.</summary>
        public static ShapeNodeBuilder ProfileSweep(
            this ShapeNodeBuilder builder,
            ForgeVector2[]        profile,
            ForgeVector3[]        path)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Profile(profile).Path(path);
        }

        /// <summary>Configures bounded path smoothing and averaged normals for a profile sweep.</summary>
        public static ShapeNodeBuilder SweepQuality(
            this ShapeNodeBuilder builder,
            int                   pathSmoothing,
            bool                  smoothNormals)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (pathSmoothing < 0 || pathSmoothing > 4)
                throw new ArgumentOutOfRangeException(nameof(pathSmoothing));

            return builder
                .Parameter(LowPolyShapeParameters.PathSmoothing, pathSmoothing)
                .Parameter(LowPolyShapeParameters.SmoothNormals, smoothNormals ? 1f : 0f);
        }
    }
}
