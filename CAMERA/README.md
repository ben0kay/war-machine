# CAMERA

`Camera.cs` lives directly here, as requested. `Camera.tscn` is the reusable active Camera3D scene; `CameraSettings.cs` and `Configuration/CameraMaster.tres` own its projection, FOV, angle, distance, zoom and follow tuning. WorldController supplies a runtime copy and the player reference, and registers this camera with Terrain3D.

Orthographic/perspective selection, FOV, tilt and wheel/distance zoom retain existing behaviour. Player position changes and changed camera inputs wake interpolation; it stops when settled. ViewChanged continues to wake the player weapon's aim adapter. Debug sliders live under DEBUG/WorldTuning, not in the player or camera controller. The parked FPS camera remains inside PLAYER/LegacyFirstPerson/Explorer.tscn.

## Camera obstruction fading

Scenery obstruction fading now lives in Occlusion/. CameraOcclusion owns bounded checks and fade interpolation; CameraOccluder opts buildings/walls into a dedicated camera-only layer. Pines supply per-instance canopy bounds/fading. Entities never fade. See Occlusion/README.md for settings, lifecycle and pending local checks.

DEBUG/TEST/FirstPerson/CAMERA subclasses the camera initialization contract for an event-driven eye view. Its implementation and settings stay in the test folder; the active overhead camera retains existing behavior.
