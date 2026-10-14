# SceneReplay Three.js adapter

This package compiles a `scenereplay.shape/1.0` document into a Three.js object hierarchy. SceneReplay remains the owner of stable semantic IDs, parent-child structure, and authored local transforms; Three.js owns rendering and scene interaction.

## Supported contract

- Preserves node IDs and shape types in `Object3D.userData.sceneReplay`.
- Preserves hierarchy, local position, local Euler rotation (degrees, XYZ order), and scale.
- Advertises the shared `game-metadata` adapter capability and keeps its versioned source document available from the compiled root.
- Compiles `core/group` and the Low Poly package's cube, sphere, cylinder, and capsule types.
- Preserves direct linear color overrides and accepts a callback for resolving style palette roles.
- Preserves versioned game metadata in `userData`, creates transformable child objects for semantic anchors, and indexes damage zones and collider rules on their target nodes.
- Rejects unknown schema versions, duplicate or missing node IDs, invalid transforms, and unsupported shape types before allocating Three.js objects.
- Rejects semantic rigs and mirrored nodes until those features have explicit Three.js mappings.

Profile and sweep geometry, semantic rigs, and patch execution are not implemented yet. Collider rules and LOD levels are preserved as declarative metadata; this adapter does not supply a physics engine or generate alternate LOD meshes.

## Use

Install `three` and this package in a Three.js application, then pass a parsed SceneReplay JSON document to `compileShapeDefinition`:

```ts
import { compileShapeDefinition } from '@scenereplay/three-adapter';

const sceneRoot = compileShapeDefinition(shapeDefinition, {
  resolveColorRole: (role, style) => paletteByStyle[style ?? '']?.[role],
  gameMetadata,
});
scene.add(sceneRoot);
```

Call `applyGameMetadata(sceneRoot, gameMetadata)` instead when attaching metadata to an already compiled hierarchy. Anchors become child `Group` objects under their target nodes; damage zones, collider rules, LOD rules, and tags remain available under `userData.sceneReplay`.

See [`../../Samples/three-consumer`](../../Samples/three-consumer) for a standalone consuming project.
