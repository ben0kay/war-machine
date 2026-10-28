> **ELD2 source implementation (2026-10-28), awaiting local validation:** [Injury locomotion and World/FPS acceptance](../Anatomy/README.md). Both variants share limp, one-leg shuffle, low living crawl and immobilization. Debug panel buttons demonstrate injuries without HP changes. Existing attacks/ELD3 and ZD1–ZD3 remain deferred. This supersedes older ELD2-Planned/upright-legless descriptions below.

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

## Persistent FPS indicator above the panel (2026-10-28)

The active `WORLD/World.tscn` owns a sibling `DebugUIRoot/Controls/FPS` label backed by the existing `DEBUG/FpsCounter.cs` and `DebugMaster.tres`, **not a control inside DebugOverlay**. This keeps the FPS label visible while panels/sections are collapsed and in both the inherited FPS test world and top-down world. The floating debug panel begins at top-left Y=148 while the counter starts at Y=112, leaving a small gap. Engine FPS updates on its throttled timer and interpolates text colour green (>=60), yellow (40), orange (30), red (<=20). The editor-only Legacy TestWorld uses the same script; it now receives the same colour behaviour without any duplicate FPS node in the active world.

Do not move FPS inside the scroll container or wire it to debug visibility; `FpsCounter.ApplySettings()` explicitly stops its timer when the FPS label is disabled. No panel-control feature or camera adapter has been changed. Local Godot scene/layout validation pending.


## Top-down orbit input update — 2026-10-28

The Camera section no longer contains the yaw/rotation slider. It instead tells users to **hold middle mouse and drag horizontally** over the world to orbit the top-down camera. Yaw remains an authored/copied numeric setting, and the FOV, pitch, projection and zoom UI remain. The FPS adapter continues to hide this top-down camera section; its mouse-look is unchanged. See [CAMERA/README.md](../../CAMERA/README.md) for event and focus behaviour. Local build and manual top-down/FPS checks pending.
