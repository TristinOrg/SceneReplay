# SceneReplay Three.js adapter

This package compiles a `scenereplay.shape/1.0` document into a Three.js object hierarchy. SceneReplay remains the owner of stable semantic IDs, parent-child structure, and authored local transforms; Three.js owns rendering and scene interaction.

## Supported contract

- Preserves node IDs and shape types in `Object3D.userData.sceneReplay`.
- Preserves hierarchy, local position, local Euler rotation (degrees, XYZ order), and scale.
- Compiles `core/group` and the Low Poly package's cube, sphere, cylinder, and capsule types.
- Preserves direct linear color overrides and accepts a callback for resolving style palette roles.
- Rejects unknown schema versions, duplicate or missing node IDs, invalid transforms, and unsupported shape types before allocating Three.js objects.
- Rejects semantic rigs and mirrored nodes until those features have explicit Three.js mappings.

Profile, sweep, rig, gameplay metadata, and patch execution are not implemented in this first adapter increment. Its descriptor advertises only stable IDs, hierarchy, and local transforms.

## Use

Install `three` and this package in a Three.js application, then pass a parsed SceneReplay JSON document to `compileShapeDefinition`:

```ts
import { compileShapeDefinition } from '@scenereplay/three-adapter';

const sceneRoot = compileShapeDefinition(shapeDefinition, {
  resolveColorRole: (role, style) => paletteByStyle[style ?? '']?.[role],
});
scene.add(sceneRoot);
```

See [`../../Samples/three-consumer`](../../Samples/three-consumer) for a standalone consuming project.
