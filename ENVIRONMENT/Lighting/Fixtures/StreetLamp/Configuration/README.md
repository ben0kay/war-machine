# StreetLampLightingMaster — all streetlamp illumination settings

`StreetLampLightingMaster.tres` is a typed `StreetLampLightingSettings` preset used by `../StreetLampLighting.tscn`. This is the **only** master governing the native spot/omni light properties and LED emission for `StreetLamp001`. It is not the Sky3D clock or the global muzzle-light cap.

Default: LightsEnabled true; warm (1, 0.82, 0.61) colour; spotlight energy 2.5, range 13m, 51° half cone, 0.85 spot attenuation, 1.45 angular edge attenuation, shadows OFF; fill energy 0.35, range 8m, 1.0 attenuation, enabled and shadowless; fog contribution 0.25 spot and 0.05 fill. These are unchanged from the old world fixture master.

Read once when light scene is instanced; `SetEnabled(bool)` changes active nodes and the optional bound panel, **not** this source .tres. Reinstantiate/reload after editing. Future time-of-day/relevance policies are separate from the preset itself.
