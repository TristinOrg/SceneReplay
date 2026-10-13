using System;
using System.Collections.Generic;

namespace SceneReplay
{
    /// <summary>
    /// Caches semantic roles, rest poses, constraints, and writable engine targets for MotionForge.
    /// </summary>
    public sealed class ShapeMotionBindingResolver : IShapeMotionBindingResolver
    {
        private readonly Dictionary<string, ShapeMotionBinding>     bindings;
        private readonly Dictionary<string, IShapeTransformTarget> targets;
        private readonly ShapeRigIndex                              rigIndex;

        /// <summary>Builds and validates a complete cache without retaining the definition tree.</summary>
        public ShapeMotionBindingResolver(ShapeDefinition definition, IShapeTransformResolver resolver)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            new ShapeDefinitionValidator().Validate(definition);
            if (definition.Rig == null)
                throw new ShapeValidationException("A motion binding requires a semantic rig.");

            RigType  = definition.Rig.Type;
            rigIndex = new(definition.Rig);
            Dictionary<string, ShapeNode> nodes = new(StringComparer.Ordinal);
            Collect(definition.Root, nodes);
            bindings = new(definition.Rig.Joints.Count, StringComparer.Ordinal);
            targets  = new(definition.Rig.Joints.Count, StringComparer.Ordinal);
            foreach (ShapeRigJoint joint in definition.Rig.Joints)
            {
                ShapeNode node = nodes[joint.NodeId];
                if (!resolver.TryGetTarget(joint.NodeId, out IShapeTransformTarget target))
                    throw new ShapeValidationException($"Motion target '{joint.NodeId}' is unavailable.");
                bindings.Add(joint.Role, new(
                    joint.Role, joint.NodeId, Clone(node.Transform), Clone(joint.RotationConstraint)));
                targets.Add(joint.Role, target);
            }
        }

        /// <inheritdoc />
        public string RigType { get; }
        /// <inheritdoc />
        public int BindingCount => bindings.Count;
        /// <inheritdoc />
        public bool TryGetBinding(string role, out ShapeMotionBinding binding) =>
            bindings.TryGetValue(role, out binding);
        /// <inheritdoc />
        public bool TryGetTarget(string role, out IShapeTransformTarget target) =>
            targets.TryGetValue(role, out target);
        /// <inheritdoc />
        public ForgeVector3 ConstrainRotationOffset(string role, ForgeVector3 requestedOffset) =>
            rigIndex.ConstrainRotationOffset(role, requestedOffset);

        /// <inheritdoc />
        public void ResetToRestPose()
        {
            foreach (KeyValuePair<string, ShapeMotionBinding> pair in bindings)
            {
                IShapeTransformTarget target = targets[pair.Key];
                ShapeTransform       rest   = pair.Value.RestPose;
                target.LocalPosition    = rest.Position;
                target.LocalEulerAngles = rest.EulerAngles;
                target.LocalScale       = rest.Scale;
            }
        }

        private static void Collect(ShapeNode node, IDictionary<string, ShapeNode> nodes)
        {
            nodes.Add(node.Id, node);
            foreach (ShapeNode child in node.Children)
                Collect(child, nodes);
        }

        private static ShapeTransform Clone(ShapeTransform value) => new()
        {
            Position    = value.Position,
            EulerAngles = value.EulerAngles,
            Scale       = value.Scale
        };

        private static ShapeRigRotationConstraint Clone(ShapeRigRotationConstraint value) =>
            value == null ? null : new(value.Minimum, value.Maximum);
    }
}
