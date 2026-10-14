# SceneReplay Godot adapter

This GDScript adapter compiles `scenereplay.shape/1.0` into a native Godot 4 `Node3D` hierarchy. It is an early, intentionally bounded integration: supported geometry fails fast when a definition asks for a type the adapter does not implement.

## Supported contract

- Preserves stable node IDs, names, hierarchy, local transforms, and shape types in `scene_replay` node metadata.
- Compiles all eleven Low Poly shapes, including extrusion, loft, cage, lathe, and sweep profile geometry, into native Godot meshes.
- Preserves direct RGBA overrides and resolves palette roles from the `paletteRoles` option.
- Preserves game tags, anchors, damage zones, collider descriptions, and LOD declarations as node metadata.
- Applies ordered `scenereplay.patch/1.0` add/remove/move/update operations atomically and reconciles the Godot hierarchy by stable ID.
- Preserves compatible `Node3D`/`MeshInstance3D` identity and unchanged mesh resources when patching; changed geometry is rebuilt before the live tree is touched.
- Validates schema versions, IDs, transforms, colors, geometry profiles and budgets, and metadata bindings before creating nodes.
- Rejects semantic rigs and mirror transforms with path-qualified errors.

Collider and LOD metadata remain descriptive; this adapter does not create physics bodies or LOD switching behavior. Semantic rig compilation and patch rollback remain future adapter work. Profile meshes use polygon triangulation, bounded smoothing, and deterministic sampled loft/cage/sweep construction.

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
	var patch := {"schema": "scenereplay.patch/1.0", "operations": [{
		"kind": "updateNode",
		"nodeId": "sample.body",
		"update": {"name": "Revised Body"},
	}]}
	var patched := SceneReplayAdapter.apply_shape_patch(result.root, patch)
	if not patched.ok:
		push_error(patched.error)
```

The result dictionary contains `ok`, `root` on success, or a source-aware `error` on failure. See [`../../Samples/godot-consumer`](../../Samples/godot-consumer) for a runnable project and headless tests.

## Run the consumer

Open `Samples/godot-consumer/project.godot` in Godot 4. The project renders the character definition and prints the number of compiled semantic nodes. To validate it from a terminal:

```sh
godot --headless --path Samples/godot-consumer --editor --quit
godot --headless --path Samples/godot-consumer --script res://tests/run_tests.gd
```

The project and adapter are tested with Godot 4.7.2 in GitHub Actions. Its project assets mirror the Three.js consumer examples; repository validation checks that the copies stay in sync.
