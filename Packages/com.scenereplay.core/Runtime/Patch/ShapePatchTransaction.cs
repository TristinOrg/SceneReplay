namespace SceneReplay
{
    /// <summary>
    /// Contains the result of applying a patch and its deterministic inverse.
    /// </summary>
    public sealed class ShapePatchTransaction
    {
        /// <summary>Initializes a completed patch transaction.</summary>
        internal ShapePatchTransaction(ShapeDefinition definition, ShapePatchDocument inversePatch)
        {
            Definition = definition;
            InversePatch = inversePatch;
        }

        /// <summary>Gets the validated definition produced by the patch.</summary>
        public ShapeDefinition Definition { get; }

        /// <summary>Gets a patch that restores the definition supplied to the transaction.</summary>
        public ShapePatchDocument InversePatch { get; }
    }
}
