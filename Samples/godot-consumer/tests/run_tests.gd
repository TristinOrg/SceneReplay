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
	_test_semantic_patch_is_atomic_and_preserves_node_identity()
	_test_native_colliders_are_opt_in_and_engine_native()
	_test_semantic_rig_roles_resolve_to_stable_nodes()
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
	_expect(nodes["sample.body"].get_node_or_null("Collider_sample_body-collider") == null, "colliders should remain metadata-only by default")
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


func _test_semantic_patch_is_atomic_and_preserves_node_identity() -> void:
	var definition: Dictionary = _read_json("res://examples/character.json")
	var metadata: Dictionary = _read_json("res://examples/character.game-metadata.json")
	var result: Dictionary = Adapter.compile_shape_definition(definition, {"gameMetadata": metadata})
	_expect(result.ok, "character should compile before patching")
	if not result.ok:
		return
	var root: Node3D = result.root
	var before := {}
	_collect_nodes(root, before)
	var body: Node = before["sample.body"]
	var arm: Node = before["sample.arm"]
	var head: Node = before["sample.head"]
	var body_mesh: Mesh = before["sample.body"].mesh
	var head_mesh: Mesh = before["sample.head"].mesh
	_expect(Adapter.ADAPTER_DESCRIPTOR.features.has("patch-application"), "adapter descriptor should advertise patch support")
	var patch := {
		"schema": "scenereplay.patch/1.0",
		"operations": [
			{"kind": "updateNode", "nodeId": "sample.head", "update": {"name": "Updated Head", "type": "lowpoly/cube", "transform": {"position": {"x": 0.1, "y": 1.3, "z": 0}, "eulerAngles": {"x": 0, "y": 0, "z": 0}, "scale": {"x": 0.75, "y": 0.75, "z": 0.75}}}},
			{"kind": "addNode", "parentId": "sample.root", "siblingIndex": -1, "node": {"id": "sample.extra", "name": "Extra", "type": "lowpoly/cube", "transform": {"position": {"x": 2, "y": 0, "z": 0}, "eulerAngles": {"x": 0, "y": 0, "z": 0}, "scale": {"x": 0.25, "y": 0.25, "z": 0.25}}}},
			{"kind": "moveNode", "nodeId": "sample.arm", "parentId": "sample.root", "siblingIndex": -1},
			{"kind": "removeNode", "nodeId": "sample.wedge"}
		]
	}
	result = Adapter.apply_shape_patch(root, patch)
	_expect(result.ok and result.root == root, "valid patch should update and return the same compiled root")
	var after := {}
	_collect_nodes(root, after)
	_expect(after["sample.body"] == body and after["sample.arm"] == arm, "patch should preserve compatible Godot node instances")
	_expect(after["sample.head"] == head and after["sample.head"].mesh is BoxMesh and after["sample.head"].mesh != head_mesh, "shape changes should preserve node identity and replace incompatible geometry")
	_expect(after["sample.body"].mesh == body_mesh, "unchanged geometry should retain its native mesh resource")
	_expect(after.has("sample.extra") and not after.has("sample.wedge"), "patch should add and remove the requested semantic nodes")
	_expect(after["sample.arm"].get_parent().get_meta("scene_replay").id == "sample.root", "move operation should update the native parent")
	_expect(after["sample.head"].name == "Updated Head", "update operation should change authored fields")
	_expect(after["sample.body"].get_meta("scene_replay_damage_zones", []).size() == 1, "patching should preserve bound gameplay metadata")
	var failed := Adapter.apply_shape_patch(root, {"schema": "scenereplay.patch/1.0", "operations": [
		{"kind": "updateNode", "nodeId": "sample.head", "update": {"name": "Should Roll Back"}},
		{"kind": "removeNode", "nodeId": "missing.node"}
	]})
	_expect(not failed.ok and str(failed.error).contains("missing.node"), "invalid patch should return a source-aware error")
	var after_failure := {}
	_collect_nodes(root, after_failure)
	_expect(after_failure["sample.body"] == body and after_failure.has("sample.extra") and after_failure["sample.head"].name == "Updated Head", "failed patch must leave the compiled hierarchy unchanged")
	root.free()


func _test_native_colliders_are_opt_in_and_engine_native() -> void:
	var definition: Dictionary = _read_json("res://examples/character.json")
	var metadata: Dictionary = _read_json("res://examples/character.game-metadata.json")
	metadata.colliders.append({
		"id": "sample.head-trigger",
		"nodeId": "sample.head",
		"kind": "sphere",
		"isTrigger": true,
		"center": {"x": 0, "y": 0, "z": 0},
		"size": {"x": 1, "y": 1, "z": 1},
		"radius": 0.4,
		"height": 0.8
	})
	var result: Dictionary = Adapter.compile_shape_definition(definition, {"gameMetadata": metadata, "compileColliders": true})
	_expect(result.ok, "native collider option should compile supported metadata")
	if not result.ok:
		return
	var nodes := {}
	_collect_nodes(result.root, nodes)
	var body_collision: Node = nodes["sample.body"].get_node_or_null("Collider_sample_body-collider")
	var head_trigger: Node = nodes["sample.head"].get_node_or_null("Collider_sample_head-trigger")
	_expect(body_collision is StaticBody3D, "non-trigger box should compile to StaticBody3D")
	_expect(body_collision != null and body_collision.get_node("Shape").shape is BoxShape3D, "box collider should use BoxShape3D")
	_expect(head_trigger is Area3D, "trigger sphere should compile to Area3D")
	_expect(head_trigger != null and head_trigger.get_node("Shape").shape is SphereShape3D, "sphere trigger should use SphereShape3D")
	_expect(result.root.get_meta("scene_replay_native_collider_count") == 2, "native collider count should be discoverable on the compiled root")
	result.root.free()


func _test_semantic_rig_roles_resolve_to_stable_nodes() -> void:
	var definition: Dictionary = _read_json("res://examples/rigged-character.json")
	var result: Dictionary = Adapter.compile_shape_definition(definition)
	_expect(result.ok, "semantic rig should compile against stable shape node IDs")
	if not result.ok:
		return
	var target: Node3D = Adapter.get_rig_target(result.root, "humanoid/head")
	_expect(target != null and target.get_meta("scene_replay").id == "fixture.head", "rig role lookup should resolve to the native target node")
	_expect(target != null and target.get_meta("scene_replay_rig_joints", [])[0].rotationConstraint.maximum.y == 45, "rotation constraints should be preserved on the target metadata")
	_expect(Adapter.get_rig_target(result.root, "humanoid/right-hand").get_meta("scene_replay").id == "fixture.hand", "the shared fixture should resolve its second rig role too")
	_expect(Adapter.ADAPTER_DESCRIPTOR.features.has("semantic-rig"), "adapter descriptor should advertise semantic-rig support")
	result.root.free()
	definition.rig.joints.append(definition.rig.joints[0].duplicate(true))
	result = Adapter.compile_shape_definition(definition)
	_expect(not result.ok and str(result.error).contains("duplicate rig role"), "duplicate rig roles should be rejected with a useful diagnostic")


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
