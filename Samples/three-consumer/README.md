# Three.js consuming project

This small Vite application demonstrates consuming a SceneReplay definition outside Unity. It loads [`scene.json`](./scene.json) and [`game-metadata.json`](./game-metadata.json), compiles the semantic hierarchy and gameplay metadata through `@scenereplay/three-adapter`, and renders the result with Three.js.

## Run locally

From this directory:

```sh
npm install
npm run dev
```

The sample uses `core/group`, the four standard primitives, a wedge, and a parameterized frustum. For other shape types, compile fails with a path-qualified unsupported-type error instead of silently dropping geometry.
