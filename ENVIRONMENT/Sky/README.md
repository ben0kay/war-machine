# Sky3D integration

`WildernessSky.tscn` contains the actual installed Sky3D scripts and shader. SunLight/MoonLight are explicitly serialized children; SkyDome and TimeOfDay reference the installed addon APIs. This scene is reused by the world.

`SkyController.cs` changes `current_time`, `minutes_per_day`, `update_interval` and `game_time_enabled` through Godot's dynamic API, because Sky3D is GDScript. All project-owned runtime code remains C#. No third-party source modifications are required.

`SkyController.ApplyMist()` applies EnvironmentMaster FogEnabled, FogDensity, FogStart, FogEnd and FogFalloff to the real SkyDome API. Density=0.025, start=0m, end=200m are the requested closer baseline; end is clamped above start. Static mist does no custom per-frame CPU work. The shared grass wind is consumed by VEGETATION, not by an obsolete grass material in SkyController.

## Sky3D time event consumers (2026-10-28)

`SkyController.TimeChanged(float hour)` now mirrors the existing Sky3D `TimeOfDay.time_changed` signal, without any competing clock. The global `StreetLampSchedule` listens to that event and applies the editable `StreetLampScheduleMaster.tres` (18:30 ON / 06:30 OFF by default) to current and later-spawned fixtures. Changing debug Sky3D time through `ApplyTime` also fires the same event. Sky3D's original atmospheric computations, paused state and clock speeds are unaffected. Local validation pending.
