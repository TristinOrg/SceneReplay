using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Persists stable node-to-Transform bindings and exposes them to engine-agnostic motion systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityShapeModel : MonoBehaviour, IShapeHierarchyTransformResolver
    {
        [SerializeField] private List<string>    nodeIds    = new();
        [SerializeField] private List<string>    parentIds  = new();
        [SerializeField] private List<Transform> targets    = new();

        private Dictionary<string, Transform> targetsById;
        private Dictionary<string, string> parentIdsById;

        /// <inheritdoc />
        public string AdapterId => UnityShapeAdapter.AdapterId;

        /// <summary>
        /// Gets the number of generated node bindings.
        /// </summary>
        public int BindingCount => nodeIds.Count;

        /// <inheritdoc />
        public bool TryGetTarget(string nodeId, out IShapeTransformTarget target)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Node ID cannot be empty.", nameof(nodeId));

            EnsureLookup();
            if (targetsById.TryGetValue(nodeId, out Transform transform))
            {
                target = new UnityShapeTransformTarget(nodeId, transform);
                return true;
            }

            target = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetParentNodeId(string nodeId, out string parentNodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException("Node ID cannot be empty.", nameof(nodeId));

            EnsureLookup();
            return parentIdsById.TryGetValue(nodeId, out parentNodeId);
        }

        internal void AddBinding(string nodeId, string parentNodeId, Transform target)
        {
            nodeIds.Add(nodeId);
            parentIds.Add(parentNodeId ?? string.Empty);
            targets.Add(target);
            targetsById = null;
            parentIdsById = null;
        }

        internal bool TryGetTransform(string nodeId, out Transform target)
        {
            EnsureLookup();
            return targetsById.TryGetValue(nodeId, out target);
        }

        /// <summary>Tries to resolve a generated Transform back to its stable SceneReplay node ID.</summary>
        public bool TryGetNodeId(Transform target, out string nodeId)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            for (int index = 0; index < targets.Count; index++)
            {
                if (targets[index] != target)
                    continue;
                nodeId = nodeIds[index];
                return true;
            }
            nodeId = null;
            return false;
        }

        private void EnsureLookup()
        {
            if (targetsById != null)
                return;

            targetsById = new(nodeIds.Count, StringComparer.Ordinal);
            parentIdsById = new(nodeIds.Count, StringComparer.Ordinal);
            for (int index = 0; index < nodeIds.Count; index++)
            {
                targetsById.Add(nodeIds[index], targets[index]);
                parentIdsById.Add(nodeIds[index], index < parentIds.Count ? parentIds[index] : string.Empty);
            }
        }

        private void OnValidate()
        {
            targetsById = null;
            parentIdsById = null;
        }
    }

    /// <summary>
    /// Adapts one Unity Transform to the engine-agnostic motion target contract.
    /// </summary>
    internal sealed class UnityShapeTransformTarget : IShapeTransformTarget
    {
        private readonly Transform target;

        public UnityShapeTransformTarget(string nodeId, Transform target)
        {
            NodeId      = nodeId;
            this.target = target;
        }

        public string NodeId { get; }

        public ForgeVector3 LocalPosition
        {
            get => target.localPosition.ToForge();
            set => target.localPosition = value.ToUnity();
        }

        public ForgeVector3 LocalEulerAngles
        {
            get => target.localEulerAngles.ToForge();
            set => target.localEulerAngles = value.ToUnity();
        }

        public ForgeVector3 LocalScale
        {
            get => target.localScale.ToForge();
            set => target.localScale = value.ToUnity();
        }
    }
}
