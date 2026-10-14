# Three.js consuming project

This small Vite application demonstrates consuming SceneReplay definitions outside Unity. Use the example picker to switch between a character assembly, a weapon prop, a small building, a profile geometry gallery, and a semantic rig fixture. Each scene definition is compiled through `@scenereplay/three-adapter` and rendered with Three.js; the first three also include gameplay metadata documents.

## Run locally

From this directory:

```sh
npm install
npm run dev
```

The examples cover all eleven currently supported adapter shape types, stable IDs, local transforms, palette overrides, semantic anchors, damage zones, collider rules, and LOD declarations. In the character example, use **Raise arm with semantic patch** to update a live node by stable ID, then restore it; the mesh object is reused across both edits. The profile gallery demonstrates extrusion, loft, cage, lathe, and sweep geometry. The rig fixture is shared byte-for-byte with the Godot consumer and includes a **Resolve humanoid head target** action. For other shape types, compilation fails with a path-qualified unsupported-type error instead of silently dropping geometry.
