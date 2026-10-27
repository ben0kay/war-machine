# Shared gameplay debug overlay

Instance `DEBUG/Overlay/DebugOverlay.tscn` once beneath the active CanvasLayer. World.tscn does this under DebugUIRoot/Controls; FirstPersonTest inherits the same scene. WorldController binds the ready references after environment, vegetation, fog and zombie-debug initialization. No global lookups or independently created FPS/top-down panel.

- DebugOverlay.tscn: shared editable shell (panel/margins/hint/scroll/content/copy button).
- DebugOverlay.cs: one builder/binding owner for day/night, weather enable/rain/snow/mist/motes, wind, ground haze, foliage, zombie sight toggle and common clipboard snapshot.
- IDebugOverlayView: only view-specific controls and snapshot values vary. FPS FirstPersonFog implements it, preserving spherical fog + linked rendering/far plane. Without an adapter, the current overhead camera controls and player-centred distance-fog controls are used.
- GroundHazeControls remains the shared haze builder under ENVIRONMENT.

Both views now get Foliage and Debug tools, including F7-synchronized zombie sight controls. Resource references are the current view's runtime copies: FPS keeps its own grass preset and fog profile; common controls do not force identical values. FPS hides overhead orbit/zoom/projection tools. Camera rotation/movement implementation and settings were not edited in this task.

To add/change a common tool, edit DebugOverlay.cs once. Change shell styling/layout in DebugOverlay.tscn. Keep genuinely different behaviour in a small adapter, not a second common control builder. The older WorldTuningPanel.cs and FirstPersonWeatherPanel.cs builders are removed.

No new Process/PhysicsProcess, polling, timers or scene/actor scans. UI input and existing Sky3D/camera/debug signals update controls. Density/size rebuild sliders apply on release. Teardown disconnects external clock/camera/vision subscriptions. Copy exports shared fields and effective view-specific fog values, without FPS reporting unused overhead camera settings. Live edits are temporary, not saved master changes.

**Validation:** source/loop syntax, scene resource IDs, scene-only overlay creation, adapter and startup/cleanup paths checked. Godot/.NET unavailable: **compilation and runtime layout/input checks not verified**. Locally run `dotnet build war-machine.csproj`; F6 World and FPS in turn. Check one DebugOverlay in Remote tree; open each common category, adjust time/weather/haze/foliage, toggle sight cones and F7, copy settings. Verify overhead camera controls remain and FPS only gets its spherical fog/render adapter. Escape releases FPS pointer, clicking UI does not fire/recapture, clicking world resumes; reload both scenes. No rebake required.
