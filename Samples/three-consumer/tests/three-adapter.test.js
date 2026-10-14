import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { Group, Mesh } from 'three';
import { compileShapeDefinition, adapterDescriptor } from '@scenereplay/three-adapter';

const readJson = async relativePath => JSON.parse(await readFile(new URL(relativePath, import.meta.url), 'utf8'));

test('descriptor advertises the supported adapter contract', () => {
  assert.deepEqual(adapterDescriptor.features, [
    'stable-node-ids', 'hierarchy', 'local-transforms', 'game-metadata',
  ]);
  assert.equal(adapterDescriptor.engineId, 'three');
});

test('consumer example compiles all six supported shapes and game metadata', async () => {
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
