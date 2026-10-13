using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneReplay.Unity
{
    /// <summary>
    /// Compiles engine-neutral game metadata into native Unity anchors, colliders, LODs, and a root manifest.
    /// </summary>
    public sealed class UnityShapeGameMetadataCompiler
    {
        /// <summary>Validates and compiles game metadata for a generated Unity model.</summary>
        public UnityShapeGameMetadataManifest Compile(
            UnityShapeModel   model,
            ShapeDefinition   definition,
            ShapeGameMetadata metadata)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            ShapeGameMetadataReport report = new ShapeGameMetadataAnalyzer().Analyze(definition, metadata);
            if (!report.IsValid)
                throw new ShapeValidationException(report.Diagnostics.Diagnostics[0].Message);
            if (model.TryGetComponent(out UnityShapeGameMetadataManifest _))
                throw new InvalidOperationException("Game metadata has already been compiled for this model.");

            ValidateMeshColliders(model, metadata);
            List<UnityEngine.Object> created = new();
            try
            {
                UnityShapeGameMetadataManifest manifest = model.gameObject.AddComponent<UnityShapeGameMetadataManifest>();
                created.Add(manifest);
                manifest.Initialize(metadata);
                CompileAnchors(model, metadata, manifest, created);
                CompileDamageZones(model, metadata, manifest);
                CompileColliders(model, metadata, created);
                CompileLods(model, metadata, created);
                return manifest;
            }
            catch
            {
                for (int index = created.Count - 1; index >= 0; index--)
                    Destroy(created[index]);
                throw;
            }
        }

        private static void CompileAnchors(
            UnityShapeModel model,
            ShapeGameMetadata metadata,
            UnityShapeGameMetadataManifest manifest,
            ICollection<UnityEngine.Object> created)
        {
            foreach (ShapeSemanticAnchor definition in metadata.Anchors)
            {
                model.TryGetTransform(definition.NodeId, out Transform owner);
                GameObject anchor = new($"ShapeAnchor_{definition.Id}");
                created.Add(anchor);
                anchor.transform.SetParent(owner, false);
                anchor.transform.localPosition    = definition.Transform.Position.ToUnity();
                anchor.transform.localEulerAngles = definition.Transform.EulerAngles.ToUnity();
                anchor.transform.localScale       = definition.Transform.Scale.ToUnity();
                manifest.AddAnchor(definition.Id, definition.Role, anchor.transform);
            }
        }

        private static void CompileDamageZones(
            UnityShapeModel model,
            ShapeGameMetadata metadata,
            UnityShapeGameMetadataManifest manifest)
        {
            foreach (ShapeDamageZone zone in metadata.DamageZones)
            {
                model.TryGetTransform(zone.NodeId, out Transform target);
                manifest.AddDamageZone(zone.Id, target, zone.Multiplier);
            }
        }

        private static void CompileColliders(
            UnityShapeModel model,
            ShapeGameMetadata metadata,
            ICollection<UnityEngine.Object> created)
        {
            foreach (ShapeColliderRule rule in metadata.Colliders)
            {
                model.TryGetTransform(rule.NodeId, out Transform target);
                Collider collider;
                switch (rule.Kind)
                {
                    case ShapeColliderKind.Box:
                        BoxCollider box = target.gameObject.AddComponent<BoxCollider>();
                        box.center = rule.Center.ToUnity();
                        box.size   = rule.Size.ToUnity();
                        collider   = box;
                        break;
                    case ShapeColliderKind.Sphere:
                        SphereCollider sphere = target.gameObject.AddComponent<SphereCollider>();
                        sphere.center = rule.Center.ToUnity();
                        sphere.radius = rule.Radius;
                        collider      = sphere;
                        break;
                    case ShapeColliderKind.Capsule:
                        CapsuleCollider capsule = target.gameObject.AddComponent<CapsuleCollider>();
                        capsule.center = rule.Center.ToUnity();
                        capsule.radius = rule.Radius;
                        capsule.height = rule.Height;
                        collider       = capsule;
                        break;
                    case ShapeColliderKind.Mesh:
                        MeshCollider mesh = target.gameObject.AddComponent<MeshCollider>();
                        mesh.sharedMesh = target.GetComponent<MeshFilter>().sharedMesh;
                        mesh.convex     = rule.IsTrigger;
                        collider        = mesh;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                collider.isTrigger = rule.IsTrigger;
                created.Add(collider);
            }
        }

        private static void CompileLods(
            UnityShapeModel model,
            ShapeGameMetadata metadata,
            ICollection<UnityEngine.Object> created)
        {
            if (metadata.Lods.Count == 0)
                return;
            LOD[] levels = new LOD[metadata.Lods.Count];
            for (int index = 0; index < metadata.Lods.Count; index++)
            {
                ShapeLodRule rule = metadata.Lods[index];
                HashSet<Renderer> renderers = new();
                foreach (string nodeId in rule.NodeIds)
                {
                    model.TryGetTransform(nodeId, out Transform target);
                    foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
                        renderers.Add(renderer);
                }
                Renderer[] rendererArray = new Renderer[renderers.Count];
                renderers.CopyTo(rendererArray);
                levels[index] = new(rule.ScreenRelativeHeight, rendererArray);
            }
            LODGroup group = model.gameObject.AddComponent<LODGroup>();
            created.Add(group);
            group.SetLODs(levels);
            group.RecalculateBounds();
        }

        private static void ValidateMeshColliders(UnityShapeModel model, ShapeGameMetadata metadata)
        {
            foreach (ShapeColliderRule rule in metadata.Colliders)
            {
                if (rule.Kind != ShapeColliderKind.Mesh)
                    continue;
                model.TryGetTransform(rule.NodeId, out Transform target);
                if (!target.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                    throw new ShapeValidationException($"Mesh collider '{rule.Id}' requires generated mesh geometry.");
            }
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(value);
            else
                UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
