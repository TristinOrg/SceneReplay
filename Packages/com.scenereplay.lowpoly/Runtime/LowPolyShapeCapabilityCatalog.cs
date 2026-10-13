using System;
using System.Collections.Generic;

namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Describes the authoring capabilities of the official Low Poly geometry backend.
    /// </summary>
    public sealed class LowPolyShapeCapabilityCatalog : IShapeCapabilityCatalog
    {
        private static readonly ShapeCapability[]                        CapabilityArray =
        {
            Basic(LowPolyShapeTypes.Cube, "A unit box.",
                "Rigid block forms, beams, panels, furniture, walls, and layered mechanical parts.",
                "Straight edges and a rectangular silhouette only."),
            Basic(LowPolyShapeTypes.Sphere, "A faceted unit sphere.",
                "Rounded joints, eyes, foliage clusters, lamps, and soft massing.",
                "Cannot express a controlled silhouette beyond transform scaling."),
            Basic(LowPolyShapeTypes.Cylinder, "A vertical unit cylinder.",
                "Posts, pipes, wheels, handles, trunks, and circular mechanical parts.",
                "A constant circular section with no authored taper."),
            Basic(LowPolyShapeTypes.Capsule, "A vertical rounded capsule.",
                "Simple limbs, padded forms, handles, and rounded connectors.",
                "A symmetric constant-radius body; avoid it for tailored anatomy or armor."),
            Basic(LowPolyShapeTypes.Wedge, "A unit triangular prism.",
                "Roof slopes, ramps, fins, simple hair spikes, and directional accents.",
                "Only one fixed triangular-prism topology."),
            new(
                LowPolyShapeTypes.Frustum,
                "A centered box volume with independent top and bottom dimensions.",
                "Tapered torsos, limbs, pedestals, roofs, containers, and architectural masses.",
                "Only linear taper between rectangular ends.",
                ShapeGenerationCost.Parameterized,
                0, 0, 0,
                Positive(LowPolyShapeParameters.TopWidth, "Top width relative to transform scale.", 0.65f),
                Positive(LowPolyShapeParameters.TopDepth, "Top depth relative to transform scale.", 0.65f),
                Positive(LowPolyShapeParameters.BottomWidth, "Bottom width relative to transform scale.", 1f),
                Positive(LowPolyShapeParameters.BottomDepth, "Bottom depth relative to transform scale.", 1f)),
            new(
                LowPolyShapeTypes.ExtrudedProfile,
                "A closed two-dimensional silhouette extruded through local depth.",
                "Clothing panels, signs, blades, furniture parts, facade details, and custom silhouettes.",
                "The profile must be a simple non-self-intersecting polygon; depth is constant.",
                ShapeGenerationCost.InputScaled,
                3, 0, 0,
                Positive(LowPolyShapeParameters.ProfileDepth, "Extrusion depth.", 0.2f),
                NonNegative(LowPolyShapeParameters.ProfileBevel, "Edge inset; excessive values collapse narrow profiles.", 0f),
                Integer(LowPolyShapeParameters.ProfileBevelSegments, "Curved rings across the bevel.", 1, 1, 8),
                Integer(LowPolyShapeParameters.ProfileSmoothing, "Corner-cutting profile smoothing iterations.", 0, 0, 4)),
            new(
                LowPolyShapeTypes.ProfileLoft,
                "A closed profile scaled and offset across ordered depth sections.",
                "Rounded or tapered heads, armor, clothing volumes, vehicle shells, and designed furniture.",
                "All sections share one profile topology and must be ordered by increasing depth.",
                ShapeGenerationCost.InputScaled,
                3, 0, 2,
                Integer(LowPolyShapeParameters.LoftSubdivisions, "Interpolated rings between authored sections.", 0, 0, 8),
                Toggle(LowPolyShapeParameters.SmoothNormals, "Enables averaged normals."),
                Integer(LowPolyShapeParameters.ProfileSmoothing, "Corner-cutting profile smoothing iterations.", 0, 0, 4)),
            new(
                LowPolyShapeTypes.ProfileCage,
                "A volume joining ordered depth sections with independently authored closed profiles.",
                "Asymmetric hair shells, tailored clothing, footwear, vehicle bodies, furniture, and organic props.",
                "Every section must have the same point count and matching point correspondence.",
                ShapeGenerationCost.InputScaled,
                0, 0, 0, 2,
                Integer(LowPolyShapeParameters.CageSubdivisions, "Interpolated rings between authored sections.", 0, 0, 8),
                Toggle(LowPolyShapeParameters.SmoothNormals, "Enables averaged normals."),
                Integer(LowPolyShapeParameters.ProfileSmoothing, "Per-section corner smoothing iterations.", 0, 0, 4)),
            new(
                LowPolyShapeTypes.LatheProfile,
                "A radius-height profile revolved around local Y.",
                "Heads, limbs, vessels, knobs, columns, wheels, and other rotational forms.",
                "Only rotationally symmetric geometry; radii must be non-negative and heights strictly increase.",
                ShapeGenerationCost.InputScaled,
                2, 0, 0,
                Integer(LowPolyShapeParameters.RadialSegments, "Faces around the axis.", 12, 3, 64),
                Toggle(LowPolyShapeParameters.SmoothNormals, "Enables averaged normals."),
                Integer(LowPolyShapeParameters.ProfileSmoothing, "Profile smoothing iterations.", 0, 0, 4)),
            new(
                LowPolyShapeTypes.ProfileSweep,
                "A closed profile transported along an ordered three-dimensional path.",
                "Cables, curved horns, rails, handles, branches, trims, and bent structural members.",
                "The path cannot repeat consecutive points or reverse directly; smoothing multiplies mesh density.",
                ShapeGenerationCost.InputScaled,
                3, 2, 0,
                Integer(LowPolyShapeParameters.ProfileSmoothing, "Profile smoothing iterations.", 0, 0, 4),
                Integer(LowPolyShapeParameters.PathSmoothing, "Path smoothing iterations.", 0, 0, 4),
                Toggle(LowPolyShapeParameters.SmoothNormals, "Enables averaged normals."))
        };
        private static readonly IReadOnlyList<ShapeCapability>             CapabilityList =
            Array.AsReadOnly(CapabilityArray);
        private static readonly Dictionary<string, ShapeCapability>        CapabilityMap  = CreateMap();

        /// <summary>Gets the shared immutable Low Poly capability catalog.</summary>
        public static LowPolyShapeCapabilityCatalog Instance { get; } = new();

        /// <inheritdoc />
        public IReadOnlyList<ShapeCapability> Shapes => CapabilityList;

        /// <summary>Creates a versioned document suitable for external tools and LLM context.</summary>
        public ShapeCapabilityCatalogDocument CreateDocument()
        {
            return new("lowpoly/official", CapabilityList);
        }

        /// <inheritdoc />
        public bool TryGet(string type, out ShapeCapability capability)
        {
            if (type == null)
            {
                capability = null;
                return false;
            }

            return CapabilityMap.TryGetValue(type, out capability);
        }

        private static ShapeCapability Basic(
            string type,
            string summary,
            string bestFor,
            string limitations)
        {
            return new(type, summary, bestFor, limitations, ShapeGenerationCost.Constant);
        }

        private static ShapeParameterCapability Positive(string name, string summary, float defaultValue)
        {
            return new(name, summary, defaultValue, 0f, minimumExclusive: true);
        }

        private static ShapeParameterCapability NonNegative(string name, string summary, float defaultValue)
        {
            return new(name, summary, defaultValue, 0f);
        }

        private static ShapeParameterCapability Integer(
            string name,
            string summary,
            int    defaultValue,
            int    minimum,
            int    maximum)
        {
            return new(name, summary, defaultValue, minimum, maximum, true);
        }

        private static ShapeParameterCapability Toggle(string name, string summary)
        {
            return new(name, summary, 0f, 0f, 1f, true);
        }

        private static Dictionary<string, ShapeCapability> CreateMap()
        {
            Dictionary<string, ShapeCapability> map = new(StringComparer.Ordinal);
            foreach (ShapeCapability capability in CapabilityArray)
                map.Add(capability.Type, capability);

            return map;
        }
    }
}
