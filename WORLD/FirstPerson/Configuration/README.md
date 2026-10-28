> **Current ownership:** Production FPS adapter/profile in `WORLD/FirstPerson/Configuration`. [View composition and settings](../README.md). Older test-folder statements below are historical; current paths and behaviour are defined by the promoted source.

# CONFIGURATION

`FirstPersonMaster.tres` uses FirstPersonSettings.cs for FOV, sensitivity, pitch, eye inset and weapon placement. These are read-only test defaults; shared masters are not edited.

FirstPersonMaster now also exposes FPS movement speeds, independent acceleration/braking/reversal, ground snapping/slope/margin and translation interpolation/teleport reset. See parent README for default values and validation.

Fog/render group: DistanceFogEnabled, FogStartDistance, FogEndDistance, FogCullBuffer and FogColor. F9 tests copies live in FPS; copy button reports effective settings. Defaults are enabled, start 1m, fully opaque at 100m, cull buffer 120m and tint Color(0.39, 0.46, 0.48, 1). The FPS buffer allows 8–200m. Fully opaque distance drives camera Far plus buffer (220m by default), and OFF restores the original Far. See ../ENVIRONMENT/README.md.

## FPS mouse-look sensitivity — 2026-10-28

Default `MouseSensitivity` lowered from **0.12 to 0.10** (~16.7% less rotation per mouse pixel). `FirstPersonCamera.cs` still reads this single setting for both horizontal and vertical mouse look; inversion, recoil, FOV and top-down camera settings are unchanged. Tune `FirstPersonMaster.tres` to taste. No code-path or input behaviour changes were needed.


## Gentler walking head bob (2026-10-28)

FPS bob remains linked to **actual post-collision ground distance**, not a footstep animation. To reduce overly rapid and strong bouncing, current `FirstPersonMaster.tres` defaults are walk **0.012m vertical / 0.006m sideways**, run **0.018m vertical / 0.010m sideways**, and bob-cycle travel **2.8m walk / 3.3m run**. A matching waveform change in `CAMERA/FirstPerson/FirstPersonHeadBob.cs` reduces the vertical oscillation count from two to one per phase revolution and lateral sway to half that rate. Roughly 1.8Hz walking and 2.4Hz running at the authored movement speeds. `HeadBobReturnSeconds=0.18` and `HeadBobEnabled` are unchanged. No player speed, mouse look, recoil or top-down settings were altered.
