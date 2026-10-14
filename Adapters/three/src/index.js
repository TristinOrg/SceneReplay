import { Color, Euler, Group, Mesh, MeshStandardMaterial } from 'three';
import {
  createLowPolyGeometry,
  supportedLowPolyTypes,
  validateLowPolyNode,
} from './geometries.js';
import { applyPatchToDefinition } from './patch.js';

const compilationOptionsByRoot = new WeakMap();
const sourceDefinitionsByRoot = new WeakMap();

export const adapterDescriptor = Object.freeze({
  schema: 'scenereplay.adapter/1.0',
  adapterId: 'scenereplay/three',
  engineId: 'three',
  version: '0.1.0',
  features: Object.freeze(['stable-node-ids', 'hierarchy', 'local-transforms', 'game-metadata', 'semantic-rig', 'patch-application']),
  unsupportedFeaturePolicy: 'reject',
});

const groupType = 'core/group';
const supportedGeometryTypes = new Set(supportedLowPolyTypes);

/** Compiles a supported SceneReplay hierarchy into native Three.js objects. */
export function compileShapeDefinition(definition, options = {}) {
  validateDefinition(definition);
  if (options.gameMetadata) validateGameMetadata(definition, options.gameMetadata);
  const root = new Group();
  root.name = definition.name;
  root.userData.sceneReplay = {
    schema: definition.schema,
    name: definition.name,
    style: definition.style ?? '',
    adapterId: adapterDescriptor.adapterId,
  };
  root.add(compileNode(definition.root, definition.style, options));
  if (options.gameMetadata) attachGameMetadata(root, options.gameMetadata);
  if (definition.rig) attachRig(root, definition.rig);
  compilationOptionsByRoot.set(root, { resolveColorRole: options.resolveColorRole });
  sourceDefinitionsByRoot.set(root, cloneJson(definition));
  return root;
}

/** Applies an engine-neutral semantic patch atomically and preserves compatible Object3D identities. */
export function applyShapePatch(compiledRoot, patch, options = {}) {
  collectCompiledNodes(compiledRoot);
  const state = compiledRoot.userData.sceneReplay;
  const sourceDefinition = sourceDefinitionsByRoot.get(compiledRoot);
  if (!sourceDefinition) throw new Error('The compiled hierarchy has no retained source definition for patching.');
  const nextDefinition = applyPatchToDefinition(sourceDefinition, patch);
  const gameMetadata = options.gameMetadata ?? state.gameMetadata;
  const currentOptions = compilationOptionsByRoot.get(compiledRoot) ?? {};
  const nextOptions = { ...currentOptions, ...options, gameMetadata };
  const nextRoot = compileShapeDefinition(nextDefinition, {
    ...nextOptions,
  });

  reconcileChildren(
    compiledRoot,
    nextRoot,
    collectDefinitionNodes(sourceDefinition),
    collectDefinitionNodes(nextDefinition),
    currentOptions.resolveColorRole === nextOptions.resolveColorRole,
  );
  compiledRoot.name = nextRoot.name;
  compiledRoot.userData.sceneReplay = nextRoot.userData.sceneReplay;
  compilationOptionsByRoot.set(compiledRoot, compilationOptionsByRoot.get(nextRoot));
  sourceDefinitionsByRoot.set(compiledRoot, nextDefinition);
  return compiledRoot;
}

/** Preserves validated game metadata on the matching objects in a compiled hierarchy. */
export function applyGameMetadata(compiledRoot, metadata) {
  const nodeIds = collectCompiledNodes(compiledRoot);
  validateGameMetadataAgainstNodeIds(metadata, nodeIds);
  attachGameMetadata(compiledRoot, metadata);
  return compiledRoot;
}

/** Resolves a semantic rig role to its stable native Object3D transform target. */
export function getRigTarget(compiledRoot, role) {
  if (typeof role !== 'string' || !role.trim()) return undefined;
  const nodes = collectCompiledNodes(compiledRoot);
  for (const node of nodes.values()) {
    const joint = node.userData.sceneReplay?.rigJoints?.find(candidate => candidate.role === role);
    if (joint) return node;
  }
  return undefined;
}

function validateDefinition(definition) {
  if (definition?.schema !== 'scenereplay.shape/1.0') {
    throw new Error(`Unsupported SceneReplay schema '${definition?.schema ?? ''}'.`);
  }
  if (typeof definition.name !== 'string' || !definition.name.trim()) {
    throw new Error('Shape definition name must be a non-empty string.');
  }
  if (!definition.root) throw new Error('Shape definition root is required.');
  const ids = new Set();
  const visit = (node, path) => {
    if (!node || typeof node !== 'object') throw new Error(`${path}: node is required.`);
    if (typeof node.id !== 'string' || !node.id.trim()) throw new Error(`${path}.id: stable node ID is required.`);
    if (ids.has(node.id)) throw new Error(`${path}.id: duplicate node ID '${node.id}'.`);
    ids.add(node.id);
    if (node.type !== groupType && !supportedGeometryTypes.has(node.type)) {
      throw new Error(`${path}.type: unsupported shape type '${node.type}'.`);
    }
    validateLowPolyNode(node, path);
    if (node.mirrorAxis && node.mirrorAxis !== 'none') {
      throw new Error(`${path}.mirrorAxis: mirrored nodes are not supported by the Three.js adapter yet.`);
    }
    if (!node.transform) throw new Error(`${path}.transform: transform is required.`);
    validateVector(node.transform.position, `${path}.transform.position`);
    validateVector(node.transform.eulerAngles, `${path}.transform.eulerAngles`);
    validateVector(node.transform.scale, `${path}.transform.scale`);
    if (node.children !== undefined && !Array.isArray(node.children)) {
      throw new Error(`${path}.children: expected an array.`);
    }
    const appearance = node.appearance;
    if (appearance?.hasColorOverride) {
      const color = appearance.color;
      if (!color || ![color.r, color.g, color.b, color.a].every(Number.isFinite) ||
          [color.r, color.g, color.b, color.a].some(component => component < 0 || component > 1)) {
        throw new Error(`${path}.appearance.color: expected finite normalized RGBA components.`);
      }
    }
    node.children?.forEach((child, index) => visit(child, `${path}.children[${index}]`));
  };

  visit(definition.root, '$.root');
  if (definition.rig !== undefined) validateRig(definition.rig, ids);
}

function validateRig(rig, nodeIds) {
  if (!rig || typeof rig !== 'object' || typeof rig.type !== 'string' || !rig.type.trim()) {
    throw new Error('Rig requires a non-empty type.');
  }
  if (!Array.isArray(rig.joints)) throw new Error('Rig joints must be an array.');
  const roles = new Set();
  rig.joints.forEach((joint, index) => {
    const path = `$.rig.joints[${index}]`;
    if (!joint || typeof joint.role !== 'string' || !joint.role.trim()) {
      throw new Error(`${path}.role: a non-empty semantic role is required.`);
    }
    if (roles.has(joint.role)) throw new Error(`${path}.role: duplicate rig role '${joint.role}'.`);
    roles.add(joint.role);
    if (typeof joint.nodeId !== 'string' || !nodeIds.has(joint.nodeId)) {
      throw new Error(`${path}.nodeId: rig references an unknown shape node '${joint.nodeId ?? ''}'.`);
    }
    if (joint.rotationConstraint !== undefined) {
      const { minimum, maximum } = joint.rotationConstraint ?? {};
      validateVector(minimum, `${path}.rotationConstraint.minimum`);
      validateVector(maximum, `${path}.rotationConstraint.maximum`);
      for (const component of ['x', 'y', 'z']) {
        if (minimum[component] > maximum[component]) {
          throw new Error(`${path}.rotationConstraint.${component}: minimum cannot exceed maximum.`);
        }
      }
    }
  });
}

function attachRig(compiledRoot, rig) {
  const nodes = collectCompiledNodes(compiledRoot);
  const snapshot = cloneJson(rig);
  compiledRoot.userData.sceneReplay.rig = snapshot;
  for (const joint of snapshot.joints) {
    const target = nodes.get(joint.nodeId);
    target.userData.sceneReplay.rigJoints ??= [];
    target.userData.sceneReplay.rigJoints.push({
      type: snapshot.type,
      role: joint.role,
      nodeId: joint.nodeId,
      restEulerAngles: {
        x: target.rotation.x * 180 / Math.PI,
        y: target.rotation.y * 180 / Math.PI,
        z: target.rotation.z * 180 / Math.PI,
      },
      ...(joint.rotationConstraint ? { rotationConstraint: joint.rotationConstraint } : {}),
    });
  }
}

function validateVector(value, path) {
  if (!value || ![value.x, value.y, value.z].every(Number.isFinite)) {
    throw new Error(`${path}: expected finite x, y, and z values.`);
  }
}

function validateGameMetadata(definition, metadata) {
  const nodeIds = new Set();
  const collect = node => {
    nodeIds.add(node.id);
    node.children?.forEach(collect);
  };
  collect(definition.root);
  validateGameMetadataAgainstNodeIds(metadata, nodeIds);
}

function validateGameMetadataAgainstNodeIds(metadata, nodeIds) {
  if (!metadata || metadata.schema !== 'scenereplay.game-metadata/1.0') {
    throw new Error(`Unsupported game-metadata schema '${metadata?.schema ?? ''}'.`);
  }
  if (typeof metadata.id !== 'string' || !metadata.id.trim()) {
    throw new Error('Game metadata requires a stable ID.');
  }
  for (const key of ['anchors', 'damageZones', 'colliders', 'lods', 'tags']) {
    if (!Array.isArray(metadata[key])) throw new Error(`Game metadata '${key}' must be an array.`);
  }
  validateTags(metadata.tags, '$.tags');
  validateRecords(metadata.anchors, 'anchor', '$.anchors', nodeIds, record => {
    if (typeof record.role !== 'string' || !record.role.trim()) throw new Error(`${record.path}.role: role is required.`);
    validateTransform(record.transform, `${record.path}.transform`);
    validateTags(record.tags, `${record.path}.tags`);
  });
  validateRecords(metadata.damageZones, 'damage zone', '$.damageZones', nodeIds, record => {
    if (!Number.isFinite(record.multiplier) || record.multiplier < 0) {
      throw new Error(`${record.path}.multiplier: expected a finite non-negative value.`);
    }
    validateTags(record.tags, `${record.path}.tags`);
  });
  validateRecords(metadata.colliders, 'collider', '$.colliders', nodeIds, record => {
    if (!['box', 'sphere', 'capsule', 'mesh'].includes(record.kind)) {
      throw new Error(`${record.path}.kind: unsupported collider kind '${record.kind}'.`);
    }
    validateVector(record.center, `${record.path}.center`);
    validatePositiveVector(record.size, `${record.path}.size`);
    if (!Number.isFinite(record.radius) || record.radius <= 0 ||
        !Number.isFinite(record.height) || record.height <= 0) {
      throw new Error(`${record.path}: radius and height must be finite positive values.`);
    }
    if (typeof record.isTrigger !== 'boolean') throw new Error(`${record.path}.isTrigger: expected a boolean.`);
  });

  let previousHeight = Number.POSITIVE_INFINITY;
  metadata.lods.forEach((lod, index) => {
    const path = `$.lods[${index}]`;
    if (!lod || lod.level !== index) throw new Error(`${path}.level: LOD levels must be contiguous and ascending.`);
    if (!Number.isFinite(lod.screenRelativeHeight) || lod.screenRelativeHeight < 0 ||
        lod.screenRelativeHeight > 1 || lod.screenRelativeHeight >= previousHeight) {
      throw new Error(`${path}.screenRelativeHeight: values must be normalized and strictly descending.`);
    }
    previousHeight = lod.screenRelativeHeight;
    if (!Array.isArray(lod.nodeIds)) throw new Error(`${path}.nodeIds: expected an array.`);
    validateTags(lod.nodeIds, `${path}.nodeIds`);
    lod.nodeIds.forEach((nodeId, nodeIndex) => requireKnownNode(nodeId, `${path}.nodeIds[${nodeIndex}]`, nodeIds));
  });
}

function validateRecords(records, label, path, nodeIds, validateRecord) {
  const ids = new Set();
  records.forEach((record, index) => {
    const recordPath = `${path}[${index}]`;
    if (!record || typeof record !== 'object') throw new Error(`${recordPath}: ${label} is required.`);
    if (typeof record.id !== 'string' || !record.id.trim() || ids.has(record.id)) {
      throw new Error(`${recordPath}.id: ${label} requires a unique stable ID.`);
    }
    ids.add(record.id);
    requireKnownNode(record.nodeId, `${recordPath}.nodeId`, nodeIds);
    validateRecord({ ...record, path: recordPath });
  });
}

function validateTransform(transform, path) {
  if (!transform) throw new Error(`${path}: transform is required.`);
  validateVector(transform.position, `${path}.position`);
  validateVector(transform.eulerAngles, `${path}.eulerAngles`);
  validateVector(transform.scale, `${path}.scale`);
}

function validatePositiveVector(value, path) {
  validateVector(value, path);
  if ([value.x, value.y, value.z].some(component => component <= 0)) {
    throw new Error(`${path}: expected positive x, y, and z values.`);
  }
}

function validateTags(tags, path) {
  if (!Array.isArray(tags)) throw new Error(`${path}: expected an array.`);
  const unique = new Set();
  tags.forEach((tag, index) => {
    if (typeof tag !== 'string' || !tag.trim() || unique.has(tag)) {
      throw new Error(`${path}[${index}]: tags must be non-empty and unique.`);
    }
    unique.add(tag);
  });
}

function requireKnownNode(nodeId, path, nodeIds) {
  if (typeof nodeId !== 'string' || !nodeIds.has(nodeId)) {
    throw new Error(`${path}: metadata references unknown node '${nodeId ?? ''}'.`);
  }
}

function collectCompiledNodes(compiledRoot) {
  if (!compiledRoot?.userData?.sceneReplay || compiledRoot.userData.sceneReplay.adapterId !== adapterDescriptor.adapterId) {
    throw new Error('Expected a root compiled by the SceneReplay Three.js adapter.');
  }
  const nodes = new Map();
  compiledRoot.traverse(object => {
    const id = object.userData?.sceneReplay?.id;
    if (typeof id === 'string' && id) nodes.set(id, object);
  });
  return nodes;
}

function attachGameMetadata(compiledRoot, metadata) {
  if (compiledRoot.userData.sceneReplay.gameMetadata) {
    throw new Error('Game metadata has already been applied to this hierarchy.');
  }
  const nodes = collectCompiledNodes(compiledRoot);
  const snapshot = JSON.parse(JSON.stringify(metadata));
  const sceneReplayRoot = compiledRoot.userData.sceneReplay;
  sceneReplayRoot.gameMetadata = snapshot;
  sceneReplayRoot.gameplayTags = [...metadata.tags];

  metadata.anchors.forEach(anchor => {
    const object = new Group();
    object.name = `Anchor_${anchor.id}`;
    object.position.set(anchor.transform.position.x, anchor.transform.position.y, anchor.transform.position.z);
    object.rotation.copy(new Euler(
      degreesToRadians(anchor.transform.eulerAngles.x),
      degreesToRadians(anchor.transform.eulerAngles.y),
      degreesToRadians(anchor.transform.eulerAngles.z),
      'XYZ',
    ));
    object.scale.set(anchor.transform.scale.x, anchor.transform.scale.y, anchor.transform.scale.z);
    object.userData.sceneReplay = { anchorId: anchor.id, role: anchor.role, tags: [...anchor.tags] };
    nodes.get(anchor.nodeId).add(object);
  });
  metadata.damageZones.forEach(zone => {
    const target = nodes.get(zone.nodeId);
    target.userData.sceneReplay.damageZones ??= [];
    target.userData.sceneReplay.damageZones.push({ ...zone, tags: [...zone.tags] });
  });
  metadata.colliders.forEach(rule => {
    const target = nodes.get(rule.nodeId);
    target.userData.sceneReplay.colliderRules ??= [];
    target.userData.sceneReplay.colliderRules.push({ ...rule, center: { ...rule.center }, size: { ...rule.size } });
  });
  sceneReplayRoot.lods = metadata.lods.map(lod => ({ ...lod, nodeIds: [...lod.nodeIds] }));
}

function compileNode(node, style, options) {
  const object = createObject(node, style, options);
  object.name = node.name || node.id;
  object.position.set(node.transform.position.x, node.transform.position.y, node.transform.position.z);
  object.rotation.copy(new Euler(
    degreesToRadians(node.transform.eulerAngles.x),
    degreesToRadians(node.transform.eulerAngles.y),
    degreesToRadians(node.transform.eulerAngles.z),
    'XYZ',
  ));
  object.scale.set(node.transform.scale.x, node.transform.scale.y, node.transform.scale.z);
  object.userData.sceneReplay = {
    id: node.id,
    type: node.type,
    colorRole: node.appearance?.colorRole ?? '',
  };
  node.children?.forEach(child => object.add(compileNode(child, style, options)));
  return object;
}

function createObject(node, style, options) {
  if (node.type === groupType) return new Group();
  return createMesh(createLowPolyGeometry(node), node, style, options);
}

function createMesh(geometry, node, style, options) {
  const appearance = node.appearance;
  const roleColor = appearance?.colorRole
    ? options.resolveColorRole?.(appearance.colorRole, style)
    : undefined;
  const color = appearance?.hasColorOverride && appearance.color
    ? new Color().setRGB(appearance.color.r, appearance.color.g, appearance.color.b)
    : roleColor;
  const alpha = appearance?.hasColorOverride && appearance.color ? appearance.color.a : 1;
  const material = new MeshStandardMaterial({
    color: color ?? 0xffffff,
    transparent: alpha < 1,
    opacity: alpha,
  });
  return new Mesh(geometry, material);
}

function degreesToRadians(degrees) {
  return degrees * Math.PI / 180;
}

function reconcileChildren(currentParent, nextParent, currentDefinitions, nextDefinitions, reuseMaterials) {
  const existing = new Map();
  currentParent.traverse(object => {
    const key = objectIdentityKey(object);
    if (key) existing.set(key, object);
  });
  const nextChildren = nextParent.children.map(child => materializeObject(
    child, existing, currentDefinitions, nextDefinitions, reuseMaterials,
  ));
  const unused = new Set(existing.values());
  for (const object of unused) {
    if (!object.parent || !unused.has(object.parent)) disposeObjectTree(object);
  }
  currentParent.clear();
  if (nextChildren.length > 0) currentParent.add(...nextChildren);
}

function materializeObject(next, existing, currentDefinitions, nextDefinitions, reuseMaterials) {
  const key = objectIdentityKey(next);
  const current = key ? existing.get(key) : undefined;
  let result = next;
  if (current && current.constructor === next.constructor) {
    existing.delete(key);
    current.parent?.remove(current);
    syncObject(
      current,
      next,
      currentDefinitions.get(key.slice('node:'.length)),
      nextDefinitions.get(key.slice('node:'.length)),
      reuseMaterials,
    );
    result = current;
  }

  const children = next.children.map(child => materializeObject(
    child, existing, currentDefinitions, nextDefinitions, reuseMaterials,
  ));
  result.clear();
  if (children.length > 0) result.add(...children);
  return result;
}

function syncObject(current, next, currentDefinition, nextDefinition, reuseMaterials) {
  current.name = next.name;
  current.position.copy(next.position);
  current.rotation.copy(next.rotation);
  current.scale.copy(next.scale);
  current.userData = next.userData;
  if (current instanceof Mesh) {
    if (geometrySignature(currentDefinition) === geometrySignature(nextDefinition)) {
      next.geometry.dispose();
    } else {
      current.geometry.dispose();
      current.geometry = next.geometry;
    }
    if (reuseMaterials && jsonSignature(currentDefinition?.appearance) === jsonSignature(nextDefinition?.appearance)) {
      disposeMaterial(next.material);
    } else {
      disposeMaterial(current.material);
      current.material = next.material;
    }
  }
}

function disposeObjectTree(object) {
  if (object instanceof Mesh) {
    object.geometry.dispose();
    disposeMaterial(object.material);
  }
  object.children.forEach(disposeObjectTree);
}

function disposeMaterial(material) {
  const materials = Array.isArray(material) ? material : [material];
  materials.forEach(item => item.dispose());
}

function objectIdentityKey(object) {
  const metadata = object.userData?.sceneReplay;
  if (metadata?.id) return `node:${metadata.id}`;
  if (metadata?.anchorId) return `anchor:${metadata.anchorId}`;
  return undefined;
}

function collectDefinitionNodes(definition) {
  const nodes = new Map();
  const visit = node => {
    nodes.set(node.id, node);
    node.children?.forEach(visit);
  };
  visit(definition.root);
  return nodes;
}

function geometrySignature(node) {
  return jsonSignature(node && {
    type: node.type,
    parameters: node.parameters,
    profile: node.profile,
    path: node.path,
    profileSections: node.profileSections,
    profileCageSections: node.profileCageSections,
  });
}

function jsonSignature(value) {
  return JSON.stringify(value);
}

function cloneJson(value) {
  return JSON.parse(JSON.stringify(value));
}
