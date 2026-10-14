import {
  BoxGeometry,
  CapsuleGeometry,
  Color,
  CylinderGeometry,
  Euler,
  Group,
  Mesh,
  MeshStandardMaterial,
  SphereGeometry,
} from 'three';

export const adapterDescriptor = Object.freeze({
  schema: 'scenereplay.adapter/1.0',
  adapterId: 'scenereplay/three',
  engineId: 'three',
  version: '0.1.0',
  features: Object.freeze(['stable-node-ids', 'hierarchy', 'local-transforms']),
  unsupportedFeaturePolicy: 'reject',
});

const groupType = 'core/group';
const supportedGeometryTypes = new Set([
  'lowpoly/cube',
  'lowpoly/sphere',
  'lowpoly/cylinder',
  'lowpoly/capsule',
]);

/** Compiles a supported SceneReplay hierarchy into native Three.js objects. */
export function compileShapeDefinition(definition, options = {}) {
  validateDefinition(definition);
  const root = new Group();
  root.name = definition.name;
  root.userData.sceneReplay = {
    schema: definition.schema,
    name: definition.name,
    style: definition.style ?? '',
    adapterId: adapterDescriptor.adapterId,
  };
  root.add(compileNode(definition.root, definition.style, options));
  return root;
}

function validateDefinition(definition) {
  if (definition?.schema !== 'scenereplay.shape/1.0') {
    throw new Error(`Unsupported SceneReplay schema '${definition?.schema ?? ''}'.`);
  }
  if (typeof definition.name !== 'string' || !definition.name.trim()) {
    throw new Error('Shape definition name must be a non-empty string.');
  }
  if (!definition.root) throw new Error('Shape definition root is required.');
  if (definition.rig) throw new Error('Semantic rigs are not supported by the Three.js adapter yet.');

  const ids = new Set();
  const visit = (node, path) => {
    if (!node || typeof node !== 'object') throw new Error(`${path}: node is required.`);
    if (typeof node.id !== 'string' || !node.id.trim()) throw new Error(`${path}.id: stable node ID is required.`);
    if (ids.has(node.id)) throw new Error(`${path}.id: duplicate node ID '${node.id}'.`);
    ids.add(node.id);
    if (node.type !== groupType && !supportedGeometryTypes.has(node.type)) {
      throw new Error(`${path}.type: unsupported shape type '${node.type}'.`);
    }
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
}

function validateVector(value, path) {
  if (!value || ![value.x, value.y, value.z].every(Number.isFinite)) {
    throw new Error(`${path}: expected finite x, y, and z values.`);
  }
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
  switch (node.type) {
    case groupType:
      return new Group();
    case 'lowpoly/cube':
      return createMesh(new BoxGeometry(1, 1, 1), node, style, options);
    case 'lowpoly/sphere':
      return createMesh(new SphereGeometry(0.5, 12, 8), node, style, options);
    case 'lowpoly/cylinder':
      return createMesh(new CylinderGeometry(0.5, 0.5, 2, 12), node, style, options);
    case 'lowpoly/capsule':
      return createMesh(new CapsuleGeometry(0.5, 1, 4, 8), node, style, options);
    default:
      throw new Error(`Unsupported shape type '${node.type}'.`);
  }
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
