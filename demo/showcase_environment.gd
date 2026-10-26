extends Node3D

var _wood: StandardMaterial3D
var _dark_wood: StandardMaterial3D
var _roof: StandardMaterial3D
var _stone: StandardMaterial3D
var _grass: StandardMaterial3D
var _pine_dark: StandardMaterial3D
var _pine_light: StandardMaterial3D
var _warm_glow: StandardMaterial3D
var _water: StandardMaterial3D


func _ready() -> void:
	_create_materials()
	_create_ground()
	_create_shelter()
	_create_path()
	_create_pines()
	_create_rocks()
	_create_fence()
	_create_puddles()
	_create_distant_hills()


func _create_materials() -> void:
	_wood = _material(Color("72513a"), 0.82)
	_dark_wood = _material(Color("3a2d28"), 0.9)
	_roof = _material(Color("252d35"), 0.72)
	_stone = _material(Color("667078"), 0.95)
	_grass = _material(Color("253d32"), 0.95)
	_pine_dark = _material(Color("17372c"), 0.94)
	_pine_light = _material(Color("285345"), 0.9)
	_warm_glow = _material(Color("f3a94f"), 0.45)
	_warm_glow.emission_enabled = true
	_warm_glow.emission = Color("f39a3c")
	_warm_glow.emission_energy_multiplier = 3.2
	_water = _material(Color(0.18, 0.28, 0.34, 0.64), 0.15)
	_water.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_water.metallic = 0.25


func _create_ground() -> void:
	_add_box("Ground", Vector3(64.0, 0.2, 64.0), Vector3(0.0, -0.12, 0.0), _grass)
	var ground_body := StaticBody3D.new()
	ground_body.name = "GroundCollision"
	var shape_node := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = Vector3(64.0, 0.2, 64.0)
	shape_node.shape = shape
	shape_node.position.y = -0.12
	ground_body.add_child(shape_node)
	add_child(ground_body)


func _create_shelter() -> void:
	var center := Vector3(-6.0, 0.0, -2.0)
	_add_box("ShelterFloor", Vector3(6.2, 0.3, 6.2), center + Vector3(0.0, 0.05, 0.0), _dark_wood)
	_add_box("ShelterBack", Vector3(0.3, 4.1, 6.2), center + Vector3(-3.0, 2.1, 0.0), _wood)
	_add_box("ShelterLeft", Vector3(6.2, 4.1, 0.3), center + Vector3(0.0, 2.1, -3.0), _wood)
	_add_box("ShelterRight", Vector3(3.2, 4.1, 0.3), center + Vector3(-1.5, 2.1, 3.0), _wood)
	_add_box("ShelterFrontPost", Vector3(0.34, 4.2, 0.34), center + Vector3(2.8, 2.1, -2.78), _dark_wood)
	_add_box("ShelterRoof", Vector3(7.0, 0.3, 7.0), center + Vector3(-0.12, 4.35, 0.0), _roof, Vector3(0.0, 0.0, -3.5))

	_add_box("WindowFrame", Vector3(0.12, 1.8, 2.4), center + Vector3(-3.18, 2.25, 0.0), _dark_wood)
	_add_box("WarmWindow", Vector3(0.08, 1.45, 2.05), center + Vector3(-3.25, 2.25, 0.0), _warm_glow)
	_add_box("BenchSeat", Vector3(2.5, 0.22, 0.65), center + Vector3(-1.15, 0.75, -2.25), _wood)
	_add_box("BenchLegA", Vector3(0.22, 0.8, 0.5), center + Vector3(-2.0, 0.38, -2.25), _dark_wood)
	_add_box("BenchLegB", Vector3(0.22, 0.8, 0.5), center + Vector3(-0.3, 0.38, -2.25), _dark_wood)

	var lantern := OmniLight3D.new()
	lantern.name = "ShelterLantern"
	lantern.position = center + Vector3(1.8, 2.8, 1.4)
	lantern.light_color = Color("ffb968")
	lantern.light_energy = 3.2
	lantern.omni_range = 8.0
	lantern.shadow_enabled = true
	add_child(lantern)
	_add_sphere("LanternGlow", Vector3(0.14, 0.2, 0.14), lantern.position, _warm_glow)


func _create_path() -> void:
	var points := [
		Vector3(8.0, 0.02, 10.0), Vector3(6.0, 0.02, 8.2), Vector3(4.0, 0.02, 6.5),
		Vector3(2.0, 0.02, 4.8), Vector3(0.0, 0.02, 3.2), Vector3(-1.8, 0.02, 1.8),
		Vector3(-3.2, 0.02, 0.6), Vector3(-4.25, 0.02, -0.55),
	]
	for index in points.size():
		var scale := Vector3(1.2 + (index % 3) * 0.18, 0.08, 0.8 + (index % 2) * 0.15)
		_add_box("PathStone%02d" % index, scale, points[index], _stone, Vector3(0.0, -18.0 + index * 7.0, 0.0))


func _create_pines() -> void:
	var positions := [
		Vector3(-15.0, 0.0, -8.0), Vector3(-12.0, 0.0, 5.0), Vector3(-9.0, 0.0, 11.0),
		Vector3(-2.0, 0.0, -13.0), Vector3(5.0, 0.0, -11.0), Vector3(12.0, 0.0, -7.0),
		Vector3(16.0, 0.0, 1.0), Vector3(15.0, 0.0, 11.0), Vector3(7.0, 0.0, 16.0),
		Vector3(-3.0, 0.0, 17.0), Vector3(-16.0, 0.0, 14.0), Vector3(-19.0, 0.0, 1.0),
	]
	for index in positions.size():
		_create_pine(positions[index], 0.85 + float(index % 4) * 0.12, index)


func _create_pine(position: Vector3, scale_factor: float, index: int) -> void:
	var trunk_mesh := CylinderMesh.new()
	trunk_mesh.top_radius = 0.16 * scale_factor
	trunk_mesh.bottom_radius = 0.25 * scale_factor
	trunk_mesh.height = 2.2 * scale_factor
	trunk_mesh.radial_segments = 7
	trunk_mesh.material = _dark_wood
	_add_mesh("PineTrunk%02d" % index, trunk_mesh, position + Vector3.UP * 1.1 * scale_factor)
	for layer in 3:
		var cone := CylinderMesh.new()
		cone.top_radius = 0.0
		cone.bottom_radius = (1.8 - layer * 0.3) * scale_factor
		cone.height = 2.8 * scale_factor
		cone.radial_segments = 8
		cone.material = _pine_light if layer == 2 else _pine_dark
		_add_mesh(
			"PineCrown%02d_%d" % [index, layer],
			cone,
			position + Vector3.UP * (2.2 + layer * 1.25) * scale_factor,
			Vector3(0.0, index * 17.0 + layer * 11.0, 0.0)
		)


func _create_rocks() -> void:
	var rocks := [
		[Vector3(2.5, 0.25, -3.0), Vector3(1.5, 0.65, 1.1)],
		[Vector3(7.5, 0.18, 3.0), Vector3(0.8, 0.42, 0.65)],
		[Vector3(-11.0, 0.28, -3.0), Vector3(1.35, 0.72, 1.0)],
		[Vector3(11.0, 0.22, 13.0), Vector3(1.0, 0.5, 0.8)],
		[Vector3(-4.0, 0.15, 8.0), Vector3(0.72, 0.35, 0.55)],
	]
	for index in rocks.size():
		_add_sphere("Rock%02d" % index, rocks[index][1], rocks[index][0], _stone, Vector3(0.0, index * 31.0, index * 7.0))


func _create_fence() -> void:
	for index in 7:
		var position := Vector3(2.0 + index * 2.4, 0.9, -5.5)
		_add_box("FencePost%02d" % index, Vector3(0.18, 1.8, 0.18), position, _dark_wood)
	_add_box("FenceRailTop", Vector3(14.6, 0.16, 0.16), Vector3(9.2, 1.35, -5.5), _wood)
	_add_box("FenceRailBottom", Vector3(14.6, 0.16, 0.16), Vector3(9.2, 0.7, -5.5), _wood)


func _create_puddles() -> void:
	var puddles := [
		[Vector3(4.8, 0.015, 2.0), Vector3(1.5, 0.025, 0.8)],
		[Vector3(8.0, 0.015, 6.0), Vector3(1.0, 0.025, 1.6)],
		[Vector3(-1.0, 0.015, 7.0), Vector3(1.8, 0.025, 0.7)],
	]
	for index in puddles.size():
		_add_sphere("Puddle%02d" % index, puddles[index][1], puddles[index][0], _water)


func _create_distant_hills() -> void:
	for index in 9:
		var angle := float(index) / 9.0 * TAU
		var position := Vector3(cos(angle) * 28.0, -1.4, sin(angle) * 28.0)
		var scale := Vector3(9.0, 3.0 + float(index % 3), 7.0)
		_add_sphere("Hill%02d" % index, scale, position, _grass)


func _material(color: Color, roughness: float) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.albedo_color = color
	material.roughness = roughness
	return material


func _add_box(node_name: String, size: Vector3, position: Vector3, material: Material, rotation := Vector3.ZERO) -> MeshInstance3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	mesh.material = material
	return _add_mesh(node_name, mesh, position, rotation)


func _add_sphere(node_name: String, scale: Vector3, position: Vector3, material: Material, rotation := Vector3.ZERO) -> MeshInstance3D:
	var mesh := SphereMesh.new()
	mesh.radius = 1.0
	mesh.height = 2.0
	mesh.radial_segments = 12
	mesh.rings = 6
	mesh.material = material
	var instance := _add_mesh(node_name, mesh, position, rotation)
	instance.scale = scale
	return instance


func _add_mesh(node_name: String, mesh: Mesh, position: Vector3, rotation := Vector3.ZERO) -> MeshInstance3D:
	var instance := MeshInstance3D.new()
	instance.name = node_name
	instance.mesh = mesh
	instance.position = position
	instance.rotation_degrees = rotation
	add_child(instance)
	return instance
