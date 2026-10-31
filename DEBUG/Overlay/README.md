> **ZD3:** Debug → Print corpse budgets reports registered/active/settled/sinking counts and fallback/retirement totals on click, in both views. No polling. Local build/runtime validation pending.

> **Zombie render isolation (2026-10-28):** Debug builds expose **Debug → Hide zombie visuals + corpses** in World/FPS. Visibility only; AI/physics/animation keep running. See [ownership and same-view comparison](../Perception/README.md). Source implemented; local build/runtime validation pending.

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

## STG2 — live global foliage and imported mesh grass controls (2026-10-30)

**F1 → Foliage → Shared tree + grass tint:** a live colour picker and strength slider (0–1) call `RenderingServer.GlobalShaderParameterSet` for `wm_foliage_tint` and `wm_foliage_tint_strength`. The Godot shader global values are read only twice when the panel is built, because rendering-thread synchronized GET is expensive; there is **no continuous polling**. All active shared-palette foliage (stylized grass, stylized pines and DF2 broadleaf crowns) reacts. Original mesh texturing/vertex colors and local wind shaders remain. Live edits are temporary and appear in Copy settings; for permanent palette edit Godot Project Settings → Shader Globals.

**F1 → Foliage → Grass:** now routes controls to the imported `StylizedGrassPlacement` when active (instead of the disabled particle grass). Enable, patch count/chunk, clumps/patch, max patch spread, max grass scale, wind; patch rebuilding waits for slider release. If no stylized placement exists, legacy grass controls appear. Weather wind strength/heading also calls the new `StylizedGrassPlacement.ApplyWind()`. No `_Process`, no per-clump nodes or new global searches. Build and runtime check pending.

## GD1: F1 groups both debug surfaces, adds global scene grading (2026-10-31)

`WORLD/WorldController.cs` now owns the **only F1 handler**. F1 opens/closes the shared left `DebugUIRoot/Controls` CanvasLayer (panel, FPS, status) **and** the independent right `PlaceableSpawnMenu` layer, together. Default is hidden after startup for unobstructed FPS/top-down play; startup errors still show in the Status label until world initialization completes. The right panel's Close button and Escape also request unified closure. FPS mouse capture is released for menu use and restored on F1 close if it was captured on open. Existing F7 visuals/overlay, spawning/clearing, preview instances and gameplay HUD are untouched.

`DEBUG/ColorGrade/` is a standalone optional one-pass screen-space `ColorRect` grade, using `CanvasLayer.Layer=0`, below HUD layer 2, debug layer 1 and spawner layer 20. Its `WorldColorGrade.cs` uses a single shader `Shaders/GlobalColorGrade.gdshader` to expose live **Hue shift −180..180°, Saturation 0..2, Contrast 0.5..1.5 and Brightness 0.5..1.5** via **F1 → Colour grading**. The source UI does not need a new sky/WorldEnvironment; the effect is on the final world image. Neutral settings (0/1/1/1) **hide the ColorRect and run no full-screen pass**; only non-neutral values incur one screen-texture read/draw per frame. The grading master remains neutral by default for no visual regression. `ColorGradeMaster.tres` owns saved values; sliders change a runtime deep copy only and export through Copy settings. Shader is a general LDR colour grade, not HDR white balance/exposure and does not affect gameplay HUD colours; assess colour/fog/haze at sunrise, noon and nighttime.

**Visibility:** spawner's full-screen background no longer intercepts mouse input or dims/blocks the left F1 controls; its right-side panel remains clickable. F1 shows BOTH, F1 again hides BOTH, Escape/Close hide BOTH. UI event ownership is only WorldController, not the spawner; no per-frame loops or duplicated input scan. Build/F6 runtime checks still required in both FPS and World scenes.
