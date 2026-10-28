# Door001 — DOOR1 physical hinged service door

**Status (2026-10-28): source implemented on development branch; local .NET/Godot playtest pending.** Reusable `Door001.tscn` is an independent non-pickup world asset. The 24×16m `ConvenienceStore001` instances exactly **one** at its **rear exterior exit**; the **front store doorway remains open**, and all three interior openings remain open. No global physical-props/pickup/door-interaction framework was implemented.

## Node construction at runtime

```text
Door001 (Node3D, HingedDoor001.cs)            # Root at hinge X/Z, local Y=0
  HingeAnchor (StaticBody3D, shape-less)       # fixed building/world anchor
  Leaf (RigidBody3D, collisions on layer 1)
    Panel (MeshInstance3D)                    # matte steel service-door slab
    DoorCollision (CollisionShape3D)          # solid, matches slab
    HandleOutside / HandleInside (Marker3D)   # future DOOR2 grab/pull points
    HandlePlateOutside / Inside (MeshInstance3D)
  HingeJoint3D (HingeJoint3D, +Y axis)         # StaticBody3D → RigidBody3D
```

The leaf rotates physically about the vertical **left hinge**, constrained to ±110° by default with **no motor or locking**. Player `CharacterBody3D.MoveAndSlide()` should push the door on contact; **no separate player impulses** or input actions added. RigidBody3D can sleep once settled. Both sides have handle anchors (DOOR2), which move with the leaf; handle click/drag, lock/latch, automatic closing and saving door angles are not DOOR1.

Tuning is through [Configuration/DoorMaster.tres](Configuration/DoorMaster.tres) and the typed `HingedDoorSettings.cs` (1.38m wide, 2.60m tall, 0.075m thick, 0.18m bottom gap; 9kg, angular damp 2.2, ±110°, handle height 1.15m). Door visual finish is generated once from that shared master; no textures, GLB source, animated nodes or per-frame C# processing. `HingeJoint3D` and `RigidBody3D` perform ordinary engine physics only when needed.

## Placement / fade / aiming integration

The building generator has a narrower **1.52m rear wall opening** inside the unchanged **2.6m service corridor**. The door's hinge root is placed at `(-0.69, 0, rearZ)` under the existing `RearWall` logical fade group; the jamb, header and wall still have their original static collision and their own narrow obstruction proxies.

`CameraOccluder` collects the panel and handle plates for **rear-wall grouped fading**, but deliberately **does not bake obstruction proxies** for their moving meshes. `structure_camera_blocker=false` keeps them out of the external stationary-triangle cache. Thus an opened door never blocks camera sight at its previous closed location; actual dynamic door collision always remains solid. `FadeAwareAim.Link(DoorCollision, Panel)` allows **top-down cursor aim** through the door while its wall is faded, but **firearm/bullet rays and character/zombie collision always hit the real leaf at its current physics position**. Dynamic door is not itself an independently tracked obstruction-triggering section in DOOR1; nearby fixed wall sections still drive the wall group's fade.

**Placement requirement:** prefab root must be immobile (building/world-local transform), with uniform scaling. The RigidBody3D leaf moves; do not parent the door root to another moving physics body. Door mesh/handle plates are under the correct wall fade group, but nothing attempts to move or overwrite the leaf's physics transform from camera code.

## Local acceptance

1. Pull/build C# and F6 `WORLD/World.tscn`; verify the **front entrance is still empty**, and only the single back exterior door has a hinged solid panel and two metal handle plates.
2. Walk into the rear panel from inside and outside in both top-down and FPS. It should swing about a fixed jamb hinge with limits, gradually slow, settle, and not fall, translate off hinge or clip into walls/terrain.
3. Verify it swings from either side, retains collision when closed/faded, and stops movement and bullets when physically in their path. Check the rear service walkway and interior connecting passage remain navigable.
4. Walk behind the rear wall and inspect its grouped fade. Door panel/plates follow wall opacity; no stationary ghost blocker at its original closed pose after opening. Check AIM1 cursor targeting while faded versus actual bullet hits.
5. Inspect Godot Output for joint warnings, missing resources or invalid physics RIDs. Test repeated scene entry/reload and FPS inheritance. If pushing is too weak/strong, tune `MassKg`/`AngularDamping` first; DOOR1 deliberately does not modify general player movement or global physics settings.

This is source/documentation acceptance only. A rendered Godot test, physics simulation and FPS measurement have **not** been performed remotely.
