namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Defines shape identifiers implemented by the official Low Poly package.
    /// </summary>
    public static class LowPolyShapeTypes
    {
        /// <summary>
        /// Identifies a unit Low Poly cube scaled by its shape transform.
        /// </summary>
        public const string Cube = "lowpoly/cube";

        /// <summary>
        /// Identifies a unit Low Poly sphere scaled by its shape transform.
        /// </summary>
        public const string Sphere = "lowpoly/sphere";

        /// <summary>
        /// Identifies a unit Low Poly cylinder scaled by its shape transform.
        /// </summary>
        public const string Cylinder = "lowpoly/cylinder";

        /// <summary>
        /// Identifies a unit Low Poly capsule scaled by its shape transform.
        /// </summary>
        public const string Capsule = "lowpoly/capsule";

        /// <summary>
        /// Identifies a unit triangular wedge suitable for sloped silhouettes.
        /// </summary>
        public const string Wedge = "lowpoly/wedge";

        /// <summary>
        /// Identifies a centered tapered box controlled by top and bottom dimensions.
        /// </summary>
        public const string Frustum = "lowpoly/frustum";

        /// <summary>
        /// Identifies a flat-shaded mesh extruded from a normalized two-dimensional outline.
        /// </summary>
        public const string ExtrudedProfile = "lowpoly/extruded-profile";

        /// <summary>
        /// Identifies a volume formed by scaling and offsetting one profile across ordered depth sections.
        /// </summary>
        public const string ProfileLoft = "lowpoly/profile-loft";

        /// <summary>
        /// Identifies a volume joining ordered sections with independently authored profiles.
        /// </summary>
        public const string ProfileCage = "lowpoly/profile-cage";

        /// <summary>
        /// Identifies a volume formed by revolving a radius-height profile around its local Y axis.
        /// </summary>
        public const string LatheProfile = "lowpoly/lathe-profile";

        /// <summary>
        /// Identifies a closed two-dimensional profile swept along a three-dimensional path.
        /// </summary>
        public const string ProfileSweep = "lowpoly/profile-sweep";
    }

    /// <summary>
    /// Defines numeric parameter names supported by official Low Poly procedural shapes.
    /// </summary>
    public static class LowPolyShapeParameters
    {
        /// <summary>Controls a frustum's top width relative to its transform scale.</summary>
        public const string TopWidth = "topWidth";

        /// <summary>Controls a frustum's top depth relative to its transform scale.</summary>
        public const string TopDepth = "topDepth";

        /// <summary>Controls a frustum's bottom width relative to its transform scale.</summary>
        public const string BottomWidth = "bottomWidth";

        /// <summary>Controls a frustum's bottom depth relative to its transform scale.</summary>
        public const string BottomDepth = "bottomDepth";

        /// <summary>Controls an extruded profile's normalized depth.</summary>
        public const string ProfileDepth = "profileDepth";

        /// <summary>Controls the normalized edge inset of an extruded profile.</summary>
        public const string ProfileBevel = "profileBevel";

        /// <summary>Controls the number of curved rings across an extruded profile bevel.</summary>
        public const string ProfileBevelSegments = "profileBevelSegments";

        /// <summary>Controls the number of interpolated rings between authored loft sections.</summary>
        public const string LoftSubdivisions = "loftSubdivisions";

        /// <summary>Controls interpolated rings between independently authored cage sections.</summary>
        public const string CageSubdivisions = "cageSubdivisions";

        /// <summary>Enables averaged vertex normals on a profile loft when greater than zero.</summary>
        public const string SmoothNormals = "smoothNormals";

        /// <summary>Controls the number of radial faces around a lathed profile.</summary>
        public const string RadialSegments = "radialSegments";

        /// <summary>Controls bounded corner-cutting iterations applied to a profile before meshing.</summary>
        public const string ProfileSmoothing = "profileSmoothing";

        /// <summary>Controls bounded corner-cutting iterations applied to a sweep path.</summary>
        public const string PathSmoothing = "pathSmoothing";
    }
}
