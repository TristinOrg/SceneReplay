extends Node3D

const Adapter = preload("res://addons/scenereplay/scene_replay_adapter.gd")


func _ready() -> void:
	var file := FileAccess.open("res://examples/character.json", FileAccess.READ)
	var metadata_file := FileAccess.open("res://examples/character.game-metadata.json", FileAccess.READ)
	if file == null or metadata_file == null:
		push_error("SceneReplay example files could not be opened.")
		return
	var definition: Variant = JSON.parse_string(file.get_as_text())
	var metadata: Variant = JSON.parse_string(metadata_file.get_as_text())
	var result: Dictionary = Adapter.compile_shape_definition(definition, {
		"gameMetadata": metadata,
		"paletteRoles": {"metal.dark": Color("#39404b")},
	})
	if not result.ok:
		push_error(result.error)
		return
	add_child(result.root)
	_add_camera()
	_add_lighting()
	print("SceneReplay Godot sample loaded %d semantic nodes." % _count_semantic_nodes(result.root))


func _add_camera() -> void:
	var camera := Camera3D.new()
	camera.position = Vector3(3.2, 2.6, 5.5)
	add_child(camera)
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
