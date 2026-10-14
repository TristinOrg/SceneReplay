# SceneReplay Godot adapter

This GDScript adapter compiles `scenereplay.shape/1.0` into a native Godot 4 `Node3D` hierarchy. It is an early, intentionally bounded integration: supported geometry fails fast when a definition asks for a type the adapter does not implement.

## Supported contract

- Preserves stable node IDs, names, hierarchy, local transforms, and shape types in `scene_replay` node metadata.
- Compiles cube, sphere, cylinder, capsule, wedge, and frustum shapes into native Godot meshes.
- Preserves direct RGBA overrides and resolves palette roles from the `paletteRoles` option.
- Preserves game tags, anchors, damage zones, collider descriptions, and LOD declarations as node metadata.
- Validates schema versions, IDs, transforms, colors, frustum dimensions, and metadata bindings before creating nodes.
- Rejects profile geometries, semantic rigs, and mirror transforms with path-qualified errors.

Collider and LOD metadata remain descriptive; this increment does not create physics bodies or LOD switching behavior. Patch application and profile geometry are future adapter work.

## Install in a Godot project

Copy `addons/scenereplay/scene_replay_adapter.gd` into the consuming project's `res://addons/scenereplay/` directory, then preload it:

```gdscript
const SceneReplayAdapter = preload("res://addons/scenereplay/scene_replay_adapter.gd")

var result: Dictionary = SceneReplayAdapter.compile_shape_definition(shape_definition, {
	"gameMetadata": game_metadata,
	"paletteRoles": {"metal.dark": Color("#39404b")},
})
if not result.ok:
	push_error(result.error)
else:
	add_child(result.root)
```

The result dictionary contains `ok`, `root` on success, or a source-aware `error` on failure. See [`../../Samples/godot-consumer`](../../Samples/godot-consumer) for a runnable project and headless tests.

## Run the consumer

Open `Samples/godot-consumer/project.godot` in Godot 4. The project renders the character definition and prints the number of compiled semantic nodes. To validate it from a terminal:

```sh
godot --headless --path Samples/godot-consumer --editor --quit
godot --headless --path Samples/godot-consumer --script res://tests/run_tests.gd
```

The project and adapter are tested with Godot 4.7.2 in GitHub Actions. Its project assets mirror the Three.js consumer examples; repository validation checks that the copies stay in sync.
