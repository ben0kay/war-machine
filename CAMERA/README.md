# CAMERA

`Camera.cs` lives directly here, as requested. `Camera.tscn` is the reusable active Camera3D scene; `CameraSettings.cs` and `Configuration/CameraMaster.tres` own its projection, FOV, angle, distance, zoom and follow tuning. WorldController supplies a runtime copy and the player reference, and registers this camera with Terrain3D.

Orthographic/perspective selection, FOV, tilt and wheel/distance zoom retain existing behaviour. Player position changes and changed camera inputs wake interpolation; it stops when settled. ViewChanged continues to wake the player weapon's aim adapter. Debug sliders live under DEBUG/WorldTuning, not in the player or camera controller. The parked FPS camera remains inside PLAYER/LegacyFirstPerson/Explorer.tscn.

## Camera obstruction fading

Scenery obstruction fading now lives in Occlusion/. CameraOcclusion owns bounded checks and fade interpolation; CameraOccluder opts buildings/walls into a dedicated camera-only layer. Pines supply per-instance canopy bounds/fading. Entities never fade. See Occlusion/README.md for settings, lifecycle and pending local checks.

DEBUG/TEST/FirstPerson/CAMERA subclasses the camera initialization contract for an event-driven eye view. Its implementation and settings stay in the test folder; the active overhead camera retains existing behavior.

## Movement jitter fix — awaiting local validation

Physics interpolation is enabled in project.godot so fixed-tick moving visuals render smoothly between ticks. The overhead camera opts out of automatic interpolation and samples PlayerController.GetGlobalTransformInterpolated().Origin during its existing render callback, following the rendered root instead of raw physics steps. Existing exponential follow smoothing, framing, zoom, projection and movement/collision settings remain unchanged. Initialization resets player interpolation after safe placement/generated rig setup to avoid spawn streaking; future teleport code must likewise call ResetPhysicsInterpolation() after moving the actor.

PositionChanged still wakes camera follow; the callback stays awake through the last movement tick and follow/zoom settling, then sleeps. No new polling loop, scene scans, allocations or per-entity callbacks. One interpolated target query per active camera frame. FPS retains its separate manual eye interpolation and automatic-camera opt-out; project-wide interpolation also affects other inheriting 3D visuals, so check their presentation locally.

Validation: focused source/API and for/foreach review; no new loops. Godot/.NET are unavailable here: **compilation not verified**. Locally run `dotnet build war-machine.csproj`, then F6 WORLD/World.tscn: walk/run left-right and diagonally, stop/restart, cross slopes/stairs, aim/fire, zoom and switch projection. Expect smooth translation with unchanged controls and settled follow. Check FPS mouse look/movement and zombie visuals after the project-wide setting change. Terrain-specific vertical corrections remain a separate possible cause if jitter persists. No measured performance or visual acceptance claimed.
