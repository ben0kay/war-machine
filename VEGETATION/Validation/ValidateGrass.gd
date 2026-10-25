# Diagnostic-only integration check. Project gameplay remains C#; run with a Godot .NET --headless --script command.
extends SceneTree
var failed = false
func check(ok, label):
 print("PASS " if ok else "FAIL ", label)
 failed = failed or not ok
func _initialize(): call_deferred("run")
func run():
 var world = load("res://WORLD/testworld.tscn").instantiate()
 root.add_child(world)
 for i in 5: await process_frame
 var grass = world.get_node("Vegetation")
 var explorer = world.get_node("Explorer")
 var terrain = world.get_node("Terrain3D")
 var sky = world.get_node("Sky3D")
 var env = world.get_node("Environment")
 check(grass.AllocatedParticles == 451584, "default ribbon particle budget: " + str(grass.AllocatedParticles))
 check(grass.get_child_count() == 49, "bounded tile pool")
 check(not grass.is_processing() and not grass.is_physics_processing(), "no grass frame polling")
 check(not terrain.assets.get_mesh_asset(0).enabled, "legacy saved cards disabled")
 check(is_equal_approx(sky.get_node("SkyDome").fog_density, 0.025) and sky.get_node("SkyDome").fog_end == 200, "requested close mist applied")
 check(sky.get_node("SkyDome").fog_material.get_shader_parameter("fog_end") == 200, "actual fog shader receives end distance")
 var material = grass.get_child(0).material_override
 var colour = material.get_shader_parameter("blade_color")
 check(colour is Vector3 and colour.is_equal_approx(Vector3(0.2, 0.22, 0.05)), "demo linear RGB reaches ribbon shader without extra colour conversion")
 check(material.get_shader_parameter("render_distance") == 60, "camera-relative fade uses configured distance")
 var binding_count = grass.TerrainBindingUpdates
 var restarts = grass.TileRestarts
 await create_timer(0.25).timeout
 check(grass.TerrainBindingUpdates == binding_count and grass.TileRestarts == restarts, "stationary: no terrain uniform upload or restart")
 explorer.position.x += 0.1
 for i in 2: await process_frame
 check(grass.TileRestarts == restarts, "subtile camera movement does not restart tiles")
 explorer.position.x += 24
 for i in 2: await process_frame
 check(grass.TileRestarts - restarts == 7, "single tile crossing restarts only seven entering tiles")
 check(grass.TerrainBindingUpdates == binding_count, "camera movement does not rebind terrain")
 var before_refresh = grass.TerrainBindingUpdates
 terrain.data.emit_signal("maps_changed")
 terrain.data.emit_signal("height_maps_changed")
 for i in 3: await process_frame
 check(grass.TerrainBindingUpdates == before_refresh + 1, "coalesced map signals refresh bindings once")
 var settings = grass.Settings
 settings.Enabled = false
 grass.ApplySettings()
 for i in 2: await process_frame
 check(grass.get_child_count() == 0 and grass.AllocatedParticles == 0, "disabled grass has no emitters")
 settings.Enabled = true; settings.Density = 0
 grass.ApplySettings()
 check(grass.get_child_count() == 0, "zero density fully dormant")
 settings.Density = 1; settings.Quality = 0
 grass.ApplySettings()
 check(grass.AllocatedParticles < 451584 and grass.get_child_count() == 49, "low quality reduces particle count")
 print("LOW_PARTICLES ", grass.AllocatedParticles)
 settings.Quality = 2; settings.RenderDistance = 128; settings.Density = 2
 grass.ApplySettings()
 check(grass.AllocatedParticles <= settings.ParticleBudget, "large radius/density obey hard budget")
 settings.RenderDistance = 60; settings.Density = 1
 grass.ApplySettings()
 check(grass.AllocatedParticles == 451584, "reactivation restores configured quality")
 var time = sky.current_time
 await create_timer(0.25).timeout
 check(sky.current_time > time, "sun continues moving")
 var k = InputEventKey.new(); k.keycode = KEY_P; k.pressed = true
 Input.parse_input_event(k); await process_frame
 var paused = sky.current_time
 await create_timer(0.25).timeout
 check(sky.current_time == paused, "existing P solar pause preserved")
 Input.parse_input_event(k); await process_frame
 k = InputEventKey.new(); k.keycode = KEY_F8; k.pressed = true
 Input.parse_input_event(k); await process_frame
 check(sky.minutes_per_day == 1, "existing F8 preview preserved")
 var h = terrain.data.get_height(Vector3(512,0,620))
 check(abs(h - 47.9678688) < 0.0001, "saved terrain height unchanged")
 world.queue_free()
 await process_frame
 check(true, "world unload disconnects data and camera signals")
 quit(1 if failed else 0)
