namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Provides a dense modern Japanese scramble-crossing environment from reusable Low Poly shapes.
    /// </summary>
    public static class LowPolyShibuyaCrossingPreset
    {
        /// <summary>Gets the style identifier used by the crossing preset.</summary>
        public const string StyleId = "lowpoly/shibuya-crossing";

        /// <summary>Creates the engine-agnostic modern crossing definition.</summary>
        public static ShapeDefinition CreateDefinition()
        {
            return ShapeBuilder
                .Create("Low Poly Shibuya Crossing")
                .WithStyle(StyleId)
                .Root("shibuya", "Shibuya Crossing", city =>
                {
                    AddRoads(city);
                    AddCrosswalks(city);
                    AddBuilding(city, "northwest", "Northwest Media Tower", -6.2f, 6.1f, 7.2f, 10.8f, 3.8f,
                        -45f, "concrete.light", "screen.blue");
                    AddBuilding(city, "northeast", "Northeast Fashion Center", 6.2f, 6.1f, 7.4f, 9.2f, 4.2f,
                        45f, "concrete.warm", "screen.magenta");
                    AddBuilding(city, "southwest", "Southwest Glass Mall", -6.4f, -6.2f, 7f, 8.4f, 4f,
                        -135f, "concrete.dark", "screen.cyan");
                    AddBuilding(city, "southeast", "Southeast Station Block", 6.3f, -6.2f, 7.6f, 7.6f, 4.4f,
                        135f, "concrete.light", "screen.red");
                    AddStreetFurniture(city);
                    AddCrowd(city);
                })
                .Build();
        }

        /// <summary>Creates the modern city palette used by the crossing preset.</summary>
        public static ShapeStyleDefinition CreateStyle()
        {
            ShapeStyleDefinition style = new(StyleId);
            style.Palette
                .Set("asphalt", new(0.075f, 0.085f, 0.1f))
                .Set("sidewalk", new(0.36f, 0.38f, 0.4f))
                .Set("crosswalk", new(0.88f, 0.9f, 0.88f))
                .Set("curb", new(0.68f, 0.69f, 0.67f))
                .Set("concrete.light", new(0.58f, 0.62f, 0.66f))
                .Set("concrete.warm", new(0.58f, 0.5f, 0.43f))
                .Set("concrete.dark", new(0.19f, 0.23f, 0.28f))
                .Set("glass", new(0.08f, 0.2f, 0.3f))
                .Set("window.light", new(0.66f, 0.82f, 0.9f))
                .Set("metal", new(0.12f, 0.14f, 0.16f))
                .Set("signal", new(0.055f, 0.07f, 0.08f))
                .Set("signal.green", new(0.08f, 0.82f, 0.42f))
                .Set("screen.blue", new(0.08f, 0.34f, 0.88f))
                .Set("screen.magenta", new(0.9f, 0.12f, 0.55f))
                .Set("screen.cyan", new(0.05f, 0.72f, 0.82f))
                .Set("screen.red", new(0.88f, 0.12f, 0.08f))
                .Set("accent.yellow", new(0.95f, 0.68f, 0.08f))
                .Set("crowd.dark", new(0.08f, 0.1f, 0.14f))
                .Set("crowd.blue", new(0.12f, 0.28f, 0.62f))
                .Set("crowd.red", new(0.72f, 0.12f, 0.12f))
                .Set("skin", new(0.78f, 0.57f, 0.43f));
            return style;
        }

        private static void AddRoads(ShapeNodeBuilder city)
        {
            city.Shape("shibuya.ground", "City Ground", LowPolyShapeTypes.Cube, ground => ground
                    .Position(0f, -0.22f, 0f).Scale(20f, 0.32f, 20f).ColorRole("sidewalk"))
                .Shape("shibuya.road.east-west", "East West Avenue", LowPolyShapeTypes.Cube, road => road
                    .Position(0f, 0f, 0f).Scale(20f, 0.12f, 6.8f).ColorRole("asphalt"))
                .Shape("shibuya.road.north-south", "North South Avenue", LowPolyShapeTypes.Cube, road => road
                    .Position(0f, 0.01f, 0f).Scale(6.8f, 0.13f, 20f).ColorRole("asphalt"))
                .Shape("shibuya.center", "Crossing Center", LowPolyShapeTypes.Cylinder, center => center
                    .Position(0f, 0.09f, 0f).Scale(3.25f, 0.035f, 3.25f).ColorRole("asphalt"));

            AddSidewalk(city, "northwest", -6.9f, 6.9f);
            AddSidewalk(city, "northeast", 6.9f, 6.9f);
            AddSidewalk(city, "southwest", -6.9f, -6.9f);
            AddSidewalk(city, "southeast", 6.9f, -6.9f);
        }

        private static void AddSidewalk(ShapeNodeBuilder city, string id, float x, float z)
        {
            city.Shape($"shibuya.sidewalk.{id}", $"{id} Sidewalk", LowPolyShapeTypes.Cube, sidewalk => sidewalk
                .Position(x, 0.13f, z)
                .Scale(6.1f, 0.22f, 6.1f)
                .ColorRole("sidewalk"));
        }

        private static void AddCrosswalks(ShapeNodeBuilder city)
        {
            city.Group("shibuya.crosswalks", "Scramble Crosswalks", crossings =>
            {
                for (int index = 0; index < 7; index++)
                {
                    float offset = -2.4f + (index * 0.8f);
                    AddStripe(crossings, $"north.{index}", offset, 4.15f, 0f, 0.46f, 2.5f);
                    AddStripe(crossings, $"south.{index}", offset, -4.15f, 0f, 0.46f, 2.5f);
                    AddStripe(crossings, $"east.{index}", 4.15f, offset, 90f, 0.46f, 2.5f);
                    AddStripe(crossings, $"west.{index}", -4.15f, offset, 90f, 0.46f, 2.5f);
                }

                for (int index = 0; index < 8; index++)
                {
                    float offset = -2.45f + (index * 0.7f);
                    AddStripe(crossings, $"diagonal-ne.{index}", offset, offset, -45f, 0.38f, 5.4f);
                    AddStripe(crossings, $"diagonal-nw.{index}", offset, -offset, 45f, 0.38f, 5.4f);
                }
            });
        }

        private static void AddStripe(
            ShapeNodeBuilder crossings,
            string           id,
            float            x,
            float            z,
            float            rotation,
            float            width,
            float            length)
        {
            crossings.Shape($"shibuya.crosswalk.{id}", $"Crosswalk Stripe {id}", LowPolyShapeTypes.Cube,
                stripe => stripe
                    .Position(x, 0.11f, z)
                    .Rotation(0f, rotation, 0f)
                    .Scale(width, 0.025f, length)
                    .ColorRole("crosswalk"));
        }

        private static void AddBuilding(
            ShapeNodeBuilder city,
            string           id,
            string           label,
            float            x,
            float            z,
            float            width,
            float            height,
            float            depth,
            float            rotation,
            string           wallRole,
            string           screenRole)
        {
            string prefix = $"shibuya.building.{id}";
            city.Group(prefix, label, building =>
            {
                building.Position(x, 0.22f, z).Rotation(0f, rotation, 0f)
                    .Shape($"{prefix}.body", "Commercial Tower", LowPolyShapeTypes.Frustum, body => body
                        .Position(0f, height * 0.5f, 0f)
                        .Scale(width, height, depth)
                        .Frustum(0.9f, 0.94f, 1f, 1f)
                        .ColorRole(wallRole))
                    .Shape($"{prefix}.glass", "Glass Facade", LowPolyShapeTypes.Cube, glass => glass
                        .Position(0f, height * 0.52f, -(depth * 0.51f))
                        .Scale(width * 0.74f, height * 0.72f, 0.08f)
                        .ColorRole("glass"))
                    .Shape($"{prefix}.screen", "Large Digital Screen", LowPolyShapeTypes.Cube, screen => screen
                        .Position(0f, height * 0.67f, -(depth * 0.56f))
                        .Scale(width * 0.62f, height * 0.3f, 0.06f)
                        .ColorRole(screenRole))
                    .Shape($"{prefix}.screen.band", "Screen Graphic Band", LowPolyShapeTypes.Cube, band => band
                        .Position(0f, height * 0.67f, -(depth * 0.59f))
                        .Rotation(0f, 0f, -8f)
                        .Scale(width * 0.56f, height * 0.045f, 0.025f)
                        .ColorRole("window.light"))
                    .Shape($"{prefix}.entrance", "Recessed Entrance", LowPolyShapeTypes.Cube, entrance => entrance
                        .Position(0f, 1.05f, -(depth * 0.54f))
                        .Scale(width * 0.42f, 1.9f, 0.12f)
                        .ColorRole("metal"))
                    .Shape($"{prefix}.roof", "Mechanical Roof", LowPolyShapeTypes.Cube, roof => roof
                        .Position(0f, height + 0.3f, 0f)
                        .Scale(width * 0.42f, 0.55f, depth * 0.48f)
                        .ColorRole("metal"));

                for (int floor = 0; floor < 4; floor++)
                {
                    float y = 2.25f + (floor * ((height - 2.8f) / 4f));
                    building.Shape($"{prefix}.floor.{floor}", $"Facade Floor {floor + 1}", LowPolyShapeTypes.Cube,
                        band => band
                            .Position(0f, y, -(depth * 0.555f))
                            .Scale(width * 0.82f, 0.12f, 0.04f)
                            .ColorRole("window.light"));
                }

                for (int column = 0; column < 5; column++)
                {
                    float windowX = (-0.32f + (column * 0.16f)) * width;
                    building.Shape($"{prefix}.mullion.{column}", $"Facade Mullion {column + 1}",
                        LowPolyShapeTypes.Cube, mullion => mullion
                            .Position(windowX, height * 0.52f, -(depth * 0.575f))
                            .Scale(0.08f, height * 0.7f, 0.035f)
                            .ColorRole("metal"));
                }
            });
        }

        private static void AddStreetFurniture(ShapeNodeBuilder city)
        {
            AddSignal(city, "northwest", -3.65f, 3.65f, 45f);
            AddSignal(city, "northeast", 3.65f, 3.65f, -45f);
            AddSignal(city, "southwest", -3.65f, -3.65f, 135f);
            AddSignal(city, "southeast", 3.65f, -3.65f, -135f);

            city.Shape("shibuya.station.sign", "Station Entrance Sign", LowPolyShapeTypes.Cube, sign => sign
                    .Position(4.45f, 1.25f, -3.95f).Rotation(0f, -45f, 0f)
                    .Scale(1.45f, 0.58f, 0.12f).ColorRole("accent.yellow"))
                .Shape("shibuya.street.clock", "Street Clock", LowPolyShapeTypes.Cylinder, clock => clock
                    .Position(-3.8f, 2.55f, -3.9f).Rotation(90f, 0f, 0f)
                    .Scale(0.48f, 0.1f, 0.48f).ColorRole("window.light"));
        }

        private static void AddSignal(ShapeNodeBuilder city, string id, float x, float z, float rotation)
        {
            string prefix = $"shibuya.signal.{id}";
            city.Group(prefix, $"{id} Traffic Signal", signal => signal
                .Position(x, 0.24f, z)
                .Rotation(0f, rotation, 0f)
                .Shape($"{prefix}.post", "Signal Post", LowPolyShapeTypes.Cylinder, post => post
                    .Position(0f, 1.65f, 0f).Scale(0.09f, 1.65f, 0.09f).ColorRole("metal"))
                .Shape($"{prefix}.arm", "Signal Arm", LowPolyShapeTypes.Cube, arm => arm
                    .Position(0.72f, 2.95f, 0f).Scale(1.45f, 0.1f, 0.1f).ColorRole("metal"))
                .Shape($"{prefix}.box", "Pedestrian Signal", LowPolyShapeTypes.Cube, box => box
                    .Position(1.38f, 2.72f, 0f).Scale(0.42f, 0.62f, 0.22f).ColorRole("signal"))
                .Shape($"{prefix}.light", "Walk Signal", LowPolyShapeTypes.Sphere, light => light
                    .Position(1.38f, 2.72f, -0.13f).Scale(0.12f, 0.18f, 0.04f).ColorRole("signal.green")));
        }

        private static void AddCrowd(ShapeNodeBuilder city)
        {
            city.Group("shibuya.crowd", "Crossing Crowd", crowd =>
            {
                for (int index = 0; index < 16; index++)
                {
                    int   lane = index % 4;
                    float x    = -2.8f + (lane * 1.75f);
                    float z    = -2.5f + ((index / 4) * 1.65f);
                    float yaw  = index % 2 == 0 ? -38f : 42f;
                    AddPedestrian(crowd, index, x, z, yaw);
                }
            });
        }

        private static void AddPedestrian(
            ShapeNodeBuilder crowd,
            int              index,
            float            x,
            float            z,
            float            yaw)
        {
            string prefix   = $"shibuya.crowd.{index}";
            string bodyRole = index % 3 == 0 ? "crowd.red" : index % 3 == 1 ? "crowd.blue" : "crowd.dark";
            crowd.Group(prefix, $"Pedestrian {index + 1}", person => person
                .Position(x, 0.16f, z)
                .Rotation(0f, yaw, 0f)
                .Shape($"{prefix}.body", "Coat", LowPolyShapeTypes.Capsule, body => body
                    .Position(0f, 0.82f, 0f).Scale(0.24f, 0.55f, 0.2f).ColorRole(bodyRole))
                .Shape($"{prefix}.head", "Head", LowPolyShapeTypes.Sphere, head => head
                    .Position(0f, 1.48f, 0f).Scale(0.19f, 0.21f, 0.18f).ColorRole("skin")));
        }
    }
}
