# DEBUG — Lightweight world diagnostics

`FpsCounter.cs` displays the native Godot FPS measurement in TestWorld's top-left overlay. `Configuration/DebugMaster.tres` / `DebugConfig.cs` control ShowFps and FpsUpdateSeconds (default 0.5s).

One Timer samples FPS twice per second; text is assigned only when the integer changes. No C# frame/physics polling is used. Hiding the label or setting ShowFps=false and calling ApplySettings stops the timer; showing it restarts the timer. The controls help text is positioned below the counter. This observes the world without altering rendering quality or simulation speed. FPS from software-rendered validation is not a target-PC benchmark.

## First-person experiment

`TEST/FirstPerson/FirstPersonTest.tscn` inherits the active world and keeps all new FPS camera/input/aim/settings under DEBUG/TEST. Build C# then F6 it. See TEST/FirstPerson/README.md; local compilation and gameplay validation pending.
