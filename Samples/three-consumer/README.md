# Three.js consuming project

This small Vite application demonstrates consuming a SceneReplay definition outside Unity. It loads [`scene.json`](./scene.json), compiles its semantic hierarchy through `@scenereplay/three-adapter`, and renders the result with Three.js.

## Run locally

From this directory:

```sh
npm install
npm run dev
```

The sample intentionally uses only `core/group` and the four primitive types currently implemented by this adapter. For other shape types, compile fails with a path-qualified unsupported-type error instead of silently dropping geometry.
