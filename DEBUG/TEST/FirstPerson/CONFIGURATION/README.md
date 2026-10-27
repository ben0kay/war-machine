# CONFIGURATION

`FirstPersonMaster.tres` uses FirstPersonSettings.cs for FOV, sensitivity, pitch, eye inset and weapon placement. These are read-only test defaults; shared masters are not edited.

FirstPersonMaster now also exposes FPS movement speeds, independent acceleration/braking/reversal, ground snapping/slope/margin and translation interpolation/teleport reset. See parent README for default values and validation.

Fog/render group: DistanceFogEnabled, FogStartDistance, FogEndDistance, FogCullBuffer and FogColor. F9 tests copies live in FPS; copy button reports effective settings. Defaults are enabled, start 1m, fully opaque at 100m, cull buffer 120m and tint Color(0.39, 0.46, 0.48, 1). The FPS buffer allows 8–200m. Fully opaque distance drives camera Far plus buffer (220m by default), and OFF restores the original Far. See ../ENVIRONMENT/README.md.
