namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Provides a compact Japanese town environment assembled from reusable Low Poly shapes.
    /// </summary>
    public static class LowPolyJapaneseTownPreset
    {
        /// <summary>Gets the style identifier used by the Japanese town preset.</summary>
        public const string StyleId = "lowpoly/japanese-town";

        /// <summary>Creates the engine-agnostic Japanese town definition.</summary>
        public static ShapeDefinition CreateDefinition()
        {
            return ShapeBuilder
                .Create("Low Poly Japanese Town")
                .WithStyle(StyleId)
                .Root("town", "Japanese Town", town =>
                {
                    AddGround(town);
                    AddMachiya(town, "ramen", "Ramen Shop", -2.5f, 1.15f, "fabric.red");
                    AddMachiya(town, "tea", "Tea House", 2.5f, 1.15f, "fabric.indigo");
                    AddTorii(town);
                    AddMarketStall(town);
                    AddBench(town);
                    AddLantern(town, "left-front", -1.35f, -1.65f);
                    AddLantern(town, "right-front", 1.35f, -1.65f);
                    AddLantern(town, "left-rear", -1.35f, 1.7f);
                    AddLantern(town, "right-rear", 1.35f, 1.7f);
                    AddCherryTree(town);
                    AddStreetDetails(town);
                })
                .Build();
        }

        /// <summary>Creates the engine-agnostic palette used by the Japanese town preset.</summary>
        public static ShapeStyleDefinition CreateStyle()
        {
            ShapeStyleDefinition style = new(StyleId);
            style.Palette
                .Set("ground", new(0.22f, 0.28f, 0.2f))
                .Set("stone", new(0.34f, 0.37f, 0.38f))
                .Set("stone.dark", new(0.19f, 0.22f, 0.23f))
                .Set("plaster", new(0.82f, 0.76f, 0.62f))
                .Set("timber", new(0.22f, 0.1f, 0.045f))
                .Set("roof", new(0.08f, 0.12f, 0.15f))
                .Set("paper", new(0.92f, 0.83f, 0.65f))
                .Set("shrine.red", new(0.72f, 0.075f, 0.045f))
                .Set("lantern", new(1f, 0.42f, 0.08f))
                .Set("fabric.red", new(0.62f, 0.08f, 0.06f))
                .Set("fabric.indigo", new(0.08f, 0.18f, 0.38f))
                .Set("foliage", new(0.16f, 0.42f, 0.18f))
                .Set("blossom", new(0.95f, 0.48f, 0.62f));
            return style;
        }

        private static void AddGround(ShapeNodeBuilder town)
        {
            town
                .Shape("town.ground", "Grass Courtyard", LowPolyShapeTypes.Cube, ground => ground
                    .Position(0f, -0.12f, 0f)
                    .Scale(8.8f, 0.2f, 7.4f)
                    .ColorRole("ground"))
                .Shape("town.road", "Stone Street", LowPolyShapeTypes.Cube, road => road
                    .Position(0f, 0.01f, -0.15f)
                    .Scale(2.35f, 0.08f, 7.1f)
                    .ColorRole("stone"));

            for (int index = 0; index < 7; index++)
            {
                float z = -2.75f + (index * 0.9f);
                town.Shape($"town.road.slab.{index}", $"Road Slab {index + 1}", LowPolyShapeTypes.Cube, slab => slab
                    .Position(0f, 0.065f, z)
                    .Scale(1.9f, 0.035f, 0.7f)
                    .ColorRole("paper"));
            }
        }

        private static void AddMachiya(
            ShapeNodeBuilder town,
            string           id,
            string           label,
            float            x,
            float            z,
            string           fabricRole)
        {
            string prefix = $"town.building.{id}";
            town.Group(prefix, label, building => building
                .Position(x, 0f, z)
                .Shape($"{prefix}.body", "Plaster Walls", LowPolyShapeTypes.Cube, body => body
                    .Position(0f, 1.05f, 0f)
                    .Scale(2.35f, 2.1f, 1.65f)
                    .ColorRole("plaster"))
                .Shape($"{prefix}.beam.left", "Left Timber", LowPolyShapeTypes.Cube, beam => beam
                    .Position(-0.98f, 1.08f, -0.86f)
                    .Scale(0.14f, 2.2f, 0.12f)
                    .ColorRole("timber"))
                .Shape($"{prefix}.beam.right", "Right Timber", LowPolyShapeTypes.Cube, beam => beam
                    .Position(0.98f, 1.08f, -0.86f)
                    .Scale(0.14f, 2.2f, 0.12f)
                    .ColorRole("timber"))
                .Shape($"{prefix}.beam.top", "Top Timber", LowPolyShapeTypes.Cube, beam => beam
                    .Position(0f, 1.92f, -0.86f)
                    .Scale(2.1f, 0.14f, 0.12f)
                    .ColorRole("timber"))
                .Shape($"{prefix}.door", "Sliding Door", LowPolyShapeTypes.Cube, door => door
                    .Position(0f, 0.76f, -0.88f)
                    .Scale(0.78f, 1.42f, 0.08f)
                    .ColorRole("timber"))
                .Shape($"{prefix}.door.paper", "Door Paper", LowPolyShapeTypes.Cube, paper => paper
                    .Position(0f, 0.8f, -0.94f)
                    .Scale(0.58f, 1.1f, 0.025f)
                    .ColorRole("paper"))
                .Shape($"{prefix}.window.left", "Left Window", LowPolyShapeTypes.Cube, window => window
                    .Position(-0.7f, 1.18f, -0.93f)
                    .Scale(0.38f, 0.58f, 0.04f)
                    .ColorRole("paper"))
                .Shape($"{prefix}.window.right", "Right Window", LowPolyShapeTypes.Cube, window => window
                    .Position(0.7f, 1.18f, -0.93f)
                    .Scale(0.38f, 0.58f, 0.04f)
                    .ColorRole("paper"))
                .Shape($"{prefix}.window.rail.horizontal", "Window Horizontal Lattice", LowPolyShapeTypes.Cube, rail => rail
                    .Position(0f, 1.18f, -0.965f)
                    .Scale(1.72f, 0.055f, 0.035f)
                    .ColorRole("timber"))
                .Shape($"{prefix}.window.rail.left", "Left Window Lattice", LowPolyShapeTypes.Cube, rail => rail
                    .Position(-0.7f, 1.18f, -0.97f)
                    .Scale(0.045f, 0.62f, 0.035f)
                    .ColorRole("timber"))
                .Shape($"{prefix}.window.rail.right", "Right Window Lattice", LowPolyShapeTypes.Cube, rail => rail
                    .Position(0.7f, 1.18f, -0.97f)
                    .Scale(0.045f, 0.62f, 0.035f)
                    .ColorRole("timber"))
                .Shape($"{prefix}.sign", "Hanging Shop Sign", LowPolyShapeTypes.ExtrudedProfile, sign => sign
                    .Position(0.68f, 1.72f, -1.16f)
                    .Scale(0.42f, 0.58f, 1f)
                    .ExtrudedProfile(0.055f, 0.018f,
                        new(-0.42f, 0.5f), new(0.42f, 0.5f), new(0.5f, 0.4f),
                        new(0.5f, -0.4f), new(0.42f, -0.5f), new(-0.42f, -0.5f),
                        new(-0.5f, -0.4f), new(-0.5f, 0.4f))
                    .ColorRole(fabricRole))
                .Shape($"{prefix}.awning", "Shop Awning", LowPolyShapeTypes.Cube, awning => awning
                    .Position(0f, 1.72f, -1.05f)
                    .Rotation(-12f, 0f, 0f)
                    .Scale(2.15f, 0.09f, 0.52f)
                    .ColorRole(fabricRole))
                .Group($"{prefix}.roof", "Tiled Roof", roof => roof
                    .Position(0f, 2.18f, 0f)
                    .Shape($"{prefix}.roof.front", "Front Roof Slope", LowPolyShapeTypes.Cube, panel => panel
                        .Position(0f, 0f, -0.42f)
                        .Rotation(-18f, 0f, 0f)
                        .Scale(2.75f, 0.15f, 1.25f)
                        .ColorRole("roof"))
                    .Shape($"{prefix}.roof.back", "Back Roof Slope", LowPolyShapeTypes.Cube, panel => panel
                        .Position(0f, 0f, 0.42f)
                        .Rotation(18f, 0f, 0f)
                        .Scale(2.75f, 0.15f, 1.25f)
                        .ColorRole("roof"))
                    .Shape($"{prefix}.roof.ridge", "Ceramic Ridge", LowPolyShapeTypes.Cylinder, ridge => ridge
                        .Position(0f, 0.34f, 0f)
                        .Rotation(0f, 0f, 90f)
                        .Scale(0.12f, 1.42f, 0.12f)
                        .ColorRole("stone.dark"))
                    .Shape($"{prefix}.roof.eave.front", "Front Eave", LowPolyShapeTypes.Cylinder, eave => eave
                        .Position(0f, -0.15f, -1.02f)
                        .Rotation(0f, 0f, 90f)
                        .Scale(0.1f, 1.42f, 0.1f)
                        .ColorRole("stone.dark"))
                    .Shape($"{prefix}.roof.eave.back", "Back Eave", LowPolyShapeTypes.Cylinder, eave => eave
                        .Position(0f, -0.15f, 1.02f)
                        .Rotation(0f, 0f, 90f)
                        .Scale(0.1f, 1.42f, 0.1f)
                        .ColorRole("stone.dark"))));
        }

        private static void AddTorii(ShapeNodeBuilder town)
        {
            town.Group("town.torii", "Shrine Torii", torii => torii
                .Position(0f, 0f, 3.05f)
                .Shape("town.torii.post.left", "Left Post", LowPolyShapeTypes.Cylinder, post => post
                    .Position(-0.85f, 1.25f, 0f).Scale(0.16f, 1.25f, 0.16f).ColorRole("shrine.red"))
                .Shape("town.torii.post.right", "Right Post", LowPolyShapeTypes.Cylinder, post => post
                    .Position(0.85f, 1.25f, 0f).Scale(0.16f, 1.25f, 0.16f).ColorRole("shrine.red"))
                .Shape("town.torii.crossbar.lower", "Lower Crossbar", LowPolyShapeTypes.Cube, bar => bar
                    .Position(0f, 1.88f, 0f).Scale(1.9f, 0.16f, 0.2f).ColorRole("shrine.red"))
                .Shape("town.torii.crossbar.upper", "Upper Crossbar", LowPolyShapeTypes.Cube, bar => bar
                    .Position(0f, 2.28f, 0f).Scale(2.35f, 0.2f, 0.24f).ColorRole("shrine.red"))
                .Shape("town.torii.cap", "Curved Cap", LowPolyShapeTypes.Cube, cap => cap
                    .Position(0f, 2.48f, 0f).Scale(2.65f, 0.12f, 0.28f).ColorRole("roof")));
        }

        private static void AddMarketStall(ShapeNodeBuilder town)
        {
            town.Group("town.stall", "Street Food Stall", stall => stall
                .Position(2.45f, 0f, -1.75f)
                .Shape("town.stall.counter", "Counter", LowPolyShapeTypes.Cube, counter => counter
                    .Position(0f, 0.82f, 0f).Scale(1.7f, 0.16f, 0.72f).ColorRole("timber"))
                .Shape("town.stall.base", "Stall Base", LowPolyShapeTypes.Cube, body => body
                    .Position(0f, 0.42f, 0.15f).Scale(1.45f, 0.7f, 0.58f).ColorRole("plaster"))
                .Shape("town.stall.post.left", "Left Stall Post", LowPolyShapeTypes.Cube, post => post
                    .Position(-0.72f, 1.35f, 0.15f).Scale(0.09f, 1.35f, 0.09f).ColorRole("timber"))
                .Shape("town.stall.post.right", "Right Stall Post", LowPolyShapeTypes.Cube, post => post
                    .Position(0.72f, 1.35f, 0.15f).Scale(0.09f, 1.35f, 0.09f).ColorRole("timber"))
                .Shape("town.stall.canopy", "Indigo Canopy", LowPolyShapeTypes.Cube, canopy => canopy
                    .Position(0f, 1.95f, 0f).Rotation(-8f, 0f, 0f).Scale(1.9f, 0.1f, 1.05f).ColorRole("fabric.indigo"))
                .Shape("town.stall.crate.left", "Left Produce Crate", LowPolyShapeTypes.Cube, crate => crate
                    .Position(-0.42f, 0.98f, -0.08f).Scale(0.42f, 0.28f, 0.38f).ColorRole("timber"))
                .Shape("town.stall.crate.right", "Right Produce Crate", LowPolyShapeTypes.Cube, crate => crate
                    .Position(0.42f, 0.98f, -0.08f).Scale(0.42f, 0.28f, 0.38f).ColorRole("timber")));
        }

        private static void AddBench(ShapeNodeBuilder town)
        {
            town.Group("town.bench", "Street Bench", bench => bench
                .Position(-2.4f, 0f, -1.8f)
                .Shape("town.bench.seat", "Bench Seat", LowPolyShapeTypes.Cube, seat => seat
                    .Position(0f, 0.48f, 0f).Scale(1.45f, 0.16f, 0.48f).ColorRole("timber"))
                .Shape("town.bench.back", "Bench Back", LowPolyShapeTypes.Cube, back => back
                    .Position(0f, 0.85f, 0.2f).Rotation(-8f, 0f, 0f).Scale(1.45f, 0.6f, 0.12f).ColorRole("timber"))
                .Shape("town.bench.leg.left", "Left Bench Leg", LowPolyShapeTypes.Cube, leg => leg
                    .Position(-0.52f, 0.22f, 0f).Scale(0.14f, 0.48f, 0.36f).ColorRole("roof"))
                .Shape("town.bench.leg.right", "Right Bench Leg", LowPolyShapeTypes.Cube, leg => leg
                    .Position(0.52f, 0.22f, 0f).Scale(0.14f, 0.48f, 0.36f).ColorRole("roof")));
        }

        private static void AddLantern(ShapeNodeBuilder town, string id, float x, float z)
        {
            string prefix = $"town.lantern.{id}";
            town.Group(prefix, $"Lantern {id}", lantern => lantern
                .Position(x, 0f, z)
                .Shape($"{prefix}.post", "Lantern Post", LowPolyShapeTypes.Cylinder, post => post
                    .Position(0f, 0.68f, 0f).Scale(0.07f, 0.68f, 0.07f).ColorRole("roof"))
                .Shape($"{prefix}.light", "Paper Lantern", LowPolyShapeTypes.LatheProfile, light => light
                    .Position(0f, 1.42f, 0f)
                    .LatheProfile(12, true,
                        new(0.12f, -0.34f), new(0.23f, -0.26f), new(0.26f, 0f),
                        new(0.23f, 0.26f), new(0.12f, 0.34f))
                    .ProfileSmoothing(1)
                    .ColorRole("lantern"))
                .Shape($"{prefix}.cap", "Lantern Cap", LowPolyShapeTypes.Cylinder, cap => cap
                    .Position(0f, 1.76f, 0f).Scale(0.18f, 0.06f, 0.18f).ColorRole("roof")));
        }

        private static void AddCherryTree(ShapeNodeBuilder town)
        {
            town.Group("town.tree.cherry", "Cherry Tree", tree => tree
                .Position(-3.65f, 0f, 2.25f)
                .Shape("town.tree.cherry.trunk", "Cherry Trunk", LowPolyShapeTypes.Cylinder, trunk => trunk
                    .Position(0f, 0.95f, 0f).Rotation(0f, 0f, -5f).Scale(0.22f, 0.95f, 0.22f).ColorRole("timber"))
                .Shape("town.tree.cherry.branch.left", "Left Branch", LowPolyShapeTypes.Cylinder, branch => branch
                    .Position(-0.28f, 1.55f, 0f).Rotation(0f, 0f, 42f).Scale(0.1f, 0.58f, 0.1f).ColorRole("timber"))
                .Shape("town.tree.cherry.branch.right", "Right Branch", LowPolyShapeTypes.Cylinder, branch => branch
                    .Position(0.3f, 1.62f, 0.05f).Rotation(0f, 0f, -38f).Scale(0.1f, 0.62f, 0.1f).ColorRole("timber"))
                .Shape("town.tree.cherry.crown.center", "Center Blossoms", LowPolyShapeTypes.Sphere, crown => crown
                    .Position(0f, 2.18f, 0f).Scale(1.12f, 0.82f, 0.95f).ColorRole("blossom"))
                .Shape("town.tree.cherry.crown.left", "Left Blossoms", LowPolyShapeTypes.Sphere, crown => crown
                    .Position(-0.72f, 1.95f, 0.05f).Scale(0.75f, 0.62f, 0.72f).ColorRole("blossom"))
                .Shape("town.tree.cherry.crown.right", "Right Blossoms", LowPolyShapeTypes.Sphere, crown => crown
                    .Position(0.72f, 2.02f, -0.05f).Scale(0.78f, 0.66f, 0.74f).ColorRole("blossom")));
        }

        private static void AddStreetDetails(ShapeNodeBuilder town)
        {
            town.Group("town.street.details", "Street Details", details =>
            {
                details
                    .Shape("town.drain.left", "Left Stone Drain", LowPolyShapeTypes.Cube, drain => drain
                        .Position(-1.28f, 0.05f, -0.15f)
                        .Scale(0.16f, 0.08f, 7.1f)
                        .ColorRole("stone.dark"))
                    .Shape("town.drain.right", "Right Stone Drain", LowPolyShapeTypes.Cube, drain => drain
                        .Position(1.28f, 0.05f, -0.15f)
                        .Scale(0.16f, 0.08f, 7.1f)
                        .ColorRole("stone.dark"))
                    .Shape("town.utility.pole", "Wood Utility Pole", LowPolyShapeTypes.Cylinder, pole => pole
                        .Position(3.75f, 1.85f, -0.2f)
                        .Scale(0.11f, 1.85f, 0.11f)
                        .ColorRole("timber"))
                    .Shape("town.utility.crossbar", "Utility Crossbar", LowPolyShapeTypes.Cube, bar => bar
                        .Position(3.75f, 3.32f, -0.2f)
                        .Scale(0.95f, 0.09f, 0.09f)
                        .ColorRole("timber"))
                    .Shape("town.utility.insulator.left", "Left Ceramic Insulator", LowPolyShapeTypes.Cylinder, item => item
                        .Position(3.47f, 3.44f, -0.2f)
                        .Scale(0.055f, 0.13f, 0.055f)
                        .ColorRole("paper")
                        .Mirror(ShapeMirrorAxis.X));

                for (int index = 0; index < 5; index++)
                {
                    float z = -2.2f + (index * 1.05f);
                    details.Shape($"town.drain.grate.{index}", $"Drain Grate {index + 1}", LowPolyShapeTypes.Cube, grate => grate
                        .Position(-1.28f, 0.105f, z)
                        .Scale(0.28f, 0.025f, 0.48f)
                        .ColorRole("roof")
                        .Mirror(ShapeMirrorAxis.X));
                }
            });
        }
    }
}
