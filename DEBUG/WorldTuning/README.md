# World tuning

`WorldTuningPanel.cs` owns the active world's scrollable projection/FOV/tilt/zoom, day/night and weather controls plus clipboard export. It receives explicit camera/config/sky/weather references from WorldController. Runtime resources only; no saved master edits and no per-frame UI polling. The legacy FPS EnvironmentTuning panel remains separate and parked with that world.

Grass enabled/density/height/wind controls now target the existing TerrainGrass. Density/height commit on drag release. Copy settings includes their values. Shared wind controls update both precipitation and grass.

## Pine integration

Pine enabled/density/size/wind controls target PinePlacement; density/size commit on drag release. Copy settings includes pine values. Shared wind heading/strength update grass, weather and pines together.

The overhead panel now accepts the optional PlayerDistanceFog owner and exposes enabled, clear/full radius, opacity, edge variation and a color picker. Copy settings includes the distance-fog fields; edits invoke ApplySettings explicitly, with no UI polling. FPS continues to omit this panel/effect.

## Expandable top-down controls / haze 2.0

Camera, Environment and Foliage are disclosure buttons. Environment contains Distance fog, Day/night, Weather and shared wind, and Ground haze; Foliage contains Grass and Pine trees. Sections start collapsed and change visibility only on clicks. FPS panels are untouched. Haze enabled/density/height/noise/drift/definition/turbulence/colour apply on input; radius applies on drag release. Shared wind heading also reapplies haze. Copy settings includes all haze fields and wind vector. Runtime resources only; no UI polling. Local Godot layout/input/clipboard acceptance pending.

The scroll area grows/shrinks with expanded content via MinimumSizeChanged, capped to the viewport; collapsed sections do not leave a full-height blank panel.

## Debug — zombie sight cones

The left-hand top-down controls now include a top-level **Debug** section with a **Show zombie sight cones** checkbox. It is connected to the existing world-local `ZombieVisionDebug` manager and the **F7** shortcut, so either changes the same state and F7 updates the checkbox without emitting a duplicate toggle. All current zombie variants' red fan meshes are affected; sight/LOS and AI are unchanged. Future registered zombies inherit current debug visibility while preserving their `ZombieSettings.ShowVisionCone` authoring flag. No per-frame work or gameplay tuning settings added; changes are runtime-only. Local Godot/.NET build and visual checks pending.

## Camera rotation and bounded zoom

The top-level Camera disclosure now includes a live **Camera rotation** slider (0–360° yaw). `Camera.SetYaw()` orbits around the player using the already-authored `CameraYawDegrees` property, with existing player-relative movement/aim hooks retained. The zoom slider reads the configured bounds for the current projection instead of using hard-coded perspective values. Default orthographic view size is 15–40, perspective distance 15–50m. Both mouse wheel and slider follow the same camera clamps. Copy settings also includes the new perspective min/max fields. All actions are event-driven; no additional idle callback. FPS has no new camera slider. Local build and F6 visual validation pending.
