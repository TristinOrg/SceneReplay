using System;

namespace SceneReplay
{
    /// <summary>
    /// Configures one shape and its children without engine-specific types.
    /// </summary>
    public sealed class ShapeNodeBuilder
    {
        internal ShapeNodeBuilder(ShapeNode node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        internal ShapeNode Node { get; }

        /// <summary>
        /// Sets the local position in meters.
        /// </summary>
        public ShapeNodeBuilder Position(float x, float y, float z)
        {
            Node.Transform.Position = new ForgeVector3(x, y, z);
            return this;
        }

        /// <summary>
        /// Sets the local Euler rotation in degrees using the specification rotation order.
        /// </summary>
        public ShapeNodeBuilder Rotation(float x, float y, float z)
        {
            Node.Transform.EulerAngles = new ForgeVector3(x, y, z);
            return this;
        }

        /// <summary>
        /// Sets the local scale.
        /// </summary>
        public ShapeNodeBuilder Scale(float x, float y, float z)
        {
            Node.Transform.Scale = new ForgeVector3(x, y, z);
            return this;
        }

        /// <summary>
        /// Generates a second instance reflected across the selected local parent-space axis.
        /// </summary>
        public ShapeNodeBuilder Mirror(ShapeMirrorAxis axis)
        {
            if (!Enum.IsDefined(typeof(ShapeMirrorAxis), axis))
                throw new ArgumentOutOfRangeException(nameof(axis));

            Node.MirrorAxis = axis;
            return this;
        }

        /// <summary>
        /// Selects a semantic color role from the active style palette.
        /// </summary>
        public ShapeNodeBuilder ColorRole(string role)
        {
            Node.Appearance.ColorRole = role ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Applies a direct linear RGBA color override.
        /// </summary>
        public ShapeNodeBuilder Color(float r, float g, float b, float a = 1f)
        {
            Node.Appearance.HasColorOverride = true;
            Node.Appearance.Color            = new ForgeColor(r, g, b, a);
            return this;
        }

        /// <summary>
        /// Adds or replaces an engine-agnostic numeric shape parameter.
        /// </summary>
        public ShapeNodeBuilder Parameter(string name, float value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Shape parameter names cannot be empty.", nameof(name));

            Node.Parameters[name] = value;
            return this;
        }

        /// <summary>
        /// Replaces the normalized two-dimensional outline used by profile-based shape implementations.
        /// </summary>
        public ShapeNodeBuilder Profile(params ForgeVector2[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            Node.Profile.Clear();
            foreach (ForgeVector2 point in points)
                Node.Profile.Add(point);

            return this;
        }

        /// <summary>
        /// Replaces the ordered three-dimensional control path used by path-based shape implementations.
        /// </summary>
        public ShapeNodeBuilder Path(params ForgeVector3[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            Node.Path.Clear();
            foreach (ForgeVector3 point in points)
                Node.Path.Add(point);

            return this;
        }

        /// <summary>
        /// Adds an ordered depth section used to scale and offset a profile loft.
        /// </summary>
        public ShapeNodeBuilder ProfileSection(
            float z,
            float scaleX,
            float scaleY,
            float offsetX = 0f,
            float offsetY = 0f)
        {
            Node.ProfileSections.Add(new(
                z,
                new(scaleX, scaleY),
                new(offsetX, offsetY)));
            return this;
        }

        /// <summary>
        /// Adds an ordered depth section with an independently authored closed profile.
        /// </summary>
        public ShapeNodeBuilder ProfileCageSection(float z, params ForgeVector2[] profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            Node.ProfileCageSections.Add(new(z, profile));
            return this;
        }

        /// <summary>
        /// Adds a non-rendering child group.
        /// </summary>
        public ShapeNodeBuilder Group(
            string                   id,
            string                   name,
            Action<ShapeNodeBuilder> configure = null)
        {
            return Shape(id, name, ShapeTypes.Group, configure);
        }

        /// <summary>
        /// Adds a child using an extensible shape type identifier.
        /// </summary>
        public ShapeNodeBuilder Shape(
            string                   id,
            string                   name,
            string                   type,
            Action<ShapeNodeBuilder> configure = null)
        {
            ShapeNodeBuilder child = new ShapeNodeBuilder(new ShapeNode(id, name, type));
            configure?.Invoke(child);
            Node.Add(child.Node);
            return this;
        }
    }
}
