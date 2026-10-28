# StreetLampLightingMaster — all streetlamp illumination settings

`StreetLampLightingMaster.tres` is a typed `StreetLampLightingSettings` preset used by `../StreetLampLighting.tscn`. This is the **only** master governing the native spot/omni light properties and LED emission for `StreetLamp001`. It is not the Sky3D clock or the global muzzle-light cap.

Default: LightsEnabled true; warm (1, 0.82, 0.61) colour; spotlight energy 4.5, range 20m, 75.6° half cone, 0.85 spot attenuation, 1.45 angular edge attenuation, shadows OFF; fill energy 0.75, range 12m, 1.0 attenuation, enabled and shadowless; fog contribution 0.25 spot and 0.05 fill. These values were increased for the convenience-store site's brighter, broader coverage on 2026-10-28. The old settings were 2.5/13m/51° for spot and 0.35/8m for fill. Shadows remain OFF; fog values unchanged.

Read once when light scene is instanced; `SetEnabled(bool)` changes active nodes and the optional bound panel, **not** this source .tres. Reinstantiate/reload after editing. Future time-of-day/relevance policies are separate from the preset itself.

### More spread (2026-10-28)

`SpotHalfAngle=75.6` degrees (was 63; exactly +20% in angular value). The exported range and runtime clamp now permit values up to 85°. Default spotlight energy/range and fill values remain 4.5/20m and 0.75/12m. This is a shared setting used by all four convenience-store street lamps; still no shadow casting or additional lights.
