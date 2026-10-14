# Three.js consuming project

This small Vite application demonstrates consuming SceneReplay definitions outside Unity. Use the example picker to switch between a character assembly, a weapon prop, a small building, and a profile geometry gallery. Each scene definition is compiled through `@scenereplay/three-adapter` and rendered with Three.js; the first three also include gameplay metadata documents.

## Run locally

From this directory:

```sh
npm install
npm run dev
```

The examples cover all eleven currently supported adapter shape types, stable IDs, local transforms, palette overrides, semantic anchors, damage zones, collider rules, and LOD declarations. In the character example, use **Raise arm with semantic patch** to update a live node by stable ID, then restore it; the mesh object is reused across both edits. The profile gallery demonstrates extrusion, loft, cage, lathe, and sweep geometry. For other shape types, compilation fails with a path-qualified unsupported-type error instead of silently dropping geometry.
