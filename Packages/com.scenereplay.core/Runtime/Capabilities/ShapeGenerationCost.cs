namespace SceneReplay
{
    /// <summary>
    /// Indicates how a shape's mesh-generation cost scales with authored data.
    /// </summary>
    public enum ShapeGenerationCost
    {
        Constant,
        Parameterized,
        InputScaled
    }
}
