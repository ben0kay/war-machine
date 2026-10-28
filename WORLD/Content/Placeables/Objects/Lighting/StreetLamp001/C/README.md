# StreetLampMesh — reusable one-shot fixture + two real Godot lights

`StreetLampMesh.cs` is a `[Tool]` script attached to `StreetLamp001.tscn`. It builds one tapered lamp post, foot, angled arm, recessed LED fixture and simple pole/base static collision once. The scene itself owns two stable child nodes: `Lighting/Downlight` (`SpotLight3D`) and `Lighting/AmbientFill` (`OmniLight3D`). `_Ready` positions/configures both from `StreetLampSettings` once and disables processing.

The downward spot creates the main broad circular ground illumination; a dimmer shadowless omni provides dispersed fill. `SetLampEnabled(bool)` toggles native nodes and the LED panel's emission without an idle loop; later day/night scheduling or an optional flicker component belongs in site/ENVIRONMENT lighting ownership. No world placement or terrain dependencies, no per-lamp timers.
