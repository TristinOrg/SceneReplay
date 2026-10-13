# SceneReplay Specification 1.0

SceneReplay documents describe models and styles independently from a game engine, renderer, programming language, or AI provider.

## Document identifiers

- Shape document: `scenereplay.shape/1.0`
- Style document: `scenereplay.style/1.0`

Readers must reject an unsupported major version. Minor versions may add optional fields without changing existing semantics.

## Transform convention

- Distance unit: meter.
- Axis meanings: positive X is right, positive Y is up, positive Z is forward.
- Rotation unit: degree.
- Euler application order: Z, then X, then Y.
- Transform values are local to the parent node.
- Scale is dimensionless and defaults to `[1, 1, 1]`.

Adapters are responsible for converting these semantics to the native axes, handedness, rotation representation, and units of their engine.

## Color convention

- Colors use linear RGBA components.
- Components are normalized to the inclusive range `[0, 1]`.
- A direct node color overrides its semantic palette role.

## Identity and hierarchy

- Every node has a non-empty ID unique within its shape document.
- Animation and external documents target nodes by ID, never by display name or hierarchy path.
- `core/group` creates hierarchy without requiring visible geometry.
- Other type identifiers are owned by implementation packages.

## Shape parameters

- A node may contain a `parameters` object with finite numeric values.
- Parameter names and meanings are owned by the node's shape type implementation.
- Parameters must describe engine-independent geometry and must not contain native engine object references.
- Readers must preserve unknown parameters so documents remain portable across adapters.

## Extensibility

Shape and style documents are the portable contract. Engine adapters may expose language-specific interfaces, but must preserve document semantics and stable node identity.
