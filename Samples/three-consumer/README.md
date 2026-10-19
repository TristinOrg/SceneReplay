# Three.js consuming project

This Vite application demonstrates the same engine-neutral asset definitions in a Three.js consumer. The Japanese town and inventor workbench are generated from the project's Unity Low Poly preset factories and exported with their matching palettes, so the Unity and Three.js examples start from the same authored data. The town includes six X-axis mirrored instances. The picker also includes a character, weapon, building, profile geometry gallery, and semantic rig fixture.

The screenshots below use a matched 1600 × 1000 frame, 45° field of view, camera direction, and target framing. The Unity images are rendered by the Unity Editor from the same preset factories that produce the JSON and palettes used by the Three.js consumer; each engine uses its own renderer and lighting. Unity images are horizontally corrected after rendering to account for the different camera handedness, without changing the authored model transforms.

| Shared preset | Unity | Three.js |
|---|---|---|
| Low Poly Japanese Town · 103 authored nodes | ![Low Poly Japanese Town rendered in Unity](screenshots/japanese-town-unity.png) | ![Low Poly Japanese Town rendered by the SceneReplay Three.js adapter](screenshots/japanese-town.png) |
| Inventor Workbench · 43 authored nodes | ![Inventor Workbench rendered in Unity](screenshots/inventor-workbench-unity.png) | ![Inventor Workbench rendered by the SceneReplay Three.js adapter](screenshots/inventor-workbench.png) |

## Run locally

From this directory:

```sh
npm install
npm run dev
```

The examples cover all eleven currently supported adapter shape types, stable IDs, local transforms, palette overrides, semantic anchors, damage zones, collider rules, and LOD declarations. Use **Fit view** to frame the selected asset and **Save PNG** to export the current view. In the character example, **Raise arm with semantic patch** updates a live node by stable ID, then restores it; the mesh object is reused across both edits. The profile gallery demonstrates extrusion, loft, cage, lathe, and sweep geometry. The rig fixture is shared byte-for-byte with the Godot consumer and includes a **Resolve humanoid head target** action. For other shape types, compilation fails with a path-qualified unsupported-type error instead of silently dropping geometry.
