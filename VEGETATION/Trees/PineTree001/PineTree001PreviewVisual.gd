# Editor/runtime preview only: loads a single PineTree001 LOD and applies separate bark/foliage shaders.
# Dense forest runtime should use MultiMesh and shared materials, NOT one script per tree.
@tool
extends Node3D
@export_range(1, 3, 1) var lod: int = 1

# =========================================================
func _ready() -> void:
    call_deferred("_load_visual")

# =========================================================
func _load_visual() -> void:
    if get_child_count() > 0: return
    var base_path := "res://ASSETS/Vegetation/Trees/PineTree001/Models/pine_tree_001_a_LOD%d.gltf" % lod
    var model := load(base_path) as PackedScene
    if model == null:
        push_error("PineTree001: Missing source model: " + base_path)
        return
    var instance := model.instantiate()
    add_child(instance)
    var bark := load("res://ASSETS/Vegetation/Trees/PineTree001/Materials/PineTree001Bark.tres") as ShaderMaterial
    var needles := load("res://ASSETS/Vegetation/Trees/PineTree001/Materials/PineTree001Foliage.tres") as ShaderMaterial
    _bind_materials(instance, bark, needles)

# =========================================================
func _bind_materials(node: Node, bark: ShaderMaterial, needles: ShaderMaterial) -> void:
    if node is MeshInstance3D:
        var visual := node as MeshInstance3D
        if visual.mesh != null and visual.mesh.get_surface_count() >= 2:
            visual.set_surface_override_material(0, bark)
            visual.set_surface_override_material(1, needles)
    for child in node.get_children():
        _bind_materials(child, bark, needles)
