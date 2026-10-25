# Optional rendered comparison of the real TestWorld; never runs from F5/F6.
extends SceneTree
var output = "user://GrassValidation"
func _initialize():
 for arg in OS.get_cmdline_user_args():
  if arg.begins_with("--output="): output = arg.trim_prefix("--output=")
 DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(output))
 call_deferred("run")
func capture(path):
 for i in 4: await process_frame
 await RenderingServer.frame_post_draw
 print("CAPTURE ", path, " ", root.get_texture().get_image().save_png(path))
 print("RENDER_COUNTS draws=", Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME), " primitives=", Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME))
func run():
 root.content_scale_size = Vector2i(640,360)
 var world = load("res://WORLD/testworld.tscn").instantiate()
 root.add_child(world)
 for i in 3: await process_frame
 var terrain = world.get_node("Terrain3D")
 var player = world.get_node("Explorer")
 var pos = Vector3(512,0,500)
 pos.y = terrain.data.get_height(pos) + 4.0
 player.position = pos
 player.rotation = Vector3.ZERO
 player.get_node("Camera3D").rotation = Vector3(-0.20,0,0)
 var sky = world.get_node("Sky3D")
 sky.game_time_enabled = false; sky.current_time = 10.0
 sky.get_node("SkyDome").wind_speed = 0
 var grass = world.get_node("Vegetation")
 grass.Settings.Enabled = false; grass.ApplySettings()
 terrain.assets.get_mesh_asset(0).enabled = true
 sky.get_node("SkyDome").fog_density = 0.01
 sky.get_node("SkyDome").fog_end = 1000.0
 await capture(output.path_join("before.png"))
 terrain.assets.get_mesh_asset(0).enabled = false
 grass.Settings.Enabled = true; grass.ApplySettings()
 world.get_node("Environment").ApplyMist()
 await capture(output.path_join("after.png"))
 await capture(output.path_join("wind.png"))
 grass.Settings.Quality = 0; grass.ApplySettings()
 await capture(output.path_join("low-quality.png"))
 grass.Settings.Quality = 2
 grass.Settings.Enabled = false; grass.ApplySettings()
 var reference = load("res://addons/terrain_3d/extras/particle_example/Terrain3DParticles.tscn").instantiate()
 reference.terrain = terrain
 terrain.add_child(reference)
 await capture(output.path_join("demo-reference.png"))
 reference.queue_free()
 await process_frame
 grass.Settings.Enabled = true; grass.ApplySettings()
 pos = Vector3(512, 0, 620)
 pos.y = terrain.data.get_height(pos) + 4.0
 player.position = pos
 player.get_node("Camera3D").rotation = Vector3(-0.10, 0, 0)
 await capture(output.path_join("mist.png"))
 quit()
