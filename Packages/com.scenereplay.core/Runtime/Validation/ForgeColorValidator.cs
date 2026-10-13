namespace SceneReplay
{
    /// <summary>
    /// Validates colors against the normalized linear RGBA specification.
    /// </summary>
    internal static class ForgeColorValidator
    {
        public static void Validate(ForgeColor color, string owner)
        {
            if (!IsNormalized(color.R) ||
                !IsNormalized(color.G) ||
                !IsNormalized(color.B) ||
                !IsNormalized(color.A))
                throw new ShapeValidationException($"{owner} contains a color outside the normalized range [0, 1].");
        }

        private static bool IsNormalized(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value) &&
                   value >= 0f &&
                   value <= 1f;
        }
    }
}
