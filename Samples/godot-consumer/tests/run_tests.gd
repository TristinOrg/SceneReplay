extends SceneTree

const Adapter = preload("res://addons/scenereplay/scene_replay_adapter.gd")

var failures := 0


func _init() -> void:
	call_deferred("_run")


func _run() -> void:
	_test_character_compiles_hierarchy_and_game_metadata()
	_test_weapon_compiles_capsule_geometry()
	_test_all_profile_geometry_compiles()
	_test_invalid_profile_geometry_is_rejected_with_a_path()
	_test_unresolved_metadata_binding_is_rejected()
	if failures == 0:
		print("Godot adapter checks passed.")
	quit(0 if failures == 0 else 1)


func _test_character_compiles_hierarchy_and_game_metadata() -> void:
	var definition: Dictionary = _read_json("res://examples/character.json")
	var metadata: Dictionary = _read_json("res://examples/character.game-metadata.json")
	var result: Dictionary = Adapter.compile_shape_definition(definition, {"gameMetadata": metadata})
	_expect(result.ok, "character sample should compile")
	if not result.ok:
		return
	var root: Node3D = result.root
	var nodes := {}
	_collect_nodes(root, nodes)
	for node_id in ["sample.root", "sample.wedge", "sample.frustum", "sample.body", "sample.head", "sample.arm"]:
		_expect(nodes.has(node_id), "compiled hierarchy should contain stable ID %s" % node_id)
	_expect(nodes["sample.body"] is MeshInstance3D, "body should compile to MeshInstance3D")
	_expect(nodes["sample.body"].get_meta("scene_replay_damage_zones", []).size() == 1, "damage zone metadata should bind to the body")
	_expect(root.get_meta("scene_replay_tags").has("character"), "root gameplay tags should be preserved")
	var anchor_found := false
	for child in nodes["sample.body"].get_children():
		if child.has_meta("scene_replay_anchor") and child.get_meta("scene_replay_anchor").role == "game/weapon-socket":
			anchor_found = true
	_expect(anchor_found, "weapon socket should compile as a metadata-bearing child Node3D")
	root.free()


func _test_weapon_compiles_capsule_geometry() -> void:
	var result: Dictionary = Adapter.compile_shape_definition(_read_json("res://examples/weapon.json"))
	_expect(result.ok, "weapon sample should compile")
	if not result.ok:
		return
	var nodes := {}
	_collect_nodes(result.root, nodes)
	_expect(nodes.has("weapon.handle"), "weapon hierarchy should contain the stable handle ID")
	if not nodes.has("weapon.handle"):
		result.root.free()
		return
	_expect(nodes["weapon.handle"] is MeshInstance3D, "capsule handle should compile to MeshInstance3D")
	_expect(nodes["weapon.handle"].mesh is CapsuleMesh, "capsule handle should use CapsuleMesh")
	result.root.free()


func _test_all_profile_geometry_compiles() -> void:
	var result: Dictionary = Adapter.compile_shape_definition(_read_json("res://examples/profiles.json"))
	_expect(result.ok, "profile gallery should compile")
	if not result.ok:
		return
	var nodes := {}
	_collect_nodes(result.root, nodes)
	for node_id in ["profiles.extruded", "profiles.loft", "profiles.cage", "profiles.lathe", "profiles.sweep"]:
		_expect(nodes.has(node_id), "profile gallery should contain %s" % node_id)
		if nodes.has(node_id):
			_expect(nodes[node_id] is MeshInstance3D, "%s should compile as a mesh node" % node_id)
			_expect(nodes[node_id].mesh is ArrayMesh and nodes[node_id].mesh.get_surface_count() == 1, "%s should produce a non-empty native mesh" % node_id)
	result.root.free()
	var smooth_definition: Dictionary = _read_json("res://examples/profiles.json")
	smooth_definition.root.children[0].parameters.smoothNormals = 1
	result = Adapter.compile_shape_definition(smooth_definition)
	_expect(result.ok, "profile gallery should compile with smooth normals enabled")
	if result.ok:
		var surface_arrays: Array = result.root.get_child(0).get_child(0).mesh.surface_get_arrays(0)
		_expect(surface_arrays[Mesh.ARRAY_INDEX].size() > 0, "smooth profile mesh should use indexed shared vertices")
		result.root.free()


func _test_invalid_profile_geometry_is_rejected_with_a_path() -> void:
	var definition: Dictionary = _read_json("res://examples/profiles.json")
	definition.root.children[2].profileCageSections[1].profile.reverse()
	var result: Dictionary = Adapter.compile_shape_definition(definition)
	_expect(not result.ok, "profile cage sections with a changed winding should be rejected")
	_expect(str(result.get("error", "")).contains("$.root.children[2].profileCageSections[1].profile"), "profile error should include its source path")
	definition = _read_json("res://examples/profiles.json")
	definition.root.children[4].path[1] = definition.root.children[4].path[0].duplicate(true)
	result = Adapter.compile_shape_definition(definition)
	_expect(not result.ok, "profile sweep with a repeated path point should be rejected")
	_expect(str(result.get("error", "")).contains("$.root.children[4].path[1]"), "path error should include its source path")


func _test_unresolved_metadata_binding_is_rejected() -> void:
	var definition: Dictionary = _read_json("res://examples/character.json")
	var metadata: Dictionary = _read_json("res://examples/character.game-metadata.json")
	metadata.anchors[0].nodeId = "missing.node"
	var result: Dictionary = Adapter.compile_shape_definition(definition, {"gameMetadata": metadata})
	_expect(not result.ok, "unresolved gameplay metadata should be rejected")
	_expect(str(result.get("error", "")).contains("unknown shape node"), "metadata error should identify the unresolved binding")


func _read_json(path: String) -> Dictionary:
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		_expect(false, "could not read %s" % path)
		return {}
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if not parsed is Dictionary:
		_expect(false, "expected JSON object in %s" % path)
		return {}
	return parsed


func _collect_nodes(node: Node, nodes: Dictionary) -> void:
	if node.has_meta("scene_replay") and node.get_meta("scene_replay").has("id"):
		nodes[node.get_meta("scene_replay").id] = node
	for child in node.get_children():
		_collect_nodes(child, nodes)


func _expect(condition: bool, message: String) -> void:
	if not condition:
		failures += 1
		push_error("FAIL: %s" % message)
