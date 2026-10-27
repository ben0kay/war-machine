# FPS environment controls — one left-hand panel

The FPS test inherits ENVIRONMENT/Environment.tscn through World.tscn, with the same Sky3D, WeatherBridge, GroundHaze, shader and GameplayEnvironmentMaster defaults. See [shared scene guide](../../../../ENVIRONMENT/SHARED_ENVIRONMENT_README.md).

FirstPersonWorld builds one always-visible panel: Environment → Day/night, Weather, Wind, Ground haze, Distance fog; Rendering/culling is a second main button. Press Escape for a cursor, expand/collapse and edit, then click outside UI to resume mouse look. F8 and F9 no longer open separate panels. Camera recapture checks hovered GUI controls; UI clicks do not fire. Existing bindings/copy/time signals remain, with no UI polling. FirstPersonWeatherPanel builds into that parent instead of creating its own overlay. GroundHazeControls is shared with top-down.

FirstPersonFog remains the FPS-only spherical distance fog and linked Far adapter. Defaults are still the FPS master (start 1m, end 100m, buffer 120m, existing colour). Weather/haze defaults come from the shared environment master. Runtime edits are temporary. Foliage stays in existing owners and no new foliage controls were added.

Source checks completed; compilation and layout/input/visual validation pending locally (Godot/.NET unavailable). Build, F6 FPS, Escape, expand all categories, edit weather/time/haze/fog, copy, click world, shoot, reload, and briefly check top-down. No rebake required. This replaces all earlier F8/F9 split-panel instructions.
