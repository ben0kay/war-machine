# FPS fog and rendering

FirstPersonFog.cs owns FPS-only profile copies, F9 live controls and linked camera Far. It reuses shared PlayerDistanceFog with spherical camera distance; overhead mode is untouched. Master settings are in ../CONFIGURATION/FirstPersonMaster.tres. Default: clear until 25m, opaque at 60m, far plane 72m. End edits update camera culling immediately with >=8m hidden buffer; OFF restores original Far and removes fog draw. Copy reports settings for later defaults.

No process/physics callback, extra per-object loop, scene scan or streaming rebuild. Camera frustum culling limits main-view geometry, not collisions/AI/chunk loading/shadow simulation. Existing rock/tree MultiMesh bounds may straddle the cutoff. Fixed tint and opaque-depth transparent-surface limitations apply. Build/runtime/visual acceptance pending locally; parent README lists brief checks.
