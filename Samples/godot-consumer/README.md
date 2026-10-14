# Godot consuming project

This standalone Godot 4 project demonstrates the GDScript adapter with the shared SceneReplay character and weapon definitions. It creates editable `Node3D` and `MeshInstance3D` hierarchies, preserves stable IDs and game metadata, and exercises all six geometry types currently supported by the Godot adapter.

Open `project.godot` in Godot or run the project with `godot --path Samples/godot-consumer`. The automated headless checks run with:

```sh
godot --headless --path Samples/godot-consumer --editor --quit
godot --headless --path Samples/godot-consumer --script res://tests/run_tests.gd
```

The adapter source is maintained in [`Adapters/godot`](../../Adapters/godot); this project carries a byte-identical addon copy so it opens as a self-contained Godot project. Repository validation fails if the addon or mirrored JSON examples drift.
