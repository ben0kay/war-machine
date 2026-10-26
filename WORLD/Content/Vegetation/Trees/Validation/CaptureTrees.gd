# Optional render-only inspection of the actual TestWorld; does not save or regenerate world resources.
extends SceneTree
var output="user://TreeValidation"
func _initialize():
 for arg in OS.get_cmdline_user_args():
  if arg.begins_with("--output="): output=arg.trim_prefix("--output=")
 DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(output))
 call_deferred("run")
func shot(path):
 for i in 4: await process_frame
 await RenderingServer.frame_post_draw
 print("CAPTURE ", root.get_texture().get_image().save_png(path), " ", path)
 print("COUNTERS draws=",Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)," primitives=",Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME))
func run():
 root.content_scale_size=Vector2i(640,360)
 var world=load("res://WORLD/testworld.tscn").instantiate()
 root.add_child(world)
 for i in 3: await process_frame
 var trees=world.get_node("Trees")
 print("TREES ",trees.ActiveTrees," CHUNKS ",trees.ActiveChunks)
 var sky=world.get_node("Sky3D")
 sky.game_time_enabled=false; sky.current_time=10
 await shot(output.path_join("world.png"))
 var first=null
 var distance=INF
 for child in trees.get_children():
  if child is MultiMeshInstance3D and child.name.ends_with("_Near"):
   for i in child.multimesh.instance_count:
    var pos=child.to_global(child.multimesh.get_instance_transform(i).origin)
    var d=pos.distance_to(world.get_node("Explorer").position)
    if d<distance: first=pos; distance=d
 if first!=null:
  var camera=world.get_node("Explorer/Camera3D")
  var camera_pos=first+Vector3(6,0,8)
  var ground=world.get_node("Terrain3D").data.get_height(camera_pos)
  camera_pos.y=max(first.y+5,ground+2)
  camera.global_position=camera_pos
  camera.look_at(first+Vector3(0,7,0))
  await shot(output.path_join("close.png"))
 quit()
