# Three.js consuming project

This small Vite application demonstrates consuming SceneReplay definitions outside Unity. Use the example picker to switch between a character assembly, a weapon prop, and a small building. Each example has a separate shape definition and gameplay metadata document, compiled through `@scenereplay/three-adapter` and rendered with Three.js.

## Run locally

From this directory:

```sh
npm install
npm run dev
```

The examples cover all six currently supported adapter shape types, stable IDs, local transforms, palette overrides, semantic anchors, damage zones, collider rules, and LOD declarations. For other shape types, compilation fails with a path-qualified unsupported-type error instead of silently dropping geometry.
