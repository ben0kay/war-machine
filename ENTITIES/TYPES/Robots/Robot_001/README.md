# Robot_001 — RM1 visual walker

**Implemented, Awaiting Validation.** Original dark industrial quadruped, approximately 11 metres tall at default scale, with four two-bone legs, broad foot pads, armoured chassis and an independently rotating cyan-optic head. No weapons, patrol, beam or detection. TestWorld is unchanged: world placement is RM4.

## Use and settings

Instantiate `Robot_001.tscn` with its origin at flat ground level, or drag it into a temporary local inspection scene. Art is generated at runtime (not in the editor). The inherited actor has one coarse capsule; legs have no collision. The procedural unit box and joint sphere meshes are shared across instances; each actor creates three finish materials once, outside the animation loop.

`Configuration/RobotMaster.tres` references `C/RobotSettings.cs`. Each actor duplicates the resource into RuntimeRobotSettings before building. Art dimensions, scale, lengths, stance, colours, emission and initial preview are spawn-time settings. Head yaw limits are read by SetHeadYaw. Change dimensions before spawning, then recreate the actor; there is no live rebuild or F9 panel in RM1. Keep positive dimensions, compatible leg lengths/hip height and a reachable stance. Nonuniform actor scale is unsupported.

- Set PreviewWalking on an instance-specific master before spawning, or call `SetWalkPreview(true)` after Ready, to run a stationary treadmill preview. `SetWalkPreview(false)` settles over SettleSeconds then sleeps.
- Later movement calls `SetMotionSpeed(actualMetresPerSecond)` when speed changes; feet use cached world-space targets and diagonal swing pairs. This phase does not move the root.
- `SetHeadYaw(degrees)` rotates only HeadPivot within the configured limit. `SensorSocket` points along local -Z for a later beam.
- `SetVisualRelevant(false)` suspends animation; the screen notifier also handles offscreen suspension. Use actor Activate/Deactivate for whole-actor lifecycle. No distance polling is added.

## Replaceable rig contract

Generated hierarchy: VisualRoot → BodyRoot → HeadPivot → SensorSocket. BodyRoot also owns FrontLeftHip, FrontRightHip, RearLeftHip and RearRightHip; each hip owns Knee, and each Knee owns Foot. Mesh geometry is attached beneath these pivots with bones along local -Z. Upper/lower segments end at the configured lengths. BodyRoot is at HipHeight; pads sit above ground-level Foot pivots.

An optional ArtOverride PackedScene must expose the same BodyRoot hierarchy under an identity art root, matching these units and segment lengths. It replaces mesh generation without changing gait or future patrol/sensor code. An arbitrary unrigged GLB needs a socket/pivot wrapper first. Head and leg transforms are animator-owned; locomotion owns the physics root.

## Performance and limits

One active Process callback updates four legs with cached references/arrays and two-bone maths. Target correction is optional and invoked only on initial stance/new swing targets; no physics raycasts or Terrain3D queries are performed by default. Flat-ground preview is the RM1 scope. Ground adaptation, actual locomotion and full terrain-aware placement remain later integration work.

Wake: requested preview/motion on an active, visible/relevant robot, or reactivation. Stop: bounded settling after speed/preview reaches zero. Offscreen/hidden art stops callbacks immediately and keeps its pose. Deactivate returns to neutral stance immediately and stops processing; ExitTree disconnects visibility signals. There are no timers, external subscriptions, per-leg callbacks or idle sensory queries. Rendering the stationary mesh and coarse collision still have engine costs; no profiler measurements are claimed.

## Brief validation handoff

Source review, resource-path checks and `git diff --check` performed. No dotnet/Godot executable is available here, so compilation, runtime rig appearance and screen-notifier behaviour are **unverified**. Locally run `dotnet build war-machine.csproj`; instantiate at flat ground, enable preview, rotate the head, stop and observe settling, disable/re-enable, hide/show, move camera away/back, and remove/re-add/free. Try two instances with different masters to confirm settings isolation. Check foot reach, joint alignment, no shader/scene errors and no idle callback after settling. Detailed FPS profiling stays with the owner. No changes to terrain, vegetation, weather, player or TestWorld.
