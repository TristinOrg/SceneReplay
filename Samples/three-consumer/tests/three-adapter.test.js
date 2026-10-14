import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { Color, Euler, Group, Mesh, Quaternion } from 'three';
import { applyShapePatch, compileShapeDefinition, getRigTarget, adapterDescriptor } from '@scenereplay/three-adapter';

const readJson = async relativePath => JSON.parse(await readFile(new URL(relativePath, import.meta.url), 'utf8'));

test('descriptor advertises the supported adapter contract', () => {
  assert.deepEqual(adapterDescriptor.features, [
    'stable-node-ids', 'hierarchy', 'local-transforms', 'mirrored-instances', 'game-metadata', 'semantic-rig', 'patch-application',
  ]);
  assert.equal(adapterDescriptor.engineId, 'three');
});

test('mirror axes create Unity-compatible sibling instances and retain them through semantic patches', async () => {
  const definition = await readJson('../scene.json');
  const armDefinition = definition.root.children[2].children.find(node => node.id === 'sample.arm');
  armDefinition.mirrorAxis = 'x';
  armDefinition.transform.eulerAngles = { x: 12, y: 24, z: -65 };
  const root = compileShapeDefinition(definition);
  const objectFor = id => {
    let match;
    root.traverse(object => {
      if (object.userData.sceneReplay?.id === id) match = object;
    });
    return match;
  };
  const arm = objectFor('sample.arm');
  const mirroredArm = objectFor('sample.arm.mirror-x');
  assert.ok(arm instanceof Mesh);
  assert.ok(mirroredArm instanceof Mesh);
  assert.equal(mirroredArm.name, 'Arm (Mirror X)');
  assert.equal(mirroredArm.position.x, -arm.position.x);
  assert.equal(mirroredArm.scale.x, -arm.scale.x);
  assert.equal(mirroredArm.userData.sceneReplay.sourceId, arm.userData.sceneReplay.sourceId);

  const expectedRotation = new Quaternion().setFromEuler(new Euler(12 * Math.PI / 180, 24 * Math.PI / 180, -65 * Math.PI / 180, 'XYZ'));
  expectedRotation.set(expectedRotation.x, -expectedRotation.y, -expectedRotation.z, expectedRotation.w);
  assert.ok(mirroredArm.quaternion.angleTo(expectedRotation) < 1e-6);

  applyShapePatch(root, {
    schema: 'scenereplay.patch/1.0',
    operations: [{
      kind: 'updateNode',
      nodeId: 'sample.arm',
      update: { transform: { position: { x: 0.9, y: 0.2, z: 0 }, eulerAngles: { x: 0, y: 0, z: -65 }, scale: { x: 0.22, y: 0.42, z: 0.22 } } },
    }],
  });
  assert.equal(objectFor('sample.arm.mirror-x'), mirroredArm);
  assert.equal(mirroredArm.position.x, -0.9);
  root.traverse(object => object.geometry?.dispose());
});

test('semantic rig roles resolve to stable object targets and preserve rotation constraints', async () => {
  const definition = await readJson('../examples/rigged-character.json');
  const root = compileShapeDefinition(definition);
  const target = getRigTarget(root, 'humanoid/head');
  assert.ok(target instanceof Mesh);
  assert.equal(target.userData.sceneReplay.rigJoints[0].rotationConstraint.maximum.y, 45);
  assert.equal(root.userData.sceneReplay.rig.type, 'game/humanoid');
  assert.equal(getRigTarget(root, 'humanoid/right-hand')?.userData.sceneReplay.id, 'fixture.hand');

  applyShapePatch(root, {
    schema: 'scenereplay.patch/1.0',
    operations: [{ kind: 'updateNode', nodeId: 'fixture.head', siblingIndex: -1, parentId: '', update: { name: 'Rigged Head' } }],
  });
  assert.equal(getRigTarget(root, 'humanoid/head'), target);
  assert.equal(target.name, 'Rigged Head');

  definition.rig.joints.push({ ...definition.rig.joints[0] });
  assert.throws(() => compileShapeDefinition(definition), /duplicate rig role/);
  root.traverse(object => object.geometry?.dispose());
});

test('semantic patches update, add, move, and remove nodes while preserving stable object identities', async () => {
  const definition = await readJson('../scene.json');
  const metadata = await readJson('../game-metadata.json');
  const root = compileShapeDefinition(definition, { gameMetadata: metadata });
  const objectFor = id => {
    let match;
    root.traverse(object => {
      if (object.userData.sceneReplay?.id === id) match = object;
    });
    return match;
  };
  const arm = objectFor('sample.arm');
  const head = objectFor('sample.head');
  const frustum = objectFor('sample.frustum');
  const armGeometry = arm.geometry;
  const armMaterial = arm.material;
  const frustumGeometry = frustum.geometry;
  const accessory = {
    id: 'sample.accessory',
    name: 'Accessory Group',
    type: 'core/group',
    transform: {
      position: { x: 0, y: 0, z: 0 },
      eulerAngles: { x: 0, y: 0, z: 0 },
      scale: { x: 1, y: 1, z: 1 },
    },
    children: [],
  };
  applyShapePatch(root, {
    schema: 'scenereplay.patch/1.0',
    operations: [
      { kind: 'updateNode', nodeId: 'sample.arm', update: { transform: { position: { x: 0.9, y: 0.2, z: 0 }, eulerAngles: { x: 0, y: 0, z: -65 }, scale: { x: 0.22, y: 0.42, z: 0.22 } } } },
      { kind: 'addNode', parentId: 'sample.body', siblingIndex: -1, node: accessory },
      { kind: 'moveNode', nodeId: 'sample.head', parentId: 'sample.accessory', siblingIndex: -1 },
      { kind: 'addNode', parentId: 'sample.accessory', siblingIndex: -1, node: {
        id: 'sample.badge', name: 'Badge', type: 'lowpoly/cube',
        transform: { position: { x: 0, y: 0, z: 0 }, eulerAngles: { x: 0, y: 0, z: 0 }, scale: { x: 0.2, y: 0.2, z: 0.2 } },
      } },
    ],
  });

  assert.equal(objectFor('sample.arm'), arm);
  assert.equal(objectFor('sample.frustum'), frustum);
  assert.equal(frustum.geometry, frustumGeometry);
  assert.equal(arm.geometry, armGeometry);
  assert.equal(arm.material, armMaterial);
  assert.equal(objectFor('sample.head'), head);
  assert.equal(arm.position.x, 0.9);
  assert.equal(head.parent.userData.sceneReplay.id, 'sample.accessory');
  assert.ok(objectFor('sample.badge') instanceof Mesh);
  assert.equal(root.userData.sceneReplay.gameMetadata.id, metadata.id);

  applyShapePatch(root, {
    schema: 'scenereplay.patch/1.0',
    operations: [{
      kind: 'updateNode',
      nodeId: 'sample.frustum',
      parentId: '',
      siblingIndex: -1,
      update: { parameters: { topWidth: 0.7, topDepth: 0.65, bottomWidth: 1, bottomDepth: 1 } },
    }],
  });
  assert.equal(objectFor('sample.frustum'), frustum);
  assert.notEqual(frustum.geometry, frustumGeometry);

  applyShapePatch(root, {
    schema: 'scenereplay.patch/1.0',
    operations: [
      { kind: 'removeNode', nodeId: 'sample.badge' },
      { kind: 'moveNode', nodeId: 'sample.head', parentId: 'sample.body', siblingIndex: -1 },
      { kind: 'removeNode', nodeId: 'sample.accessory' },
    ],
  });
  assert.equal(objectFor('sample.head'), head);
  assert.equal(objectFor('sample.arm'), arm);
  assert.equal(head.parent.userData.sceneReplay.id, 'sample.body');
  assert.equal(objectFor('sample.badge'), undefined);
});

test('invalid semantic patches leave the compiled hierarchy unchanged', async () => {
  const root = compileShapeDefinition(await readJson('../scene.json'));
  let arm;
  root.traverse(object => {
    if (object.userData.sceneReplay?.id === 'sample.arm') arm = object;
  });
  const rootMetadata = root.userData.sceneReplay;
  assert.throws(() => applyShapePatch(root, {
    schema: 'scenereplay.patch/1.0',
    operations: [
      { kind: 'updateNode', nodeId: 'sample.arm', update: { transform: { position: { x: 2, y: 0, z: 0 }, eulerAngles: { x: 0, y: 0, z: 0 }, scale: { x: 1, y: 1, z: 1 } } } },
      { kind: 'moveNode', nodeId: 'sample.body', parentId: 'sample.head', siblingIndex: -1 },
    ],
  }), /would create a cycle/);
  assert.throws(() => applyShapePatch(root, {
    schema: 'scenereplay.patch/1.0',
    operations: [{ kind: 'updateNode', nodeId: 'sample.arm', update: { type: 'lowpoly/extruded-profile' } }],
  }), /expected at least 3 points/);
  assert.equal(arm.position.x, 0.55);
  assert.equal(root.userData.sceneReplay, rootMetadata);
});

test('character consumer example compiles its hierarchy and game metadata', async () => {
  const definition = await readJson('../scene.json');
  const gameMetadata = await readJson('../game-metadata.json');
  const root = compileShapeDefinition(definition, { gameMetadata });
  const byId = new Map();
  root.traverse(object => {
    const id = object.userData.sceneReplay?.id;
    if (id) byId.set(id, object);
  });

  assert.ok(root instanceof Group);
  for (const id of ['sample.root', 'sample.wedge', 'sample.frustum', 'sample.body', 'sample.head']) {
    assert.ok(byId.has(id), `missing compiled semantic node ${id}`);
  }
  assert.ok(byId.get('sample.body') instanceof Mesh);
  assert.ok(byId.get('sample.wedge') instanceof Mesh);
  assert.ok(byId.get('sample.frustum') instanceof Mesh);
  assert.equal(root.userData.sceneReplay.gameplayTags.includes('character'), true);
  assert.equal(root.userData.sceneReplay.lods.length, 2);
  assert.equal(byId.get('sample.body').userData.sceneReplay.damageZones[0].id, 'sample.body-zone');
  assert.equal(byId.get('sample.body').userData.sceneReplay.colliderRules[0].kind, 'box');

  const anchor = byId.get('sample.body').getObjectByName('Anchor_sample.weapon-socket');
  assert.ok(anchor instanceof Group);
  assert.equal(anchor.userData.sceneReplay.role, 'game/weapon-socket');
});

test('weapon and building examples compile with their semantic metadata', async () => {
  for (const example of ['weapon', 'building']) {
    const definition = await readJson(`../examples/${example}.json`);
    const gameMetadata = await readJson(`../examples/${example}.game-metadata.json`);
    const root = compileShapeDefinition(definition, { gameMetadata });
    assert.equal(root.userData.sceneReplay.gameMetadata.id, `${example}.gameplay`);
    assert.ok(root.userData.sceneReplay.gameplayTags.length > 0);
  }
});

test('Unity Low Poly town and workbench presets compile with their shared palettes and mirror instances', async () => {
  for (const [name, expectedNodes, expectedMirrors] of [['japanese-town', 103, 6], ['inventor-workbench', 43, 0]]) {
    const definition = await readJson(`../examples/${name}.json`);
    const style = await readJson(`../examples/${name}.style.json`);
    const colors = new Map(style.palette.entries.map(entry => [entry.role, entry.color]));
    const root = compileShapeDefinition(definition, {
      resolveColorRole: (role, styleId) => {
        const color = styleId === style.id ? colors.get(role) : undefined;
        return color ? new Color(color.r, color.g, color.b) : undefined;
      },
    });
    const authoredIds = new Set();
    let authoredNodes = 0;
    let mirrors = 0;
    let resolvedColors = 0;
    root.traverse(object => {
      const node = object.userData.sceneReplay;
      if (!node?.id) return;
      if (node.id.includes('.mirror-')) mirrors += 1;
      else {
        authoredNodes += 1;
        authoredIds.add(node.id);
      }
      if (object instanceof Mesh && object.material.color.getHex() !== 0xffffff) resolvedColors += 1;
      if (object instanceof Mesh) assert.ok(object.geometry.getAttribute('position').count > 0);
    });
    assert.equal(authoredNodes, expectedNodes);
    assert.equal(authoredIds.size, expectedNodes);
    assert.equal(mirrors, expectedMirrors);
    assert.ok(resolvedColors > 0, `${name} should resolve palette roles`);
  }
});

test('profile geometry gallery compiles all five advanced geometry types', async () => {
  const definition = await readJson('../examples/profiles.json');
  const root = compileShapeDefinition(definition);
  const expectedTypes = [
    'lowpoly/extruded-profile',
    'lowpoly/profile-loft',
    'lowpoly/profile-cage',
    'lowpoly/lathe-profile',
    'lowpoly/profile-sweep',
  ];
  const meshes = [];
  root.traverse(object => {
    if (object instanceof Mesh) meshes.push(object);
  });

  assert.equal(meshes.length, expectedTypes.length);
  assert.deepEqual(meshes.map(mesh => mesh.userData.sceneReplay.type).sort(), [...expectedTypes].sort());
  for (const mesh of meshes) {
    assert.ok(mesh.geometry.getAttribute('position').count > 0);
    mesh.geometry.computeBoundingBox();
    assert.ok(mesh.geometry.boundingBox && !mesh.geometry.boundingBox.isEmpty());
  }
});

test('profile validation rejects inconsistent cage winding and non-finite sweep paths', async () => {
  const definition = await readJson('../examples/profiles.json');
  const cage = definition.root.children[2];
  cage.profileCageSections[1].profile.reverse();
  assert.throws(() => compileShapeDefinition(definition), /same point winding/);

  const sweep = await readJson('../examples/profiles.json');
  sweep.root.children[4].path[1].y = Number.NaN;
  assert.throws(() => compileShapeDefinition(sweep), /expected finite x, y, and z values/);
});

test('consumer examples exercise every supported Low Poly type', async () => {
  const expectedTypes = new Set([
    'lowpoly/cube', 'lowpoly/sphere', 'lowpoly/cylinder', 'lowpoly/capsule',
    'lowpoly/wedge', 'lowpoly/frustum', 'lowpoly/extruded-profile',
    'lowpoly/profile-loft', 'lowpoly/profile-cage', 'lowpoly/lathe-profile',
    'lowpoly/profile-sweep',
  ]);
  const actualTypes = new Set();
  for (const file of ['../scene.json', '../examples/weapon.json', '../examples/building.json', '../examples/profiles.json']) {
    const definition = await readJson(file);
    const visit = node => {
      if (node.type.startsWith('lowpoly/')) actualTypes.add(node.type);
      node.children?.forEach(visit);
    };
    visit(definition.root);
    compileShapeDefinition(definition);
  }

  assert.deepEqual([...actualTypes].sort(), [...expectedTypes].sort());
});

test('unsupported geometry and duplicate node IDs fail with a source path', async () => {
  const unsupported = await readJson('../scene.json');
  unsupported.root.children[0].type = 'lowpoly/not-implemented';
  assert.throws(() => compileShapeDefinition(unsupported), /\$\.root\.children\[0\]\.type/);

  const duplicate = await readJson('../scene.json');
  duplicate.root.children[1].id = duplicate.root.children[0].id;
  assert.throws(() => compileShapeDefinition(duplicate), /duplicate node ID/);
});

test('invalid frustum dimensions and unresolved metadata node IDs are rejected', async () => {
  const invalidFrustum = await readJson('../scene.json');
  invalidFrustum.root.children[1].parameters.topWidth = 0;
  assert.throws(() => compileShapeDefinition(invalidFrustum), /parameters\.topWidth/);

  const definition = await readJson('../scene.json');
  const gameMetadata = await readJson('../game-metadata.json');
  gameMetadata.anchors[0].nodeId = 'missing.node';
  assert.throws(() => compileShapeDefinition(definition, { gameMetadata }), /unknown node 'missing.node'/);
});
