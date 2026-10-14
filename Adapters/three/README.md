# SceneReplay Three.js adapter

This package compiles a `scenereplay.shape/1.0` document into a Three.js object hierarchy. SceneReplay remains the owner of stable semantic IDs, parent-child structure, and authored local transforms; Three.js owns rendering and scene interaction.

## Supported contract

- Preserves node IDs and shape types in `Object3D.userData.sceneReplay`.
- Preserves hierarchy, local position, local Euler rotation (degrees, XYZ order), and scale.
- Expands authored `mirrorAxis` values (`x`, `y`, or `z`) into stable sibling instances with reflected local transforms and IDs such as `part.left.mirror-x`.
- Advertises the shared `game-metadata` adapter capability and keeps its versioned source document available from the compiled root.
- Applies `scenereplay.patch/1.0` documents atomically and preserves Three.js object identity for nodes whose stable ID and native object class remain compatible.
- Compiles all eleven current Low Poly geometry types: cube, sphere, cylinder, capsule, wedge, frustum, extruded profile, profile loft, profile cage, lathe profile, and profile sweep.
- Validates 2D contours, section order and winding, sweep paths, shape parameters, and generated vertex budgets before creating geometry.
- Preserves direct linear color overrides and accepts a callback for resolving style palette roles.
- Preserves versioned game metadata in `userData`, creates transformable child objects for semantic anchors, and indexes damage zones and collider rules on their target nodes.
- Rejects unknown schema versions, duplicate or missing node IDs, invalid transforms, and unsupported shape types before allocating Three.js objects.
- Resolves semantic rig roles to stable `Object3D` targets and preserves authored rest poses and optional rotation limits in `userData.sceneReplay.rigJoints`.

Rig roles expose transform targets for an external animation system; this adapter does not create a `Skeleton` or play animation. Collider rules and LOD levels are preserved as declarative metadata; this adapter does not supply a physics engine or generate alternate LOD meshes. The profile sweep follows Three.js `ExtrudeGeometry` path behavior and does not provide a parallel-transport frame for tight turns. A node whose authored type changes between a Three.js `Group` and `Mesh` must be replaced because those native classes are not interchangeable.

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

Apply edits produced by SceneReplay Core with the patch adapter. The compiled root retains the source definition needed to validate the resulting document before changing the live hierarchy:

```ts
import { applyShapePatch } from '@scenereplay/three-adapter';

applyShapePatch(sceneRoot, {
  schema: 'scenereplay.patch/1.0',
  operations: [{
    kind: 'updateNode',
    nodeId: 'robot.left-arm',
    parentId: '',
    siblingIndex: -1,
    update: { transform: nextArmTransform },
  }],
});
```

If validation fails, the live scene remains untouched. Compatible nodes retain their `Object3D` identity, so existing application references continue to work across edits. The consumer sample includes a button that moves an arm through this API.

See [`../../Samples/three-consumer`](../../Samples/three-consumer) for a standalone consuming project.

The consumer includes the Unity-authored Japanese Town and Inventor Workbench presets as well as character, weapon, building, and profile geometry galleries. Run its tests and production build with `npm test` and `npm run build` from that directory.
