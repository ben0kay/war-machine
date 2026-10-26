# Optional diagnostic runner for the actual TestWorld; gameplay scripts remain C#.
extends SceneTree
var failures = 0
var checks = 0
func check(ok, label):
 checks += 1
 if not ok: failures += 1
 print("PASS " if ok else "FAIL ", label)
func key(code, pressed):
 var event=InputEventKey.new()
 event.keycode=code; event.physical_keycode=code; event.pressed=pressed
 Input.parse_input_event(event)
 Input.flush_buffered_events()
func mouse(pressed):
 var event=InputEventMouseButton.new()
 event.button_index=MOUSE_BUTTON_RIGHT; event.pressed=pressed
 event.button_mask=MOUSE_BUTTON_MASK_RIGHT if pressed else 0
 Input.parse_input_event(event)
 Input.flush_buffered_events()
func _initialize(): call_deferred("run")
func run():
 var world=load("res://WORLD/testworld.tscn").instantiate()
 root.add_child(world)
 if DisplayServer.get_name()!="headless": root.grab_focus()
 for i in 4: await process_frame
 if DisplayServer.get_name()!="headless": Input.mouse_mode=Input.MOUSE_MODE_CAPTURED
 var trees=world.get_node("Trees")
 var player=world.get_node("Explorer")
 var fps=world.get_node("Controls/FPS")
 var terrain=world.get_node("Terrain3D")
 check(trees.ActiveTrees>0, "saved terrain receives generated trees")
 check(not trees.is_processing() and not trees.is_physics_processing(), "tree placement has no idle frame callbacks")
 var near=null
 var far=null
 for child in trees.get_children():
  if child.name.ends_with("_Near"): near=child; far=trees.get_node(str(child.name).trim_suffix("_Near")+"_Far"); break
 check(near!=null and far!=null, "near and distant mesh batches exist")
 var high=near.multimesh.mesh
 var low=far.multimesh.mesh
 var high_leaves=high.surface_get_arrays(1)[Mesh.ARRAY_VERTEX].size()/12
 var low_leaves=low.surface_get_arrays(1)[Mesh.ARRAY_VERTEX].size()/12
 check(high.get_surface_count()==2, "one wood and one leaf surface per tree mesh")
 check(high_leaves<=trees.Settings.LeafBudget, "near mesh obeys leaf budget: "+str(high_leaves))
 check(low_leaves<high_leaves/2, "far leaf mesh is substantially cheaper: "+str(low_leaves))
 check(near.visibility_range_end==110 and far.visibility_range_begin==110, "native LOD switch is configured")
 check(far.visibility_range_end==240 and far.visibility_range_end_margin>0, "native tree distance fading is configured")
 check(far.cast_shadow==GeometryInstance3D.SHADOW_CASTING_SETTING_OFF, "distant tree shadows default off")
 check(near.extra_cull_margin>=1, "leaf wind has culling margin")
 var roots=near.multimesh.get_instance_transform(0)
 check(near.position.is_equal_approx(far.position) and roots.is_equal_approx(far.multimesh.get_instance_transform(0)), "LOD roots and tree variant transforms match")
 check(near.position.length_squared()>100, "range culling is anchored to the chunk, not world origin")
 if DisplayServer.get_name()!="headless":
  var root_pos=near.to_global(roots.origin)
  check(abs(root_pos.y-terrain.data.get_height(root_pos)+0.1)<0.03, "chunk-local transform preserves the actual terrain root height")
 var node_count=trees.get_child_count()
 var chunks=trees.ActiveChunks
 var count=trees.ActiveTrees
 player.position.x+=0.1
 for i in 2: await process_frame
 check(trees.get_child_count()==node_count and trees.ActiveChunks==chunks and trees.ActiveTrees==count, "sub-chunk movement does not rebuild trees")
 check(fps.text.begins_with("FPS:"), "FPS counter appears at top left")
 check(not fps.is_processing(), "FPS label has no per-frame script")
 var timer=fps.get_child(0)
 check(timer.wait_time==0.5 and not timer.is_stopped(), "FPS updates at two hertz")
 fps.hide()
 check(timer.is_stopped(), "hidden FPS counter stops its timer")
 fps.show()
 check(not timer.is_stopped(), "showing FPS counter restarts its timer")
 # Synthetic input exercises the actual physical-key poller without requiring an OS keyboard.
 for pair in [[KEY_UP,-1,2],[KEY_DOWN,1,2],[KEY_LEFT,-1,0],[KEY_RIGHT,1,0]]:
  key(pair[0],true)
  var direction=player.call("Direction")
  var axis=player.get_node("Camera3D").global_basis.z if pair[2]==2 else player.global_basis.x
  check(direction.dot(axis)*pair[1]>0.9, "arrow mapping "+str(pair[0]))
  key(pair[0],false)
 key(KEY_CTRL,true)
 check(player.call("Direction").is_equal_approx(Vector3.DOWN), "Ctrl moves vertically down")
 key(KEY_CTRL,false)
 mouse(true)
 check(player.call("Direction").is_equal_approx(Vector3.UP), "RMB moves vertically up")
 if DisplayServer.get_name()!="headless": check(player.is_physics_processing(), "mouse-only input wakes flight")
 else: print("SKIP mouse-only wake requires an OS window with captured mouse")
 mouse(false)
 check(not player.is_physics_processing(), "releasing mouse-only movement sleeps flight")
 key(KEY_W,true); key(KEY_UP,true)
 check(is_equal_approx(player.call("Direction").length(),1), "W plus Up does not double forward speed")
 key(KEY_W,false); key(KEY_UP,false)
 if DisplayServer.get_name()!="headless" and root.has_focus():
  var start=player.global_position
  mouse(true)
  await physics_frame; await physics_frame
  mouse(false)
  check(player.global_position.y>start.y+0.1, "holding RMB actually moves the explorer upwards")
  start=player.global_position
  key(KEY_CTRL,true)
  await physics_frame; await physics_frame
  key(KEY_CTRL,false)
  check(player.global_position.y<start.y-0.1, "holding Ctrl actually moves the explorer downwards")
  start=player.global_position
  var forward=-player.get_node("Camera3D").global_basis.z
  key(KEY_UP,true)
  await physics_frame; await physics_frame
  key(KEY_UP,false)
  check((player.global_position-start).dot(forward)>0.1, "holding Up actually flies forwards")
 var h=terrain.data.get_height(Vector3(512,0,620))
 check(abs(h-47.9678688)<0.0001, "saved terrain remains unchanged")
 trees.Settings.Enabled=false; trees.ApplySettings()
 for i in 2: await process_frame
 check(trees.get_child_count()==0 and trees.ActiveTrees==0, "disabled trees free both LOD batches")
 trees.Settings.Enabled=true; trees.ApplySettings()
 check(trees.ActiveTrees>0, "tree batches reactivate")
 world.queue_free(); await process_frame
 print("RESULT ", checks, " checks, ", failures, " failures")
 quit(1 if failures else 0)
