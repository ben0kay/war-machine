# StreetLampMaster — typed lamp configuration

`StreetLampMaster.tres` is now a `StreetLampSettings : Resource` master attached to `StreetLamp001.tscn`.

Groups:
- **Fixture geometry**: 5.8m pole height, 1.1m horizontal arm, metal/housing palette.
- **Light controls**: LightsEnabled (default true), warm colour, spot energy 2.5, range 13m, angular half-cone 51°, attenuation 0.85, angular-edge softness 1.45, shadows OFF.
- **Dispersed fill**: FillEnabled true, Omni energy 0.35, range 8m, attenuation 1.
- **Fog**: spot volumetric energy 0.25, omni volumetric energy 0.05.

These are authoring settings read **once on instance Ready**. Changing them in the resource requires re-instancing/reloading the scene; no per-frame tuning/writes. The site owner can later send `SetLampEnabled(bool)` for runtime on/off and a separate flicker controller, without rewriting the fixture geometry.
