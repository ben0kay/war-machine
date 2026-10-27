# CONFIGURATION

`FirstPersonMaster.tres` uses FirstPersonSettings.cs for FOV, sensitivity, pitch, eye inset and weapon placement. These are read-only test defaults; shared masters are not edited.

FirstPersonMaster now also exposes FPS movement speeds, independent acceleration/braking/reversal, ground snapping/slope/margin and translation interpolation/teleport reset. See parent README for default values and validation.

Fog/render group: DistanceFogEnabled, FogStartDistance, FogEndDistance, FogCullBuffer and FogColor. F9 tests copies live in FPS; copy button reports effective settings. Fully opaque distance drives camera Far plus buffer, and OFF restores the original Far. See ../ENVIRONMENT/README.md.
