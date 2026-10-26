# TestWorld weather integration

Project-owned C# WeatherBridge instantiates the new weather_atmosphere addon's precipitation and atmosphere components through TestWeather.tscn. TestWeatherController.gd is a small subclass exposing a combined rain/snow preview state. Addon sources are unchanged. Sky3D keeps exclusive ownership of time, sunlight and fog (manage_environment=false; no addon sun reference).

EnvironmentMaster now owns enabled, independent rain/snow intensity, local mist, drifting motes, gusts, turbulence, particle size/fall speed and quality (0 Low / 1 Medium / 2 High). F9 applies changes immediately. Existing shared wind strength/speed and the new wind heading also feed precipitation and all plant materials. Rain and snow can coexist. Settings reset with F9 resets and runtime resource copies protect source masters. Clear weather is the default.

The addon is a useful prototype: layered GPU precipitation, quality budgets, mist, drifting particles, weather profiles/transitions, zones and optional lightning/audio. Integration limitations found in review: default particles anchor around y=0, idle component callbacks continue, splash placement assumes a flat ground plane, and its environment/sun ownership can compete with Sky3D. This bridge uses camera-height anchoring, disables inactive component/emitter processing and hides inactive emitters. Active weather still intentionally runs the addon's camera-follow/wind callbacks; no claim of zero CPU work while raining. Zero weather disables the entire effect subtree directly; reactivation wakes it explicitly. Large allocations remain bounded by the addon's quality tables.

Cave shelter is checked on explorer movement, with state reapplied only on shelter changes. Local rain/snow/mist/motes are suppressed within the cave. This is a local tunnel test, not a general roof/occlusion solution. Rain/snow outside do not collide individually with arbitrary roofs or terrain. Splash, lightning and audio components are not instantiated yet; snow accumulation and automatic storm/cloud/ground-wetness changes are not included. These need a separate, deliberate integration rather than blindly enabling every demo component.

Initial source/reference checks only; Godot/.NET are unavailable here. Test rain/snow separately and together, wind heading/strength, quality, zero/reactivation, cave shelter and FPS locally. No compiled/runtime/visual acceptance claimed.

## Ground haze (separate from distance fog)

F9 has a Ground haze (Forward+) category: enable, density and layer height. It is OFF by default; try density 0.06 and height 2.5m. This uses native Godot FogVolumes on Sky3D's existing Environment, alongside its untouched distance-fog shader. Volumetric base density is zero: only local terrain-anchored volumes contribute. The prior volumetric enabled/density settings restore when disabling haze. Other Sky3D time/sky/sun/fog settings remain owned by Sky3D.

Nine small box volumes follow nearby terrain in 24m tiles, sampling heights only when crossing a tile boundary. Camera altitude does not raise the layer. Shelter transitions hide the volumes inside the cave. No per-frame C# haze callback; off frees all volumes. This adds native volumetric GPU cost and requires Forward+. Height anchoring is approximate on steep slopes, not a full terrain-conforming fog simulation. Tiles may show height transitions on rough ground. It is distinct from the addon's camera-following Local mist particles, which remain separately adjustable.

Source/resource checks only; Godot/.NET unavailable here, so compilation, shader/renderer compatibility, cave join appearance and haze/FPS checks remain local.
