# Ground haze 2.0 and weather

## Ground haze 2.0 — implemented, awaiting local validation

The active top-down `WORLD/World.tscn` now reuses `GroundHaze.cs` and `Shaders/GroundWisps.gdshader`. WorldController passes the current player, Terrain3D data, camera and **the existing Sky3D Environment**. No second WorldEnvironment, weather controller, gameplay/physics change or addon edit. The legacy Initialize(explorer, cave) contract remains, including its cave and >35m flight suppression. FPS comparison deliberately skips the new top-down haze integration and keeps its own controls.

One cached local FogVolume replaces overlapping boxes, eliminating internal tile seams. A small RG float texture stores terrain height and validity on a fixed 4m world grid. The shader interpolates that surface, suppresses missing regions, tapers density above the ground, and combines a foot-level veil with broad/detail noise and gently warped drift. Noise uses WORLD_POSITION and GPU TIME, so scenery does not slide with the camera. Coverage feathers from 60% of the radius to zero. Coverage follows movement in 1.5m increments; the world-space pattern remains fixed except intentional wind drift. No per-volume boundaries or CPU noise animation.

### Active defaults and live controls

`ENVIRONMENT/Configuration/GameplayEnvironmentMaster.tres`:
- GroundHazeEnabled = true
- GroundHazeDensity = 0.22
- GroundHazeHeight = 1.2 metres (wisps can extend to ~1.45× this)
- GroundHazeNoiseScale = 0.25 (smaller = larger features)
- GroundHazeDriftSpeed = 0.2 m/s
- GroundHazeContrast = 0.85
- GroundHazeTurbulence = 0.15
- GroundHazeCoverageRadius = 30 metres, clamped 6–48
- GroundHazeColor = (0.64, 0.71, 0.73)
- WindDirection remains the shared environment wind; Environment → Weather and shared wind exposes its heading.

All haze settings appear under **Environment → Ground haze** in the top-down tuning panel. Coverage applies on slider release; other controls apply immediately. Copy settings exports haze and shared wind too. Live edits affect runtime copies only; save chosen values in the master for persistence. Legacy master values are retained, with a default 9m radius when no override exists.

### Performance and ownership

No Process/PhysicsProcess callback, timer, actor query, zombie dependency or scene scan. At rest, only the intentional GPU fog pass/noise runs. Movement crossing an 8m tile refreshes the cached height image; retained overlap samples are reused. Default radius uses 21×21 = 441 initial samples, then 42 new samples for a single-axis 8m crossing (80 diagonal); max radius uses 29×29 = 841 initial samples, then 58 single-axis/112 diagonal. Large teleports sample the bounded window again. One existing image/texture is updated, not recreated, on movement; dictionaries reuse storage after warm-up. Only coverage size changes resize the texture.

OFF or density zero hides the volume, skips movement sampling/material work and restores prior volumetric state; cached resources stay for reactivation. Legacy shelter/altitude suppression does likewise. Unload/reinitialization disconnect subscriptions and restore owned state. Haze temporarily enables the same Environment volumetric pass; it keeps any already-enabled baseline density, uses zero global density only if previously disabled, and restores enable/density/length on release. The overhead froxel range reserves up to the supported 100m camera distance plus radius/padding, so zooming out at rest still sees haze. No volumetric quality/global render settings changed.

### Brief validation and limitations

Source, C# loop syntax, resource/uniform wiring, coverage/sample math and legacy/FPS call sites reviewed. **Compilation and shader compilation not verified: Godot/.NET unavailable here.** No visual/performance acceptance or measured FPS improvement claimed.

Locally:
1. `dotnet build war-machine.csproj`.
2. Open `WORLD/World.tscn`, F6. Expand Environment → Ground haze; check drifting patches around feet and between trees.
3. Walk/run across tile boundaries and gentle slopes, stop, zoom/tilt/switch projection. Look for popping, seams, floating patches or disappearing coverage.
4. Try zero, OFF, re-enable, colour, wind heading, drift, height and coverage. Ensure rain/snow, Sky3D and distance fog still work.
5. Check the older wilderness cave/flight/F9 flow, FPS comparison, scene reload and profiler with horde/vegetation enabled.

4m height samples approximate gentle slopes; sharp cliffs, thin holes and cave/overhang geometry are not represented exactly. Haze currently can appear inside buildings; no generic roof/room exclusion was added. The one volume's vertical bounds encompass local height relief, which increases GPU work near mountains. Native froxel resolution/temporal reprojection can blur fine wisps or leave trails; inspect on target D3D12 hardware before acceptance. High density/height can obscure characters; tune down as needed. The local coverage edge may become visible when zoomed beyond its radius; widen within the cap or use the existing distance fog. Future independent volumetric owners should coordinate acquisition/release rather than concurrently edit haze-owned fields.

## Existing weather integration (unchanged)

# TestWorld weather integration

Project-owned C# WeatherBridge instantiates the new weather_atmosphere addon's precipitation and atmosphere components through TestWeather.tscn. TestWeatherController.gd is a small subclass exposing a combined rain/snow preview state. Addon sources are unchanged. Sky3D keeps exclusive ownership of time, sunlight and fog (manage_environment=false; no addon sun reference).

EnvironmentMaster now owns enabled, independent rain/snow intensity, local mist, drifting motes, gusts, turbulence, particle size/fall speed and quality (0 Low / 1 Medium / 2 High). F9 applies changes immediately. Existing shared wind strength/speed and the new wind heading also feed precipitation and all plant materials. Rain and snow can coexist. Settings reset with F9 resets and runtime resource copies protect source masters. Clear weather is the default.

The addon is a useful prototype: layered GPU precipitation, quality budgets, mist, drifting particles, weather profiles/transitions, zones and optional lightning/audio. Integration limitations found in review: default particles anchor around y=0, idle component callbacks continue, splash placement assumes a flat ground plane, and its environment/sun ownership can compete with Sky3D. This bridge uses camera-height anchoring, disables inactive component/emitter processing and hides inactive emitters. Active weather still intentionally runs the addon's camera-follow/wind callbacks; no claim of zero CPU work while raining. Zero weather disables the entire effect subtree directly; reactivation wakes it explicitly. Large allocations remain bounded by the addon's quality tables.

Cave shelter is checked on explorer movement, with state reapplied only on shelter changes. Local rain/snow/mist/motes are suppressed within the cave. This is a local tunnel test, not a general roof/occlusion solution. Rain/snow outside do not collide individually with arbitrary roofs or terrain. Splash, lightning and audio components are not instantiated yet; snow accumulation and automatic storm/cloud/ground-wetness changes are not included. These need a separate, deliberate integration rather than blindly enabling every demo component.

Initial source/reference checks only; Godot/.NET are unavailable here. Test rain/snow separately and together, wind heading/strength, quality, zero/reactivation, cave shelter and FPS locally. No compiled/runtime/visual acceptance claimed.

## Orthographic preview reuse

WeatherBridge also accepts an explicit Camera3D and Node3D anchor, without an ExplorationController or cave. TopDownWorldTest uses its player as anchor and SetOrthographicCoverage on zoom/viewport resize to widen the existing rain/snow emitters and culling bounds. Existing FPS initialization still anchors at its camera and retains movement-triggered cave shelter. Particle quality budgets and dormant-zero behaviour are shared; addon sources are unchanged.
