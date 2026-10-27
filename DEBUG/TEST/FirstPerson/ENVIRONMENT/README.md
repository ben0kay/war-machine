# FPS fog and rendering

FirstPersonFog.cs owns FPS-only profile copies, F9 live controls and linked camera Far. It reuses shared PlayerDistanceFog with spherical camera distance; overhead mode is untouched. Master settings are in ../CONFIGURATION/FirstPersonMaster.tres. Default: fog starts at 1m, becomes fully opaque at 100m, and the far plane is 220m (100m fog horizon + 120m buffer). The FPS buffer slider supports 8–200m; this intentionally generous default may render more hidden geometry than a smaller buffer. End edits update camera culling immediately with >=8m hidden buffer; OFF restores original Far and removes fog draw. Copy reports settings for later defaults.

No process/physics callback, extra per-object loop, scene scan or streaming rebuild. Camera frustum culling limits main-view geometry, not collisions/AI/chunk loading/shadow simulation. Existing rock/tree MultiMesh bounds may straddle the cutoff. Fixed tint and opaque-depth transparent-surface limitations apply. Build/runtime/visual acceptance pending locally; parent README lists brief checks.

## FPS weather and time controls (F8)

`FirstPersonWeatherPanel.cs` adds a scrollable F8 weather/clock panel alongside the existing **F9** FPS camera-distance fog panel. It edits the shared WorldController's already-duplicated runtime `EnvironmentConfig` through the existing `SkyController`, `WeatherBridge`, `TerrainGrass` and `PinePlacement`—no new environmental simulation or saved-master changes. It includes time-of-day, pause, day length, weather enabled, rain, snow, local mist, airborne motes, wind/heading, gusts, turbulence, particle size/speed and quality, plus copy-to-clipboard JSON. Sky3D's `time_changed` signal updates the time readout while the clock runs; cleanup disconnects it. The panel is event-driven with no idle processing.

FirstPersonWorld makes F8 and F9 mutually exclusive and maintains mouse-capture state when switching; the FPS camera does not click-to-recapture while either panel is open. Escape closes the panel without recapturing; focus loss cancels recapture. The top-down tuning panel is unchanged.

**Awaiting local validation:** build C# and F6 FPS. Change time from day to night and back; pause/unpause; test rain/snow/mist on/off and wind; copy settings; swap F8/F9 and check no overlap/click shooting; close/escape/focus loss; briefly open the top-down world. No terrain rebake needed.
