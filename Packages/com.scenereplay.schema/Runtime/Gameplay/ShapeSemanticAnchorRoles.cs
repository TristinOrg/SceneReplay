namespace SceneReplay
{
    /// <summary>
    /// Defines standard extensible roles for game-semantic anchors.
    /// </summary>
    public static class ShapeSemanticAnchorRoles
    {
        /// <summary>Identifies a generic attachment socket.</summary>
        public const string Socket           = "game/socket";
        /// <summary>Identifies a hand grip pose.</summary>
        public const string HandGrip         = "game/hand-grip";
        /// <summary>Identifies a weapon attachment socket.</summary>
        public const string WeaponSocket     = "game/weapon-socket";
        /// <summary>Identifies a mount or rider attachment.</summary>
        public const string MountPoint       = "game/mount-point";
        /// <summary>Identifies a player or AI interaction point.</summary>
        public const string InteractionPoint = "game/interaction-point";
        /// <summary>Identifies a Foot IK target or hint.</summary>
        public const string FootIk           = "game/foot-ik";
        /// <summary>Identifies a grounding reference.</summary>
        public const string Grounding        = "game/grounding";
    }
}
