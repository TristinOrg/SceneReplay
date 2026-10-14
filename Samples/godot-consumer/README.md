# Godot consuming project

This standalone Godot 4 project demonstrates the GDScript adapter with shared SceneReplay character, weapon, profile-gallery, and rig-conformance definitions. It creates editable `Node3D` and `MeshInstance3D` hierarchies, resolves semantic rig roles to stable targets, preserves game metadata, supports all eleven Low Poly geometry types, optionally compiles native physics colliders, and applies atomic add/remove/move/update patches while preserving compatible nodes. Run the project and press `1` for the collider-enabled character, `2` for the five-shape profile gallery, `3` to patch the character's head in place, or `4` to inspect the shared semantic rig fixture.

Open `project.godot` in Godot or run the project with `godot --path Samples/godot-consumer`. The automated headless checks run with:

```sh
godot --headless --path Samples/godot-consumer --editor --quit
godot --headless --path Samples/godot-consumer --script res://tests/run_tests.gd
```

The adapter source is maintained in [`Adapters/godot`](../../Adapters/godot); this project carries a byte-identical addon copy so it opens as a self-contained Godot project. Repository validation fails if the addon or mirrored JSON examples drift.
