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

## Orbit rotation and tighter zoom bounds — awaiting local validation

The top-down camera panel now exposes a **Camera rotation** yaw slider from 0–360° (45° default). `Camera.SetYaw()` orbits the follow camera about the current player target, preserving tilt, projection and zoom; one input event updates the camera transform and triggers the existing view/settings notifications. Camera-relative walking and aiming continue using the updated camera basis. Rotation has no idle `_Process` cost and does not alter the FPS debug eye-view implementation.

The tighter bounds are saved in `Configuration/CameraMaster.tres`: orthographic view size **15–40** (previously 20–80), perspective camera distance **15–50m** (previously 20–100m). This makes the near bound 25% smaller and the far bound 50% smaller in each mode. `CameraSettings` now exports `MinimumPerspectiveDistance` and `MaximumPerspectiveDistance` so perspective does not keep hard-coded limits. Wheel scrolling, Camera panel slider, projection switching and desired camera offset all use the same effective limits; the orthographic initial size was reduced to 40 to fit the new allowed range. Perspective starts at its existing 36.5m, 50.5° FOV and 40° pitch. Runtime resource copying and existing follow smoothing remain unchanged.

**Validation pending:** no Godot/.NET runtime available to compile or visually check. Locally build `dotnet build war-machine.csproj` and F6 `WORLD/World.tscn`. Expand Camera, rotate yaw through 0/90/180/270/360° while stationary and moving, confirm player stays centered and screen-relative arrows/mouse aim remain correct. Zoom to 15/50m in perspective and 15/40 orthographic with wheel and slider; switch projection at boundaries and test occluder fading. Briefly F6 the FPS debug scene to check unchanged camera operation. No terrain rebake required.

## Bounded overhead view — 2026-10-28

Owner requested half the previous maximum zoom-out and no near-horizontal camera. Perspective maximum distance is now 25m (was 50); orthographic maximum size is 20m (was 40). Starting values match those maxima, keeping startup within bounds. Minimums remain 15. Downward pitch defaults to 35° and is clamped 35–80° during initialization and SetPitch; the debug slider matches. FOV/yaw behavior is unchanged.

Overhead FarClipDistance is now an Inspector setting, default 256m instead of hard-coded 1800m (runtime safety 64–2048). It is a camera clipping limit, not a zoom distance or a simulation/unload radius. Frustum culling already rejects ordinary off-screen geometry; shadow passes, batched instances, particles and CPU simulation have separate costs. This scoped change does not promise to fix every movement/zoom hitch. FPS initialization owns its own far plane and its fog-linked clipping remains unchanged. Source/config checks only; no Godot/dotnet available. Locally verify wheel and slider endpoints in both projections, pitch minimum, startup framing, terrain/fog edge and no FPS regression.
