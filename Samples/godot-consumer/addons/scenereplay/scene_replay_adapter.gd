class_name SceneReplayAdapter
extends RefCounted
## Compiles SceneReplay shape JSON into a Godot 4 Node3D hierarchy.

const SCHEMA := "scenereplay.shape/1.0"
const METADATA_SCHEMA := "scenereplay.game-metadata/1.0"
const GROUP_TYPE := "core/group"
const SUPPORTED_TYPES := [
	"lowpoly/cube",
	"lowpoly/sphere",
	"lowpoly/cylinder",
	"lowpoly/capsule",
	"lowpoly/wedge",
	"lowpoly/frustum",
]
const ADAPTER_DESCRIPTOR := {
	"schema": "scenereplay.adapter/1.0",
	"adapterId": "scenereplay/godot",
	"engineId": "godot",
	"version": "0.1.0",
	"features": ["stable-node-ids", "hierarchy", "local-transforms", "game-metadata"],
	"unsupportedFeaturePolicy": "reject",
}


static func load_shape_definition(path: String, options: Dictionary = {}) -> Dictionary:
	if not FileAccess.file_exists(path):
		return _failure("Shape file does not exist: %s" % path)
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return _failure("Could not open shape file: %s" % path)
	var parser := JSON.new()
	var parse_error := parser.parse(file.get_as_text())
	if parse_error != OK:
		return _failure("Shape JSON error at line %d: %s" % [parser.get_error_line(), parser.get_error_message()])
	if not parser.data is Dictionary:
		return _failure("Shape JSON root must be an object.")
	return compile_shape_definition(parser.data, options)


static func compile_shape_definition(definition: Dictionary, options: Dictionary = {}) -> Dictionary:
	var node_ids := {}
	var validation_error := _validate_definition(definition, node_ids)
	if not validation_error.is_empty():
		return _failure(validation_error)
	if options.has("gameMetadata"):
		validation_error = _validate_game_metadata(options.gameMetadata, node_ids)
		if not validation_error.is_empty():
			return _failure(validation_error)

	var root := Node3D.new()
	root.name = str(definition.name)
	root.set_meta("scene_replay", {
		"schema": SCHEMA,
		"name": definition.name,
		"style": str(definition.get("style", "")),
		"adapterId": ADAPTER_DESCRIPTOR.adapterId,
	})
	root.add_child(_compile_node(definition.root, options))
	if options.has("gameMetadata"):
		_attach_game_metadata(root, options.gameMetadata, node_ids)
	return {"ok": true, "root": root}


static func _validate_definition(definition: Dictionary, node_ids: Dictionary) -> String:
	if definition.get("schema", "") != SCHEMA:
		return "Unsupported SceneReplay shape schema '%s'." % str(definition.get("schema", ""))
	if not definition.get("name", "") is String or str(definition.name).strip_edges().is_empty():
		return "Shape definition name must be a non-empty string."
	if not definition.get("root") is Dictionary:
		return "Shape definition root must be an object."
	if definition.has("rig"):
		return "Semantic rigs are not supported by the Godot adapter yet."
	return _validate_node(definition.root, "$.root", node_ids)


static func _validate_node(node: Dictionary, path: String, node_ids: Dictionary) -> String:
	var node_id: Variant = node.get("id", "")
	if not node_id is String or str(node_id).strip_edges().is_empty():
		return "%s.id: stable node ID is required." % path
	if node_ids.has(node_id):
		return "%s.id: duplicate node ID '%s'." % [path, node_id]
	node_ids[node_id] = true
	var shape_type: String = str(node.get("type", ""))
	if shape_type != GROUP_TYPE and not SUPPORTED_TYPES.has(shape_type):
		return "%s.type: unsupported Godot shape type '%s'." % [path, shape_type]
	if node.has("mirrorAxis") and str(node.mirrorAxis) not in ["", "none", "None"]:
		return "%s.mirrorAxis: mirrored nodes are not supported by the Godot adapter yet." % path
	var transform: Variant = node.get("transform")
	if not transform is Dictionary:
		return "%s.transform: transform is required." % path
	for component in ["position", "eulerAngles", "scale"]:
		if not _valid_vector3(transform.get(component)):
			return "%s.transform.%s: expected finite x, y, and z values." % [path, component]
	if shape_type == "lowpoly/frustum":
		var parameters: Dictionary = node.get("parameters", {})
		for key in ["topWidth", "topDepth", "bottomWidth", "bottomDepth"]:
			var value: Variant = parameters.get(key, 0.65 if key.begins_with("top") else 1.0)
			if not _valid_number(value) or float(value) <= 0.0:
				return "%s.parameters.%s must be finite and positive." % [path, key]
	var appearance: Variant = node.get("appearance", {})
	if appearance is Dictionary and appearance.get("hasColorOverride", false):
		var color: Variant = appearance.get("color")
		if not color is Dictionary:
			return "%s.appearance.color: color override requires RGBA values." % path
		for channel in ["r", "g", "b", "a"]:
			var value: Variant = color.get(channel)
			if not _valid_number(value) or float(value) < 0.0 or float(value) > 1.0:
				return "%s.appearance.color.%s: expected a normalized finite value." % [path, channel]
	var children: Variant = node.get("children", [])
	if not children is Array:
		return "%s.children: expected an array." % path
	for index in range(children.size()):
		if not children[index] is Dictionary:
			return "%s.children[%d]: node must be an object." % [path, index]
		var child_error := _validate_node(children[index], "%s.children[%d]" % [path, index], node_ids)
		if not child_error.is_empty():
			return child_error
	return ""


static func _validate_game_metadata(metadata: Variant, node_ids: Dictionary) -> String:
	if not metadata is Dictionary or metadata.get("schema", "") != METADATA_SCHEMA:
		return "Unsupported SceneReplay game-metadata schema."
	if not metadata.get("id", "") is String or str(metadata.id).strip_edges().is_empty():
		return "Game metadata requires a stable ID."
	for key in ["anchors", "damageZones", "colliders", "lods", "tags"]:
		if not metadata.get(key, null) is Array:
			return "Game metadata '%s' must be an array." % key
	var tag_error := _validate_tags(metadata.tags, "$.tags")
	if not tag_error.is_empty():
		return tag_error
	for collection in ["anchors", "damageZones", "colliders"]:
		var record_ids := {}
		for index in range(metadata[collection].size()):
			var record: Variant = metadata[collection][index]
			if not record is Dictionary or not node_ids.has(record.get("nodeId", "")):
				return "$.%s[%d].nodeId: metadata references an unknown shape node." % [collection, index]
			if not record.get("id", "") is String or str(record.id).strip_edges().is_empty() or record_ids.has(record.id):
				return "$.%s[%d].id: metadata records require unique stable IDs." % [collection, index]
			record_ids[record.id] = true
			var record_tags: Variant = record.get("tags", [])
			tag_error = _validate_tags(record_tags, "$.%s[%d].tags" % [collection, index])
			if not tag_error.is_empty():
				return tag_error
			if collection == "anchors":
				if str(record.get("role", "")).strip_edges().is_empty() or not _valid_transform(record.get("transform")):
					return "$.anchors[%d]: anchors require a role and valid transform." % index
			elif collection == "damageZones":
				if not _valid_number(record.get("multiplier")) or float(record.multiplier) < 0.0:
					return "$.damageZones[%d].multiplier: expected a finite non-negative value." % index
			else:
				if str(record.get("kind", "")) not in ["box", "sphere", "capsule", "mesh"]:
					return "$.colliders[%d].kind: unsupported collider kind." % index
				if not _valid_vector3(record.get("center")) or not _valid_vector3(record.get("size")):
					return "$.colliders[%d]: collider center and size must be finite vectors." % index
				if not _valid_number(record.get("radius")) or float(record.radius) <= 0.0 or not _valid_number(record.get("height")) or float(record.height) <= 0.0 or not record.get("isTrigger") is bool:
					return "$.colliders[%d]: collider dimensions and trigger flag are invalid." % index
	var previous_lod_height := INF
	for index in range(metadata.lods.size()):
		var lod: Variant = metadata.lods[index]
		if not lod is Dictionary or not lod.get("nodeIds", []) is Array:
			return "$.lods[%d].nodeIds: expected an array." % index
		if int(lod.get("level", -1)) != index or not _valid_number(lod.get("screenRelativeHeight")):
			return "$.lods[%d]: LOD levels must be contiguous and have valid screen heights." % index
		var lod_height := float(lod.screenRelativeHeight)
		if lod_height < 0.0 or lod_height > 1.0 or lod_height >= previous_lod_height:
			return "$.lods[%d].screenRelativeHeight: heights must be normalized and strictly descending." % index
		previous_lod_height = lod_height
		for node_id in lod.nodeIds:
			if not node_ids.has(node_id):
				return "$.lods[%d].nodeIds: metadata references unknown node '%s'." % [index, node_id]
	return ""


static func _validate_tags(tags: Variant, path: String) -> String:
	if not tags is Array:
		return "%s: expected an array of tags." % path
	var seen := {}
	for index in range(tags.size()):
		var tag: Variant = tags[index]
		if not tag is String or str(tag).strip_edges().is_empty() or seen.has(tag):
			return "%s[%d]: tags must be non-empty and unique." % [path, index]
		seen[tag] = true
	return ""


static func _valid_transform(value: Variant) -> bool:
	if not value is Dictionary:
		return false
	return _valid_vector3(value.get("position")) and _valid_vector3(value.get("eulerAngles")) and _valid_vector3(value.get("scale"))


static func _compile_node(node: Dictionary, options: Dictionary) -> Node3D:
	var shape_type: String = str(node.type)
	var object: Node3D
	if shape_type == GROUP_TYPE:
		object = Node3D.new()
	else:
		var mesh_instance := MeshInstance3D.new()
		mesh_instance.mesh = _create_mesh(node)
		var material := _create_material(node, options)
		if material != null:
			mesh_instance.material_override = material
		object = mesh_instance
	object.name = str(node.get("name", node.id))
	var transform: Dictionary = node.transform
	object.position = _vector3(transform.position)
	object.rotation_order = EULER_ORDER_XYZ
	var degrees: Vector3 = _vector3(transform.eulerAngles)
	object.rotation = Vector3(deg_to_rad(degrees.x), deg_to_rad(degrees.y), deg_to_rad(degrees.z))
	object.scale = _vector3(transform.scale)
	object.set_meta("scene_replay", {
		"id": node.id,
		"type": shape_type,
		"colorRole": str(node.get("appearance", {}).get("colorRole", "")),
	})
	for child in node.get("children", []):
		object.add_child(_compile_node(child, options))
	return object


static func _create_mesh(node: Dictionary) -> Mesh:
	match str(node.type):
		"lowpoly/cube":
			return BoxMesh.new()
		"lowpoly/sphere":
			var sphere := SphereMesh.new()
			sphere.radius = 0.5
			sphere.height = 1.0
			sphere.radial_segments = 12
			sphere.rings = 8
			return sphere
		"lowpoly/cylinder":
			var cylinder := CylinderMesh.new()
			cylinder.top_radius = 0.5
			cylinder.bottom_radius = 0.5
			cylinder.height = 2.0
			cylinder.radial_segments = 12
			return cylinder
		"lowpoly/capsule":
			var capsule := CapsuleMesh.new()
			capsule.radius = 0.5
			capsule.height = 2.0
			capsule.radial_segments = 12
			capsule.rings = 4
			return capsule
		"lowpoly/wedge":
			return _create_wedge_mesh()
		"lowpoly/frustum":
			return _create_frustum_mesh(node.get("parameters", {}))
	return null


static func _create_wedge_mesh() -> ArrayMesh:
	var bfl := Vector3(-0.5, -0.5, -0.5)
	var bfr := Vector3(0.5, -0.5, -0.5)
	var bbr := Vector3(0.5, -0.5, 0.5)
	var bbl := Vector3(-0.5, -0.5, 0.5)
	var tbr := Vector3(0.5, 0.5, 0.5)
	var tbl := Vector3(-0.5, 0.5, 0.5)
	return _mesh_from_faces([
		[bfl, bfr, bbr, bbl], [bfr, bfl, tbl, tbr], [bbl, bbr, tbr, tbl],
		[bfl, bbl, tbl], [bbr, bfr, tbr],
	])


static func _create_frustum_mesh(parameters: Dictionary) -> ArrayMesh:
	var top_width: float = float(parameters.get("topWidth", 0.65))
	var top_depth: float = float(parameters.get("topDepth", 0.65))
	var bottom_width: float = float(parameters.get("bottomWidth", 1.0))
	var bottom_depth: float = float(parameters.get("bottomDepth", 1.0))
	var corner := func(width: float, depth: float, x: float, z: float, y: float) -> Vector3:
		return Vector3(width * x * 0.5, y, depth * z * 0.5)
	var bfl: Vector3 = corner.call(bottom_width, bottom_depth, -1.0, -1.0, -0.5)
	var bfr: Vector3 = corner.call(bottom_width, bottom_depth, 1.0, -1.0, -0.5)
	var bbr: Vector3 = corner.call(bottom_width, bottom_depth, 1.0, 1.0, -0.5)
	var bbl: Vector3 = corner.call(bottom_width, bottom_depth, -1.0, 1.0, -0.5)
	var tfl: Vector3 = corner.call(top_width, top_depth, -1.0, -1.0, 0.5)
	var tfr: Vector3 = corner.call(top_width, top_depth, 1.0, -1.0, 0.5)
	var tbr: Vector3 = corner.call(top_width, top_depth, 1.0, 1.0, 0.5)
	var tbl: Vector3 = corner.call(top_width, top_depth, -1.0, 1.0, 0.5)
	return _mesh_from_faces([
		[bfl, bfr, bbr, bbl], [tbl, tbr, tfr, tfl], [bfr, bfl, tfl, tfr],
		[bbl, bbr, tbr, tbl], [bfl, bbl, tbl, tfl], [bbr, bfr, tfr, tbr],
	])


static func _mesh_from_faces(faces: Array) -> ArrayMesh:
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	for face in faces:
		for index in range(1, face.size() - 1):
			surface.add_vertex(face[0])
			surface.add_vertex(face[index])
			surface.add_vertex(face[index + 1])
	surface.generate_normals()
	return surface.commit()


static func _create_material(node: Dictionary, options: Dictionary) -> StandardMaterial3D:
	var appearance: Dictionary = node.get("appearance", {})
	var color: Variant = null
	if appearance.get("hasColorOverride", false):
		var rgba: Dictionary = appearance.color
		color = Color(float(rgba.r), float(rgba.g), float(rgba.b), float(rgba.a))
	else:
		var role: String = str(appearance.get("colorRole", ""))
		var palette: Dictionary = options.get("paletteRoles", {})
		if not role.is_empty() and palette.has(role):
			color = palette[role]
	if color == null:
		return null
	if color is Dictionary:
		color = Color(float(color.get("r", 1.0)), float(color.get("g", 1.0)), float(color.get("b", 1.0)), float(color.get("a", 1.0)))
	elif color is String:
		color = Color.from_string(color, Color.WHITE)
	if not color is Color:
		return null
	var material := StandardMaterial3D.new()
	material.albedo_color = color
	if color.a < 1.0:
		material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	return material


static func _attach_game_metadata(root: Node3D, metadata: Dictionary, node_ids: Dictionary) -> void:
	var nodes := {}
	_collect_compiled_nodes(root, nodes)
	root.set_meta("scene_replay_game_metadata", metadata.duplicate(true))
	root.set_meta("scene_replay_tags", metadata.tags.duplicate())
	root.set_meta("scene_replay_lods", metadata.lods.duplicate(true))
	for anchor in metadata.anchors:
		var marker := Node3D.new()
		marker.name = "Anchor_%s" % anchor.id
		marker.position = _vector3(anchor.transform.position)
		marker.rotation_order = EULER_ORDER_XYZ
		var degrees: Vector3 = _vector3(anchor.transform.eulerAngles)
		marker.rotation = Vector3(deg_to_rad(degrees.x), deg_to_rad(degrees.y), deg_to_rad(degrees.z))
		marker.scale = _vector3(anchor.transform.scale)
		marker.set_meta("scene_replay_anchor", {
			"id": anchor.id,
			"role": anchor.role,
			"tags": anchor.tags.duplicate(),
		})
		nodes[anchor.nodeId].add_child(marker)
	for zone in metadata.damageZones:
		var zones: Array = nodes[zone.nodeId].get_meta("scene_replay_damage_zones", [])
		zones.append(zone.duplicate(true))
		nodes[zone.nodeId].set_meta("scene_replay_damage_zones", zones)
	for collider in metadata.colliders:
		var colliders: Array = nodes[collider.nodeId].get_meta("scene_replay_colliders", [])
		colliders.append(collider.duplicate(true))
		nodes[collider.nodeId].set_meta("scene_replay_colliders", colliders)


static func _collect_compiled_nodes(node: Node, nodes: Dictionary) -> void:
	if node.has_meta("scene_replay") and node.get_meta("scene_replay").has("id"):
		nodes[node.get_meta("scene_replay").id] = node
	for child in node.get_children():
		_collect_compiled_nodes(child, nodes)


static func _valid_vector3(value: Variant) -> bool:
	if not value is Dictionary:
		return false
	for component in ["x", "y", "z"]:
		if not value.has(component) or not _valid_number(value[component]):
			return false
	return true


static func _valid_number(value: Variant) -> bool:
	return (value is int or value is float) and is_finite(float(value))


static func _vector3(value: Dictionary) -> Vector3:
	return Vector3(float(value.x), float(value.y), float(value.z))


static func _failure(message: String) -> Dictionary:
	return {"ok": false, "error": message}
