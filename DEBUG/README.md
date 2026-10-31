# DEBUG — Lightweight world diagnostics

`FpsCounter.cs` displays the native Godot FPS measurement in TestWorld's top-left overlay. `Configuration/DebugMaster.tres` / `DebugConfig.cs` control ShowFps and FpsUpdateSeconds (default 0.5s).

One Timer samples FPS twice per second; text is assigned only when the integer changes. No C# frame/physics polling is used. Hiding the label or setting ShowFps=false and calling ApplySettings stops the timer; showing it restarts the timer. The controls help text is positioned below the counter. This observes the world without altering rendering quality or simulation speed. FPS from software-rendered validation is not a target-PC benchmark.

## First-person experiment

`TEST/FirstPerson/FirstPersonTest.tscn` inherits the active world and keeps all new FPS camera/input/aim/settings under DEBUG/TEST. Build C# then F6 it. See TEST/FirstPerson/README.md; local compilation and gameplay validation pending.

## F1 placeable model previews

The isolated [PlaceableSpawner](PlaceableSpawner/README.md) provides an F1 menu in the shared overhead and FPS scenes. Bookcase001 and Book001 appear as **visual-only GLB models** ahead of the player. Clear removes debug instances, and no physics/interaction is activated.

## Shared FPS counter in the active World and FPS scenes (2026-10-28)

The **current** `WORLD/World.tscn` now instantiates the same `FpsCounter.cs` directly under `DebugUIRoot/Controls/FPS`. The FPS test scene inherits that UI as-is: **one counter, not two**. The counter sits at X=18, Y=112 immediately above the left `DebugOverlay` panel (top Y=148); the existing upper-left controls/status help remains above it. The counter belongs to the CanvasLayer, not the scrollable panel, so opening/closing debug sections, moving or changing cameras does not hide it.

The label reads `Engine.GetFramesPerSecond()` at `DebugMaster.tres`'s default **0.5-second interval**; it changes text and font colour only when FPS changes. Smooth red (20 or below) → orange (30) → yellow (40) → green (60 or above) transition. Text has a shadow for readability on grass/concrete. As with the legacy TestWorld, `ShowFps=false` disables the label and stops the timer. No `_Process`, `_PhysicsProcess`, rendering settings or other diagnostic UI were changed.

Local validation pending: `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`, check FPS counter above the left debug panel in both views and verify it remains visible independently of panel interactions. Verify green at >60 when possible, and progressive warm colours under heavy load. If there are unusual five-line status warnings, confirm the text does not overlap the counter; adjust panel/status layout if necessary. FPS is a Godot measurement, not a benchmark guarantee.


## DF2.7 — permanent top-left FPS/TRIS readout (2026-10-31)

**Implemented in source; Godot build and local display acceptance pending.** `WORLD/World.tscn` now puts **one** existing `FpsCounter` label under `DebugUIRoot/PerformanceHud/FPS`, separate from the F1-controlled `DebugUIRoot/Controls`. FPS remains colour-coded, now at X=12/Y=6 with 13px font; a smaller pale `TRIS` label sits directly beneath it. F1 no longer hides either value, and the old debug FPS line is removed rather than duplicated. The F1-only Status label starts at X=204 to avoid the small top-left readout. Both controls ignore mouse input.

The existing 0.5s timer updates **both** counters. Godot's `RenderingServer.ViewportGetRenderInfo(... Visible, PrimitivesInFrame)` reports 3D points/lines/triangles submitted for the *visible* viewport render pass; it excludes shadow passes and is **not the count of unique triangles stored in all scene assets**. It can read 0 until a couple of rendered frames have elapsed. When hidden by `DebugMaster.ShowFps=false`, the timer sleeps. Other scenes using `FpsCounter` without an optional `Triangles` label still show just FPS. No global graphics settings changed. Test in the shared World and inherited FirstPersonWorld scenes; verify FPS/TRIS while F1 is closed and open, viewport movement, palette, and legibility at top-left.
