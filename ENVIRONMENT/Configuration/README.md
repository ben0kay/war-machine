# Environment configuration

`EnvironmentMaster.tres` is the saved, Inspector-editable master resource. Its C# resource class lives one folder above. Select the resource in Godot to tune grouped values. Changes apply on the next scene launch; no per-frame config polling.

## Saved ambient and ground haze defaults — 2026-10-28

The shared active world environment scene references `GameplayEnvironmentMaster.tres`, which now has `NightAmbientEnergy=1.5` and `GroundHazeDriftSpeed=1.5`m/s. The legacy `EnvironmentMaster.tres` and `EnvironmentConfig.cs` fallback use the same values. These are start/reload defaults, not changes to running UI copies. Daytime ambient, moonlight and other fog/weather settings are unchanged. Streetlamp cone tuning is independent under `Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres`.
