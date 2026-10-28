# StreetLampMaster — geometry only

`StreetLampMaster.tres` and typed `StreetLampSettings : Resource` now store **only** pole height (5.8m), arm reach (1.1m), metal colour and housing colour. The geometry script reads those values once at `_Ready` and does not write to this master.

**Lighting controls have moved** to [StreetLampLightingMaster.tres](../../../../../../../../ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres). Edit spot/omni energy, ranges, cone, fog, shadows, colour and default enable flag there, not in this world asset configuration. Reinstance/reopen for Inspector changes; there is no automatic runtime settings polling.

### Taller site streetlamp (2026-10-28)

`StreetLampMaster.tres` now defaults to `PoleHeight=7.0`m instead of 5.8m. The pole, collision and head placement are all generated from that same height; `ArmReach` remains 1.1m. All four convenient-store-site instances use this master. Spotlight/fill brightness and coverage are configured separately under `ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres`, not here.
