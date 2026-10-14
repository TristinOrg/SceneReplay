extends Node3D

const Adapter = preload("res://addons/scenereplay/scene_replay_adapter.gd")

var current_root: Node3D
var camera: Camera3D


func _ready() -> void:
	_add_camera()
	_add_lighting()
	_load_example("character", true)
	print("Press 1 for the character, or 2 for the profile geometry gallery.")


func _unhandled_key_input(event: InputEvent) -> void:
	if not event is InputEventKey or not event.pressed or event.echo:
		return
	if event.keycode == KEY_1:
		_load_example("character", true)
	elif event.keycode == KEY_2:
		_load_example("profiles", false)
	elif event.keycode == KEY_3:
		_apply_patch_demo()


func _load_example(example_name: String, with_metadata: bool) -> void:
	var file := FileAccess.open("res://examples/%s.json" % example_name, FileAccess.READ)
	if file == null:
		push_error("SceneReplay example could not be opened: %s" % example_name)
		return
	var definition: Variant = JSON.parse_string(file.get_as_text())
	var options := {"paletteRoles": {"metal.dark": Color("#39404b")}}
	if with_metadata:
		var metadata_file := FileAccess.open("res://examples/%s.game-metadata.json" % example_name, FileAccess.READ)
		if metadata_file == null:
			push_error("SceneReplay metadata could not be opened: %s" % example_name)
			return
		options.gameMetadata = JSON.parse_string(metadata_file.get_as_text())
	var result: Dictionary = Adapter.compile_shape_definition(definition, options)
	if not result.ok:
		push_error(result.error)
		return
	if current_root != null:
		current_root.queue_free()
	current_root = result.root
	add_child(current_root)
	if camera != null:
		camera.position = Vector3(3.2, 2.6, 5.5) if with_metadata else Vector3(0.0, 3.6, 10.5)
		camera.look_at(Vector3(0.0, 0.8, 0.0))
	print("SceneReplay Godot sample loaded %s with %d semantic nodes." % [example_name, _count_semantic_nodes(current_root)])


func _apply_patch_demo() -> void:
	if current_root == null or current_root.get_meta("scene_replay_definition", {}).root.id != "sample.root":
		print("Load the character with 1 before trying the patch demo.")
		return
	var patch := {
		"schema": "scenereplay.patch/1.0",
		"operations": [{
			"kind": "updateNode",
			"nodeId": "sample.head",
			"update": {
				"type": "lowpoly/cube",
				"name": "Patched Head",
				"transform": {"position": {"x": 0, "y": 1.2, "z": 0}, "eulerAngles": {"x": 0, "y": 12, "z": 0}, "scale": {"x": 0.75, "y": 0.75, "z": 0.75}}
			}
		}]
	}
	var result: Dictionary = Adapter.apply_shape_patch(current_root, patch)
	if not result.ok:
		push_error(result.error)
		return
	print("Applied semantic patch to sample.head; compatible node IDs remain in place.")


func _add_camera() -> void:
	camera = Camera3D.new()
	add_child(camera)
	camera.position = Vector3(3.2, 2.6, 5.5)
	camera.look_at(Vector3(0.0, 0.8, 0.0))


func _add_lighting() -> void:
	var light := DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-45.0, -30.0, 0.0)
	light.light_energy = 1.4
	add_child(light)
	var environment := WorldEnvironment.new()
	var scene_environment := Environment.new()
	scene_environment.background_mode = Environment.BG_SKY
	scene_environment.sky = Sky.new()
	scene_environment.sky.sky_material = ProceduralSkyMaterial.new()
	environment.environment = scene_environment
	add_child(environment)


func _count_semantic_nodes(node: Node) -> int:
	var count := 1 if node.has_meta("scene_replay") and node.get_meta("scene_replay").has("id") else 0
	for child in node.get_children():
		count += _count_semantic_nodes(child)
	return count
