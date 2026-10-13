namespace SceneReplay
{
    /// <summary>
    /// Resolves style-dependent appearance without coupling Core to a concrete style package.
    /// </summary>
    public interface IShapeStyleResolver
    {
        /// <summary>
        /// Attempts to resolve the color requested by a shape node.
        /// </summary>
        bool TryResolveColor(ShapeDefinition definition, ShapeNode node, out ForgeColor color);
    }
}
