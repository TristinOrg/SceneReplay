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
	"lowpoly/extruded-profile",
	"lowpoly/profile-loft",
	"lowpoly/profile-cage",
	"lowpoly/lathe-profile",
	"lowpoly/profile-sweep",
]
const ADAPTER_DESCRIPTOR := {
	"schema": "scenereplay.adapter/1.0",
	"adapterId": "scenereplay/godot",
	"engineId": "godot",
	"version": "0.1.0",
	"features": ["stable-node-ids", "hierarchy", "local-transforms", "game-metadata", "profile-geometry"],
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
	var profile_error := _validate_profile_node(node, path)
	if not profile_error.is_empty():
		return profile_error
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


static func _validate_profile_node(node: Dictionary, path: String) -> String:
	var shape_type: String = str(node.get("type", ""))
	if not shape_type in ["lowpoly/extruded-profile", "lowpoly/profile-loft", "lowpoly/profile-cage", "lowpoly/lathe-profile", "lowpoly/profile-sweep"]:
		return ""
	var params: Variant = node.get("parameters", {})
	if not params is Dictionary:
		return "%s.parameters: expected an object." % path
	for smoothing_key in ["profileSmoothing", "pathSmoothing"]:
		if params.has(smoothing_key) and not _valid_integer_range(params[smoothing_key], 0, 2):
			return "%s.parameters.%s must be an integer from 0 to 2." % [path, smoothing_key]
	if params.has("smoothNormals") and (not _valid_number(params.smoothNormals) or float(params.smoothNormals) < 0.0):
		return "%s.parameters.smoothNormals must be a finite non-negative value." % path
	var closed := shape_type != "lowpoly/lathe-profile"
	var profile: Variant = node.get("profile", [])
	if shape_type != "lowpoly/profile-cage":
		var profile_error := _validate_profile(profile, closed, "%s.profile" % path)
		if not profile_error.is_empty():
			return profile_error
	if shape_type == "lowpoly/extruded-profile":
		var depth: Variant = params.get("profileDepth", 0.35)
		if not _valid_number(depth) or float(depth) <= 0.0:
			return "%s.parameters.profileDepth must be finite and positive." % path
		var bevel: Variant = params.get("profileBevel", 0.0)
		var bevel_segments: Variant = params.get("profileBevelSegments", 1)
		if not _valid_number(bevel) or float(bevel) < 0.0 or float(bevel) * 2.0 >= float(depth):
			return "%s.parameters.profileBevel must be non-negative and less than half the extrusion depth." % path
		if not _valid_integer_range(bevel_segments, 1, 8):
			return "%s.parameters.profileBevelSegments must be an integer from 1 to 8." % path
	elif shape_type == "lowpoly/profile-loft":
		var sections: Variant = node.get("profileSections", [])
		if not sections is Array or sections.size() < 2 or sections.size() > 16:
			return "%s.profileSections: expected 2 to 16 sections." % path
		var previous_z := -INF
		for index in range(sections.size()):
			var section: Variant = sections[index]
			if not section is Dictionary or not _valid_number(section.get("z")) or float(section.z) <= previous_z:
				return "%s.profileSections[%d].z must be finite and strictly increasing." % [path, index]
			previous_z = float(section.z)
			for key in ["scale", "offset"]:
				if not _valid_vector2(section.get(key)):
					return "%s.profileSections[%d].%s must contain finite x and y values." % [path, index, key]
			if float(section.scale.x) <= 0.0 or float(section.scale.y) <= 0.0:
				return "%s.profileSections[%d].scale must be positive." % [path, index]
		var subdivisions: Variant = params.get("loftSubdivisions", 1)
		if not _valid_integer_range(subdivisions, 1, 4):
			return "%s.parameters.loftSubdivisions must be an integer from 1 to 4." % path
	elif shape_type == "lowpoly/profile-cage":
		var sections: Variant = node.get("profileCageSections", [])
		if not sections is Array or sections.size() < 2 or sections.size() > 16:
			return "%s.profileCageSections: expected 2 to 16 sections." % path
		var previous_z := -INF
		var expected_count := -1
		for index in range(sections.size()):
			var section: Variant = sections[index]
			if not section is Dictionary or not _valid_number(section.get("z")) or float(section.z) <= previous_z:
				return "%s.profileCageSections[%d].z must be finite and strictly increasing." % [path, index]
			previous_z = float(section.z)
			var cage_profile: Variant = section.get("profile", [])
			var profile_error := _validate_profile(cage_profile, true, "%s.profileCageSections[%d].profile" % [path, index])
			if not profile_error.is_empty():
				return profile_error
			if expected_count < 0:
				expected_count = cage_profile.size()
			elif cage_profile.size() != expected_count:
				return "%s.profileCageSections[%d].profile must match the first section's point count." % [path, index]
			elif _profile_signed_area(cage_profile) * _profile_signed_area(sections[0].profile) <= 0.0:
				return "%s.profileCageSections[%d].profile must keep the first section's winding." % [path, index]
		var subdivisions: Variant = params.get("cageSubdivisions", 1)
		if not _valid_integer_range(subdivisions, 1, 4):
			return "%s.parameters.cageSubdivisions must be an integer from 1 to 4." % path
	elif shape_type == "lowpoly/lathe-profile":
		var previous_y := -INF
		for index in range(profile.size()):
			var point: Dictionary = profile[index]
			if float(point.x) < 0.0 or float(point.y) <= previous_y:
				return "%s.profile[%d] must have non-negative radius and strictly increasing height." % [path, index]
			previous_y = float(point.y)
		var radial_segments: Variant = params.get("radialSegments", 12)
		if not _valid_integer_range(radial_segments, 3, 64):
			return "%s.parameters.radialSegments must be an integer from 3 to 64." % path
	elif shape_type == "lowpoly/profile-sweep":
		var points: Variant = node.get("path", [])
		if not points is Array or points.size() < 2 or points.size() > 32:
			return "%s.path: expected 2 to 32 points." % path
		for index in range(points.size()):
			if not _valid_vector3(points[index]):
				return "%s.path[%d] must contain finite x, y, and z values." % [path, index]
			if index > 0 and _vector3(points[index]).distance_to(_vector3(points[index - 1])) <= 0.00001:
				return "%s.path[%d] must differ from the previous point." % [path, index]
	return ""


static func _validate_profile(value: Variant, closed: bool, path: String) -> String:
	var minimum := 3 if closed else 2
	if not value is Array or value.size() < minimum:
		return "%s: expected at least %d points." % [path, minimum]
	for index in range(value.size()):
		if not _valid_vector2(value[index]):
			return "%s[%d]: expected finite x and y values." % [path, index]
		if index > 0 and _vector2(value[index]).distance_to(_vector2(value[index - 1])) <= 0.00001:
			return "%s[%d]: consecutive duplicate points are not allowed." % [path, index]
	if closed and _vector2(value[0]).distance_to(_vector2(value[value.size() - 1])) <= 0.00001:
		return "%s: do not repeat the first point at the end." % path
	if closed and absf(_profile_signed_area(value)) <= 0.00001:
		return "%s: closed profile area must be non-zero." % path
	if value.size() > 32:
		return "%s: profile is limited to 32 points." % path
	if closed and Geometry2D.triangulate_polygon(_packed_profile(value)).is_empty():
		return "%s: profile must be a simple polygon." % path
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
		"lowpoly/extruded-profile":
			return _create_extruded_profile_mesh(node)
		"lowpoly/profile-loft":
			return _create_profile_loft_mesh(node)
		"lowpoly/profile-cage":
			return _create_profile_cage_mesh(node)
		"lowpoly/lathe-profile":
			return _create_lathe_profile_mesh(node)
		"lowpoly/profile-sweep":
			return _create_profile_sweep_mesh(node)
	return null


static func _create_extruded_profile_mesh(node: Dictionary) -> ArrayMesh:
	var profile: Array = _profile_points(node.profile, int(node.get("parameters", {}).get("profileSmoothing", 0)), true)
	var params: Dictionary = node.get("parameters", {})
	var half_depth := float(params.get("profileDepth", 0.35)) * 0.5
	var bevel := float(params.get("profileBevel", 0.0))
	var bevel_segments := int(params.get("profileBevelSegments", 1))
	var inset := _offset_profile(profile, bevel) if bevel > 0.0 else profile
	var rings: Array = []
	if bevel > 0.0:
		for step in range(bevel_segments + 1):
			var t := float(step) / float(bevel_segments)
			rings.append(_profile_ring(inset, profile, t, lerpf(-half_depth, -half_depth + bevel, t)))
		rings.append(_profile_ring(profile, profile, 0.0, half_depth - bevel))
		for step in range(1, bevel_segments + 1):
			var t := float(step) / float(bevel_segments)
			rings.append(_profile_ring(profile, inset, t, lerpf(half_depth - bevel, half_depth, t)))
	else:
		rings = [_profile_ring(profile, profile, 0.0, -half_depth), _profile_ring(profile, profile, 0.0, half_depth)]
	return _mesh_from_rings(rings, true, inset, true, float(params.get("smoothNormals", 0.0)) > 0.0)


static func _profile_ring(from_profile: Array, to_profile: Array, t: float, z: float) -> Array[Vector3]:
	var ring: Array[Vector3] = []
	for index in range(from_profile.size()):
		var point: Vector2 = from_profile[index].lerp(to_profile[index], t)
		ring.append(Vector3(point.x, point.y, z))
	return ring


static func _offset_profile(profile: Array, distance: float) -> Array:
	var inset: Array = []
	for index in range(profile.size()):
		var previous: Vector2 = profile[(index - 1 + profile.size()) % profile.size()]
		var current: Vector2 = profile[index]
		var next: Vector2 = profile[(index + 1) % profile.size()]
		var edge_a := (current - previous).normalized()
		var edge_b := (next - current).normalized()
		var normal_a := Vector2(-edge_a.y, edge_a.x)
		var normal_b := Vector2(-edge_b.y, edge_b.x)
		var bisector := (normal_a + normal_b).normalized()
		var denominator := bisector.dot(normal_a)
		if absf(denominator) < 0.0001:
			return profile
		inset.append(current + bisector * (distance / denominator))
	return inset


static func _create_profile_loft_mesh(node: Dictionary) -> ArrayMesh:
	var params: Dictionary = node.get("parameters", {})
	var base: Array = _profile_points(node.profile, int(params.get("profileSmoothing", 0)), true)
	var sections: Array = node.profileSections
	var subdivisions := int(params.get("loftSubdivisions", 1))
	var rings: Array = []
	for section_index in range(sections.size() - 1):
		var first: Dictionary = sections[section_index]
		var second: Dictionary = sections[section_index + 1]
		for step in range(subdivisions):
			var t := float(step) / float(subdivisions)
			rings.append(_loft_ring(base, first, second, t))
	rings.append(_loft_ring(base, sections[sections.size() - 1], sections[sections.size() - 1], 0.0))
	return _mesh_from_rings(rings, true, base, true, float(params.get("smoothNormals", 0.0)) > 0.0)


static func _loft_ring(profile: Array, first: Dictionary, second: Dictionary, t: float) -> Array[Vector3]:
	var eased := t * t * (3.0 - 2.0 * t)
	var z := lerpf(float(first.z), float(second.z), eased)
	var scale_x := lerpf(float(first.scale.x), float(second.scale.x), eased)
	var scale_y := lerpf(float(first.scale.y), float(second.scale.y), eased)
	var offset_x := lerpf(float(first.offset.x), float(second.offset.x), eased)
	var offset_y := lerpf(float(first.offset.y), float(second.offset.y), eased)
	var ring: Array[Vector3] = []
	for point in profile:
		ring.append(Vector3(point.x * scale_x + offset_x, point.y * scale_y + offset_y, z))
	return ring


static func _create_profile_cage_mesh(node: Dictionary) -> ArrayMesh:
	var params: Dictionary = node.get("parameters", {})
	var sections: Array = node.profileCageSections
	var subdivisions := int(params.get("cageSubdivisions", 1))
	var rings: Array = []
	for section_index in range(sections.size() - 1):
		var first: Dictionary = sections[section_index]
		var second: Dictionary = sections[section_index + 1]
		var first_profile := _profile_points(first.profile, int(params.get("profileSmoothing", 0)), true)
		var second_profile := _profile_points(second.profile, int(params.get("profileSmoothing", 0)), true)
		for step in range(subdivisions):
			var t := float(step) / float(subdivisions)
			var ring: Array[Vector3] = []
			for point_index in range(first_profile.size()):
				var a: Vector2 = first_profile[point_index]
				var b: Vector2 = second_profile[point_index]
				var point := a.lerp(b, t)
				ring.append(Vector3(point.x, point.y, lerpf(float(first.z), float(second.z), t)))
			rings.append(ring)
	var final_section: Dictionary = sections[sections.size() - 1]
	var last_ring: Array[Vector3] = []
	for point in _profile_points(final_section.profile, int(params.get("profileSmoothing", 0)), true):
		last_ring.append(Vector3(point.x, point.y, float(final_section.z)))
	rings.append(last_ring)
	var cap_profile: Array = []
	for point in _profile_points(sections[0].profile, int(params.get("profileSmoothing", 0)), true):
		cap_profile.append(point)
	return _mesh_from_rings(rings, true, cap_profile, true, float(params.get("smoothNormals", 0.0)) > 0.0)


static func _create_lathe_profile_mesh(node: Dictionary) -> ArrayMesh:
	var params: Dictionary = node.get("parameters", {})
	var profile: Array = _profile_points(node.profile, int(params.get("profileSmoothing", 0)), false)
	var segments := int(params.get("radialSegments", 12))
	var rings: Array = []
	for radial_index in range(segments + 1):
		var angle := TAU * float(radial_index) / float(segments)
		var ring: Array[Vector3] = []
		for point in profile:
			ring.append(Vector3(cos(angle) * point.x, point.y, sin(angle) * point.x))
		rings.append(ring)
	return _mesh_from_rings(rings, false, [], false, float(params.get("smoothNormals", 0.0)) > 0.0)


static func _create_profile_sweep_mesh(node: Dictionary) -> ArrayMesh:
	var params: Dictionary = node.get("parameters", {})
	var profile: Array = _profile_points(node.profile, int(params.get("profileSmoothing", 0)), true)
	var path: Array = _path_points(node.path, int(params.get("pathSmoothing", 0)))
	var rings: Array = []
	for path_index in range(path.size()):
		var tangent: Vector3
		if path_index == 0:
			tangent = (path[1] - path[0]).normalized()
		elif path_index == path.size() - 1:
			tangent = (path[path_index] - path[path_index - 1]).normalized()
		else:
			tangent = (path[path_index + 1] - path[path_index - 1]).normalized()
		var reference := Vector3.UP if absf(tangent.dot(Vector3.UP)) < 0.95 else Vector3.RIGHT
		var side := reference.cross(tangent).normalized()
		var vertical := tangent.cross(side).normalized()
		var ring: Array[Vector3] = []
		for point in profile:
			ring.append(path[path_index] + side * point.x + vertical * point.y)
		rings.append(ring)
	return _mesh_from_rings(rings, true, profile, true, float(params.get("smoothNormals", 0.0)) > 0.0)


static func _mesh_from_rings(rings: Array, capped: bool, cap_profile: Array, profile_closed: bool = true, smooth_normals: bool = false) -> ArrayMesh:
	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	if rings.is_empty():
		return surface.commit()
	var point_count: int = rings[0].size()
	for ring_index in range(rings.size() - 1):
		var edge_count := point_count if profile_closed else point_count - 1
		for point_index in range(edge_count):
			var next_index := (point_index + 1) % point_count
			var a: Vector3 = rings[ring_index][point_index]
			var b: Vector3 = rings[ring_index][next_index]
			var c: Vector3 = rings[ring_index + 1][next_index]
			var d: Vector3 = rings[ring_index + 1][point_index]
			_add_triangle(surface, a, b, c)
			_add_triangle(surface, a, c, d)
	if capped and not cap_profile.is_empty():
		var triangulation := Geometry2D.triangulate_polygon(_packed_profile(cap_profile))
		var first_ring: Array = rings[0]
		var last_ring: Array = rings[rings.size() - 1]
		for index in range(0, triangulation.size(), 3):
			var ia: int = triangulation[index]
			var ib: int = triangulation[index + 1]
			var ic: int = triangulation[index + 2]
			_add_triangle(surface, first_ring[ic], first_ring[ib], first_ring[ia])
			_add_triangle(surface, last_ring[ia], last_ring[ib], last_ring[ic])
	if smooth_normals:
		surface.index()
	surface.generate_normals()
	return surface.commit()


static func _add_triangle(surface: SurfaceTool, a: Vector3, b: Vector3, c: Vector3) -> void:
	surface.add_vertex(a)
	surface.add_vertex(b)
	surface.add_vertex(c)


static func _profile_points(source: Array, smoothing: int, closed: bool) -> Array:
	var points: Array = []
	for point in source:
		points.append(_vector2(point))
	points = _chaikin(points, closed, smoothing)
	if closed and _profile_signed_area(points) < 0.0:
		points.reverse()
	return points


static func _path_points(source: Array, smoothing: int) -> Array:
	var points: Array = []
	for point in source:
		points.append(_vector3(point))
	return _chaikin(points, false, smoothing)


static func _chaikin(points: Array, closed: bool, iterations: int) -> Array:
	var result := points.duplicate()
	for _iteration in range(iterations):
		var next: Array = []
		var edge_count: int = result.size() if closed else result.size() - 1
		if not closed:
			next.append(result[0])
		for index in range(edge_count):
			var a: Variant = result[index]
			var b: Variant = result[(index + 1) % result.size()]
			next.append(a * 0.75 + b * 0.25)
			next.append(a * 0.25 + b * 0.75)
		if not closed:
			next.append(result[result.size() - 1])
		result = next
	return result


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


static func _valid_vector2(value: Variant) -> bool:
	if not value is Dictionary:
		return false
	for component in ["x", "y"]:
		if not value.has(component) or not _valid_number(value[component]):
			return false
	return true


static func _valid_integer_range(value: Variant, minimum: int, maximum: int) -> bool:
	return (value is int or value is float) and is_finite(float(value)) and float(value) == floorf(float(value)) and int(value) >= minimum and int(value) <= maximum


static func _vector2(value: Dictionary) -> Vector2:
	return Vector2(float(value.x), float(value.y))


static func _packed_profile(points: Array) -> PackedVector2Array:
	var result := PackedVector2Array()
	for point in points:
		result.append(point if point is Vector2 else _vector2(point))
	return result


static func _profile_signed_area(points: Array) -> float:
	var area := 0.0
	for index in range(points.size()):
		var current: Vector2 = points[index] if points[index] is Vector2 else _vector2(points[index])
		var next_value: Variant = points[(index + 1) % points.size()]
		var next: Vector2 = next_value if next_value is Vector2 else _vector2(next_value)
		area += current.x * next.y - next.x * current.y
	return area * 0.5


static func _valid_number(value: Variant) -> bool:
	return (value is int or value is float) and is_finite(float(value))


static func _vector3(value: Dictionary) -> Vector3:
	return Vector3(float(value.x), float(value.y), float(value.z))


static func _failure(message: String) -> Dictionary:
	return {"ok": false, "error": message}
