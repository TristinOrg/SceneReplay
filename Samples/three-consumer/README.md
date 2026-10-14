# Three.js consuming project

This Vite application demonstrates the same engine-neutral asset definitions in a Three.js consumer. The Japanese town and inventor workbench are generated from the project's Unity Low Poly preset factories and exported with their matching palettes, so the Unity and Three.js examples start from the same authored data. The town includes six X-axis mirrored instances. The picker also includes a character, weapon, building, profile geometry gallery, and semantic rig fixture.

The checked-in screenshots below are captured from the running Three.js consumer. They show the same SceneReplay definitions that the Unity presets produce; Three.js applies its own renderer and camera.

| Low Poly Japanese Town · 103 authored nodes | Inventor Workbench · 43 authored nodes |
|---|---|
| ![Low Poly Japanese Town rendered by the SceneReplay Three.js adapter](screenshots/japanese-town.png) | ![Inventor Workbench rendered by the SceneReplay Three.js adapter](screenshots/inventor-workbench.png) |

## Run locally

From this directory:

```sh
npm install
npm run dev
```

The examples cover all eleven currently supported adapter shape types, stable IDs, local transforms, palette overrides, semantic anchors, damage zones, collider rules, and LOD declarations. Use **Fit view** to frame the selected asset and **Save PNG** to export the current view. In the character example, **Raise arm with semantic patch** updates a live node by stable ID, then restores it; the mesh object is reused across both edits. The profile gallery demonstrates extrusion, loft, cage, lathe, and sweep geometry. The rig fixture is shared byte-for-byte with the Godot consumer and includes a **Resolve humanoid head target** action. For other shape types, compilation fails with a path-qualified unsupported-type error instead of silently dropping geometry.
