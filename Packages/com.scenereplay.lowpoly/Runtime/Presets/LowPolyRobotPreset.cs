namespace SceneReplay.LowPoly
{
    /// <summary>
    /// Provides a detailed modular robot preset with transform-ready articulated pivots.
    /// </summary>
    public static class LowPolyRobotPreset
    {
        /// <summary>
        /// Gets the style identifier used by the robot preset.
        /// </summary>
        public const string StyleId = "lowpoly/robot";

        /// <summary>
        /// Creates the engine-agnostic robot model definition.
        /// </summary>
        public static ShapeDefinition CreateDefinition()
        {
            return ShapeBuilder
                .Create("Low Poly Sentinel Robot")
                .WithStyle(StyleId)
                .WithRig("humanoid/basic",
                    new ShapeRigJoint(ShapeRigRoles.Root, "robot"),
                    Joint(ShapeRigRoles.Head, "robot.head.pivot", -30f, 30f, -75f, 75f, -25f, 25f),
                    Joint(ShapeRigRoles.LeftShoulder, "robot.arm.left.shoulder.pivot",
                        -80f, 80f, -40f, 40f, -20f, 105f),
                    Joint(ShapeRigRoles.LeftElbow, "robot.arm.left.elbow.pivot", -5f, 125f, 0f, 0f, 0f, 0f),
                    Joint(ShapeRigRoles.RightShoulder, "robot.arm.right.shoulder.pivot",
                        -80f, 80f, -40f, 40f, -105f, 20f),
                    Joint(ShapeRigRoles.RightElbow, "robot.arm.right.elbow.pivot", -5f, 125f, 0f, 0f, 0f, 0f),
                    Joint(ShapeRigRoles.LeftHip, "robot.leg.left.hip.pivot", -50f, 50f, -20f, 20f, -15f, 15f),
                    Joint(ShapeRigRoles.LeftKnee, "robot.leg.left.knee.pivot", -125f, 5f, 0f, 0f, 0f, 0f),
                    Joint(ShapeRigRoles.RightHip, "robot.leg.right.hip.pivot", -50f, 50f, -20f, 20f, -15f, 15f),
                    Joint(ShapeRigRoles.RightKnee, "robot.leg.right.knee.pivot", -125f, 5f, 0f, 0f, 0f, 0f))
                .Root("robot", "Sentinel Robot", robot =>
                {
                    robot
                        .Shape("robot.body", "Armored Torso", LowPolyShapeTypes.ProfileLoft, body => body
                            .Position(0f, 1.75f, 0f)
                            .Scale(1.05f, 1.1f, 0.58f)
                            .ProfileLoft(
                                ArmorProfile(),
                                Section(-0.5f, 0.78f, 0.88f),
                                Section(0.08f, 1f, 1f),
                                Section(0.5f, 0.86f, 0.92f))
                            .LoftQuality(2, true)
                            .ColorRole("metal.primary"))
                        .Shape("robot.chest", "Chest Plate", LowPolyShapeTypes.ExtrudedProfile, chest => chest
                            .Position(0f, 1.86f, 0.34f)
                            .Scale(0.78f, 0.56f, 1f)
                            .ExtrudedProfile(0.1f, 0.025f,
                                new(-0.5f, 0.22f), new(-0.32f, 0.5f), new(0.32f, 0.5f),
                                new(0.5f, 0.22f), new(0.4f, -0.5f), new(-0.4f, -0.5f))
                            .ColorRole("accent"))
                        .Shape("robot.reactor", "Chest Reactor", LowPolyShapeTypes.Sphere, reactor => reactor
                            .Position(0f, 1.88f, 0.44f)
                            .Scale(0.22f, 0.22f, 0.12f)
                            .ColorRole("glow"))
                        .Shape("robot.waist", "Waist", LowPolyShapeTypes.Cylinder, waist => waist
                            .Position(0f, 1.05f, 0f)
                            .Scale(0.48f, 0.18f, 0.48f)
                            .ColorRole("metal.dark"))
                        .Shape("robot.spine", "Rear Power Spine", LowPolyShapeTypes.Cube, spine => spine
                            .Position(0f, 1.75f, -0.36f)
                            .Scale(0.26f, 0.78f, 0.16f)
                            .ColorRole("metal.dark"))
                        .Shape("robot.vent.left", "Left Chest Vent", LowPolyShapeTypes.Cube, vent => vent
                            .Position(-0.31f, 1.65f, 0.43f)
                            .Rotation(0f, 0f, -12f)
                            .Scale(0.2f, 0.055f, 0.035f)
                            .ColorRole("metal.dark")
                            .Mirror(ShapeMirrorAxis.X));

                    AddHead(robot);
                    AddArm(robot, "left", "Left", -0.72f);
                    AddArm(robot, "right", "Right", 0.72f);
                    AddLeg(robot, "left", "Left", -0.3f);
                    AddLeg(robot, "right", "Right", 0.3f);
                })
                .Build();
        }

        private static ShapeRigJoint Joint(
            string role,
            string nodeId,
            float  minimumX,
            float  maximumX,
            float  minimumY,
            float  maximumY,
            float  minimumZ,
            float  maximumZ)
        {
            return new ShapeRigJoint(role, nodeId, new ShapeRigRotationConstraint(
                new(minimumX, minimumY, minimumZ),
                new(maximumX, maximumY, maximumZ)));
        }

        /// <summary>
        /// Creates the engine-agnostic palette used by the robot preset.
        /// </summary>
        public static ShapeStyleDefinition CreateStyle()
        {
            ShapeStyleDefinition style = new(StyleId);
            style.Palette
                .Set("metal.primary", new(0.08f, 0.34f, 0.62f))
                .Set("metal.secondary", new(0.18f, 0.25f, 0.34f))
                .Set("metal.dark", new(0.045f, 0.06f, 0.085f))
                .Set("accent", new(0.95f, 0.32f, 0.06f))
                .Set("glow", new(0.1f, 0.9f, 1f));
            return style;
        }

        private static void AddHead(ShapeNodeBuilder robot)
        {
            robot.Group("robot.head.pivot", "Head Pivot", pivot => pivot
                .Position(0f, 2.48f, 0f)
                .Shape("robot.neck", "Neck", LowPolyShapeTypes.Cylinder, neck => neck
                    .Scale(0.24f, 0.16f, 0.24f)
                    .ColorRole("metal.dark"))
                .Shape("robot.head", "Helmet", LowPolyShapeTypes.ProfileLoft, head => head
                    .Position(0f, 0.34f, 0f)
                    .Scale(0.72f, 0.58f, 0.62f)
                    .ProfileLoft(
                        HelmetProfile(),
                        Section(-0.5f, 0.72f, 0.82f),
                        Section(0f, 1f, 1f),
                        Section(0.5f, 0.82f, 0.9f))
                    .LoftQuality(2, true)
                    .ColorRole("metal.primary"))
                .Shape("robot.visor", "Visor", LowPolyShapeTypes.Cube, visor => visor
                    .Position(0f, 0.38f, 0.34f)
                    .Scale(0.52f, 0.14f, 0.08f)
                    .ColorRole("glow"))
                .Shape("robot.jaw", "Armored Jaw", LowPolyShapeTypes.ExtrudedProfile, jaw => jaw
                    .Position(0f, 0.16f, 0.34f)
                    .Scale(0.48f, 0.2f, 1f)
                    .ExtrudedProfile(0.09f, 0.018f,
                        new(-0.5f, 0.5f), new(0.5f, 0.5f), new(0.36f, -0.5f), new(-0.36f, -0.5f))
                    .ColorRole("metal.secondary"))
                .Shape("robot.ear.left", "Left Sensor Pod", LowPolyShapeTypes.Cylinder, pod => pod
                    .Position(-0.42f, 0.34f, 0f)
                    .Rotation(0f, 0f, 90f)
                    .Scale(0.13f, 0.09f, 0.13f)
                    .ColorRole("accent")
                    .Mirror(ShapeMirrorAxis.X))
                .Shape("robot.antenna", "Antenna", LowPolyShapeTypes.Cylinder, antenna => antenna
                    .Position(0.22f, 0.85f, 0f)
                    .Scale(0.045f, 0.28f, 0.045f)
                    .ColorRole("metal.dark"))
                .Shape("robot.antenna.tip", "Antenna Tip", LowPolyShapeTypes.Sphere, tip => tip
                    .Position(0.22f, 1.14f, 0f)
                    .Scale(0.1f, 0.1f, 0.1f)
                    .ColorRole("accent")));
        }

        private static void AddArm(ShapeNodeBuilder robot, string side, string label, float x)
        {
            string prefix = $"robot.arm.{side}";
            robot.Group($"{prefix}.shoulder.pivot", $"{label} Shoulder Pivot", shoulder => shoulder
                .Position(x, 2.08f, 0f)
                .Shape($"{prefix}.shoulder", $"{label} Shoulder Armor", LowPolyShapeTypes.ProfileLoft, armor => armor
                    .Scale(0.42f, 0.38f, 0.42f)
                    .ProfileLoft(
                        ArmorProfile(),
                        Section(-0.5f, 0.72f, 0.8f),
                        Section(0f, 1f, 1f),
                        Section(0.5f, 0.82f, 0.88f))
                    .LoftQuality(1, true)
                    .ColorRole("metal.primary"))
                .Shape($"{prefix}.upper", $"{label} Upper Arm", LowPolyShapeTypes.LatheProfile, upper => upper
                    .Position(0f, -0.48f, 0f)
                    .LatheProfile(12, true,
                        new(0.15f, -0.38f), new(0.2f, -0.28f), new(0.18f, 0.22f), new(0.14f, 0.38f))
                    .ProfileSmoothing(1)
                    .ColorRole("metal.secondary"))
                .Group($"{prefix}.elbow.pivot", $"{label} Elbow Pivot", elbow => elbow
                    .Position(0f, -0.9f, 0f)
                    .Shape($"{prefix}.elbow", $"{label} Elbow", LowPolyShapeTypes.Sphere, joint => joint
                        .Scale(0.22f, 0.22f, 0.22f)
                        .ColorRole("accent"))
                    .Shape($"{prefix}.forearm", $"{label} Forearm", LowPolyShapeTypes.LatheProfile, forearm => forearm
                        .Position(0f, -0.45f, 0f)
                        .LatheProfile(12, true,
                            new(0.17f, -0.4f), new(0.25f, -0.25f), new(0.22f, 0.24f), new(0.15f, 0.4f))
                        .ProfileSmoothing(1)
                        .ColorRole("metal.primary"))
                    .Shape($"{prefix}.forearm.guard", $"{label} Forearm Guard", LowPolyShapeTypes.ExtrudedProfile, guard => guard
                        .Position(0f, -0.45f, 0.22f)
                        .Scale(0.3f, 0.5f, 1f)
                        .ExtrudedProfile(0.07f, 0.015f,
                            new(-0.5f, 0.38f), new(0.5f, 0.38f), new(0.36f, -0.5f), new(-0.36f, -0.5f))
                        .ColorRole("accent"))
                    .Shape($"{prefix}.hand", $"{label} Hand", LowPolyShapeTypes.Cube, hand => hand
                        .Position(0f, -0.92f, 0.06f)
                        .Scale(0.28f, 0.22f, 0.34f)
                        .ColorRole("metal.dark"))));
        }

        private static void AddLeg(ShapeNodeBuilder robot, string side, string label, float x)
        {
            string prefix = $"robot.leg.{side}";
            robot.Group($"{prefix}.hip.pivot", $"{label} Hip Pivot", hip => hip
                .Position(x, 1.05f, 0f)
                .Shape($"{prefix}.upper", $"{label} Thigh", LowPolyShapeTypes.ProfileLoft, thigh => thigh
                    .Position(0f, -0.28f, 0f)
                    .Scale(0.38f, 0.52f, 0.42f)
                    .ProfileLoft(
                        ArmorProfile(),
                        Section(-0.5f, 0.72f, 0.78f),
                        Section(0.5f, 1f, 1f))
                    .LoftQuality(1, true)
                    .ColorRole("metal.secondary"))
                .Group($"{prefix}.knee.pivot", $"{label} Knee Pivot", knee => knee
                    .Position(0f, -0.55f, 0f)
                    .Shape($"{prefix}.knee", $"{label} Knee", LowPolyShapeTypes.Sphere, joint => joint
                        .Scale(0.25f, 0.22f, 0.28f)
                        .ColorRole("accent"))
                    .Shape($"{prefix}.shin", $"{label} Shin", LowPolyShapeTypes.ProfileLoft, shin => shin
                        .Position(0f, -0.25f, 0f)
                        .Scale(0.42f, 0.5f, 0.44f)
                        .ProfileLoft(
                            ArmorProfile(),
                            Section(-0.5f, 0.68f, 0.72f),
                            Section(0.08f, 1f, 1f),
                            Section(0.5f, 0.82f, 0.86f))
                        .LoftQuality(1, true)
                        .ColorRole("metal.primary"))
                    .Shape($"{prefix}.knee.guard", $"{label} Knee Guard", LowPolyShapeTypes.ExtrudedProfile, guard => guard
                        .Position(0f, -0.02f, 0.28f)
                        .Scale(0.3f, 0.28f, 1f)
                        .ExtrudedProfile(0.08f, 0.015f,
                            new(0f, 0.5f), new(0.5f, 0.1f), new(0.34f, -0.5f), new(-0.34f, -0.5f), new(-0.5f, 0.1f))
                        .ColorRole("accent"))
                    .Shape($"{prefix}.foot", $"{label} Foot", LowPolyShapeTypes.Cube, foot => foot
                        .Position(0f, -0.48f, 0.14f)
                        .Scale(0.42f, 0.16f, 0.7f)
                        .ColorRole("metal.dark"))));
        }

        private static ForgeVector2[] ArmorProfile()
        {
            return new ForgeVector2[]
            {
                new(-0.32f, 0.5f), new(0.32f, 0.5f), new(0.5f, 0.28f), new(0.44f, -0.38f),
                new(0.28f, -0.5f), new(-0.28f, -0.5f), new(-0.44f, -0.38f), new(-0.5f, 0.28f)
            };
        }

        private static ForgeVector2[] HelmetProfile()
        {
            return new ForgeVector2[]
            {
                new(-0.34f, 0.5f), new(0.34f, 0.5f), new(0.5f, 0.28f), new(0.46f, -0.34f),
                new(0.28f, -0.5f), new(-0.28f, -0.5f), new(-0.46f, -0.34f), new(-0.5f, 0.28f)
            };
        }

        private static ShapeProfileSection Section(float z, float scaleX, float scaleY)
        {
            return new(z, new(scaleX, scaleY), new());
        }
    }
}
