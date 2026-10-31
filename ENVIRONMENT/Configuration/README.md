# Environment configuration

`EnvironmentMaster.tres` is the saved, Inspector-editable master resource. Its C# resource class lives one folder above. Select the resource in Godot to tune grouped values. Changes apply on the next scene launch; no per-frame config polling.

## Saved ambient and ground haze defaults — 2026-10-28

The shared active world environment scene references `GameplayEnvironmentMaster.tres`, which now has `NightAmbientEnergy=1.5` and `GroundHazeDriftSpeed=1.5`m/s. The legacy `EnvironmentMaster.tres` and `EnvironmentConfig.cs` fallback use the same values. These are start/reload defaults, not changes to running UI copies. Daytime ambient, moonlight and other fog/weather settings are unchanged. Streetlamp cone tuning is independent under `Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres`.

## Saved active ground-haze preset (2026-10-31)

The actual `ENVIRONMENT/Environment.tscn` references `GameplayEnvironmentMaster.tres` for both gameplay views; this is the authoritative master for the owner's F1 ground-haze snapshot. Active saved defaults: **enabled, density 0.16, height 0.7m, noise scale 0.24, drift speed 1.04, contrast 0.21, turbulence 0.27 (rounded from F1 float output 0.26999998), coverage radius 23m, colour (0.64, 0.71, 0.73, 1)**. All unrelated atmosphere/weather fields are untouched. The legacy `EnvironmentMaster.tres` is deliberately not modified. This preset is authored source; local Godot/FPS visual check pending.
