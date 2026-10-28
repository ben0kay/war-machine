# StreetLampLightingMaster — all streetlamp illumination settings

`StreetLampLightingMaster.tres` is a typed `StreetLampLightingSettings` preset used by `../StreetLampLighting.tscn`. This is the **only** master governing the native spot/omni light properties and LED emission for `StreetLamp001`. It is not the Sky3D clock or the global muzzle-light cap.

Default: LightsEnabled true; warm (1, 0.82, 0.61) colour; spotlight energy 4.5, range 20m, 75.6° half cone, 0.85 spot attenuation, 1.45 angular edge attenuation, shadows OFF; fill energy 0.75, range 12m, 1.0 attenuation, enabled and shadowless; fog contribution 0.25 spot and 0.05 fill. These values were increased for the convenience-store site's brighter, broader coverage on 2026-10-28. The old settings were 2.5/13m/51° for spot and 0.35/8m for fill. Shadows remain OFF; fog values unchanged.

Read once when light scene is instanced; `SetEnabled(bool)` changes active nodes and the optional bound panel, **not** this source .tres. Reinstantiate/reload after editing. Future time-of-day/relevance policies are separate from the preset itself.

### More spread (2026-10-28)

`SpotHalfAngle=75.6` degrees (was 63; exactly +20% in angular value). The exported range and runtime clamp now permit values up to 85°. Default spotlight energy/range and fill values remain 4.5/20m and 0.75/12m. This is a shared setting used by all four convenience-store street lamps; still no shadow casting or additional lights.

## Global scheduling and translucent cone (2026-10-28)

**This folder now contains two distinct masters.** `StreetLampLightingMaster.tres` still governs **each light fixture's shared appearance** and contains `VisibleConeEnabled=true`, `VisibleConeOpacity=0.035`, `VisibleConeLength=5.8` m and `VisibleConeBaseRadius=3` m. The faint cone's colour uses existing `LightColor`. `StreetLampScheduleMaster.tres` is the **one global schedule for all lamps**: `AutomaticTimeSwitching=true`, `TurnOnHour=18.5` (18:30), `TurnOffHour=6.5` (06:30), in Sky3D in-game hours. Turn-on / turn-off spans midnight by default. Equal hours = always on; `AutomaticTimeSwitching=false` delegates to each fixture's existing LightsEnabled toggle. `GameEnvironment` instantiates one `StreetLampSchedule` and connects to Sky3D time changes without adding extra clock or per-lamp loops. These controls take effect on fresh scene load; if changing the runtime resource, call `StreetLampSchedule.ApplySettings()` explicitly. **Source implemented; compiler, shader and visuals not tested locally.**
