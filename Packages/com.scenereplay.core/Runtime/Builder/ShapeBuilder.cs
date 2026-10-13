using System;

namespace SceneReplay
{
    /// <summary>
    /// Builds validated engine-agnostic SceneReplay model definitions fluently.
    /// </summary>
    public sealed class ShapeBuilder
    {
        private readonly string name;
        private string          style = string.Empty;
        private ShapeRigDefinition rig;
        private ShapeNode       root;

        private ShapeBuilder(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A model name cannot be empty.", nameof(name));

            this.name = name;
        }

        /// <summary>
        /// Starts a new engine-agnostic model definition.
        /// </summary>
        public static ShapeBuilder Create(string name)
        {
            return new ShapeBuilder(name);
        }

        /// <summary>
        /// Selects the style identifier used when generating the model.
        /// </summary>
        public ShapeBuilder WithStyle(string style)
        {
            this.style = style ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Exposes semantic joint roles for engine-independent motion systems.
        /// </summary>
        public ShapeBuilder WithRig(string type, params ShapeRigJoint[] joints)
        {
            rig = new ShapeRigDefinition
            {
                Type   = type,
                Joints = joints ?? Array.Empty<ShapeRigJoint>()
            };
            return this;
        }

        /// <summary>
        /// Defines the hierarchy root as a non-rendering group.
        /// </summary>
        public ShapeBuilder Root(
            string                   id,
            string                   name,
            Action<ShapeNodeBuilder> configure = null)
        {
            ShapeNodeBuilder builder = new ShapeNodeBuilder(new ShapeNode(id, name, ShapeTypes.Group));
            configure?.Invoke(builder);
            root = builder.Node;
            return this;
        }

        /// <summary>
        /// Builds and validates the model definition.
        /// </summary>
        public ShapeDefinition Build()
        {
            if (root == null)
                throw new InvalidOperationException("A model requires a root shape.");

            ShapeDefinition definition = new ShapeDefinition(name, root)
            {
                Style = style,
                Rig   = rig
            };

            new ShapeDefinitionValidator().Validate(definition);
            return definition;
        }
    }
}
