export function applyPatchToDefinition(source, patch) {
  if (patch?.schema !== 'scenereplay.patch/1.0') {
    throw new Error(`Unsupported patch schema '${patch?.schema ?? ''}'.`);
  }
  if (!Array.isArray(patch.operations)) throw new Error('Patch operations must be an array.');

  const definition = cloneJson(source);
  for (let index = 0; index < patch.operations.length; index++) {
    const operation = patch.operations[index];
    const path = `$.operations[${index}]`;
    if (!operation || typeof operation !== 'object') throw new Error(`${path}: operation is required.`);
    switch (operation.kind) {
      case 'addNode': addNode(definition.root, operation, path); break;
      case 'removeNode': removeNode(definition.root, operation, path); break;
      case 'moveNode': moveNode(definition.root, operation, path); break;
      case 'updateNode': updateNode(definition.root, operation, path); break;
      default: throw new Error(`${path}.kind: unsupported patch operation '${operation.kind ?? ''}'.`);
    }
  }
  return definition;
}

function addNode(root, operation, path) {
  if (!operation.node || typeof operation.node !== 'object') throw new Error(`${path}.node: add operation requires a node.`);
  const parent = requireNode(root, operation.parentId, `${path}.parentId`);
  const existingIds = collectIds(root);
  visit(operation.node, node => {
    if (!node.id || existingIds.has(node.id)) throw new Error(`${path}.node: duplicate or missing node ID '${node.id ?? ''}'.`);
    existingIds.add(node.id);
  });
  const children = parent.children ??= [];
  insert(children, cloneJson(operation.node), operation.siblingIndex ?? -1, path);
}

function removeNode(root, operation, path) {
  const target = requireNode(root, operation.nodeId, `${path}.nodeId`);
  if (target === root) throw new Error(`${path}: the root node cannot be removed.`);
  const parent = findParent(root, target);
  if (!parent) throw new Error(`${path}.nodeId: could not find the parent of '${target.id}'.`);
  parent.children.splice(parent.children.indexOf(target), 1);
}

function moveNode(root, operation, path) {
  const target = requireNode(root, operation.nodeId, `${path}.nodeId`);
  if (target === root) throw new Error(`${path}: the root node cannot be moved.`);
  const destination = requireNode(root, operation.parentId, `${path}.parentId`);
  if (target === destination || findNode(target, destination.id)) {
    throw new Error(`${path}: moving '${target.id}' below '${destination.id}' would create a cycle.`);
  }
  const source = findParent(root, target);
  if (!source) throw new Error(`${path}.nodeId: could not find the parent of '${target.id}'.`);
  source.children.splice(source.children.indexOf(target), 1);
  const children = destination.children ??= [];
  insert(children, target, operation.siblingIndex ?? -1, path);
}

function updateNode(root, operation, path) {
  const target = requireNode(root, operation.nodeId, `${path}.nodeId`);
  if (!operation.update || typeof operation.update !== 'object') throw new Error(`${path}.update: update operation requires authored values.`);
  for (const key of [
    'name', 'type', 'transform', 'appearance', 'mirrorAxis', 'parameters',
    'profile', 'path', 'profileSections', 'profileCageSections',
  ]) {
    if (operation.update[key] !== undefined && operation.update[key] !== null) {
      target[key] = cloneJson(operation.update[key]);
    }
  }
}

function insert(children, node, requestedIndex, path) {
  const index = requestedIndex === -1 ? children.length : requestedIndex;
  if (!Number.isInteger(index) || index < 0 || index > children.length) {
    throw new Error(`${path}.siblingIndex: index ${requestedIndex} is outside the destination range.`);
  }
  children.splice(index, 0, node);
}

function requireNode(root, id, path) {
  if (typeof id !== 'string' || !id.trim()) throw new Error(`${path}: stable node ID is required.`);
  const node = findNode(root, id);
  if (!node) throw new Error(`${path}: unknown shape node ID '${id}'.`);
  return node;
}

function findNode(root, id) {
  if (root?.id === id) return root;
  for (const child of root?.children ?? []) {
    const result = findNode(child, id);
    if (result) return result;
  }
  return undefined;
}

function findParent(root, target) {
  for (const child of root?.children ?? []) {
    if (child === target) return root;
    const result = findParent(child, target);
    if (result) return result;
  }
  return undefined;
}

function collectIds(root) {
  const ids = new Set();
  visit(root, node => ids.add(node.id));
  return ids;
}

function visit(root, callback) {
  callback(root);
  root.children?.forEach(child => visit(child, callback));
}

function cloneJson(value) {
  return JSON.parse(JSON.stringify(value));
}
