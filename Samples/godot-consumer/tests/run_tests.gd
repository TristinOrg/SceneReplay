extends SceneTree

const Adapter = preload("res://addons/scenereplay/scene_replay_adapter.gd")

var failures := 0


func _init() -> void:
	call_deferred("_run")


func _run() -> void:
	_test_character_compiles_hierarchy_and_game_metadata()
	_test_weapon_compiles_capsule_geometry()
	_test_unsupported_geometry_is_rejected_with_a_path()
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


func _test_unsupported_geometry_is_rejected_with_a_path() -> void:
	var definition: Dictionary = _read_json("res://examples/character.json")
	definition.root.children[0].type = "lowpoly/profile-sweep"
	var result: Dictionary = Adapter.compile_shape_definition(definition)
	_expect(not result.ok, "unsupported profile sweep should be rejected")
	_expect(str(result.get("error", "")).contains("$.root.children[0].type"), "unsupported type error should include its source path")


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
