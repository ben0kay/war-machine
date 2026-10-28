# Shared Environment scene

`ENVIRONMENT/Environment.tscn` is the reusable atmosphere prefab for **both** `WORLD/World.tscn` and its inherited `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`.

- Root `GameEnvironment.cs`: one runtime copy of Settings, startup composition and explicit view binding.
- `Sky`: the existing WildernessSky scene and sole WorldEnvironment. Sky3D still owns sky/light/time.
- `Weather`: existing WeatherBridge; exported EffectsScene references the existing TestWeather scene.
- `GroundHaze`: existing GroundHaze; exported WispsShader references the shared fog shader.
- `Environment` SkyController child is created once after Sky is ready, retaining the existing path used by tuning tools.

Edit this scene or its referenced scenes/shader once to affect both views. Edit **Configuration/GameplayEnvironmentMaster.tres** for shared saved defaults; root Settings references it. Live sliders modify an instance copy and deliberately do not save defaults or change another running instance. WorldController no longer owns a separate EnvironmentSettings export: it receives the prefab's runtime copy. For a new world, instance this prefab once, then call InitializeView(terrain, player, camera) after safe placement. It intentionally does not scan for camera/player/terrain or initialize weather without them.

The first-person test inherits the same EnvironmentRoot instance from World and no longer opts out of haze. Do not add a second environment/weather scene to that inherited test. FPS retains its own camera-distance fog/render-cutoff profile; top-down retains its player-centred distance overlay. These are view adapters, not duplicated weather. Older standalone wilderness initialization APIs remain available and are not migrated by this job.

## Debug controls

Both views have expandable left-hand categories. FPS now uses one visible panel with **Environment** (Day/night, Weather, Wind, Ground haze, Distance fog) and **Rendering/culling**. No F8/F9 panel shortcuts. Press Escape to release the cursor, click categories/sliders, then click the world to recapture mouse look. UI clicks are refused by camera recapture and existing weapon input. GroundHazeControls builds the same haze widgets in both views. Foliage simulation/settings are not moved into this prefab.

## Performance / validation

No new Process/PhysicsProcess loop, timer, polling, scene search or per-entity work. The root binds references once; existing weather zero/off sleep and cached GPU-only haze animation/movement thresholds remain. OFF/zero hides haze/restores its volumetric request; clear weather disables emitters. Unload releases camera/haze subscriptions. Active precipitation retains its existing addon camera-follow callbacks and visible volumetric fog retains GPU cost.

Source/loop/scene wiring and resource references reviewed. **Compilation, shader compilation and runtime checks remain unverified: no Godot/.NET executable here.** Locally build `dotnet build war-machine.csproj`, then F6 World and FirstPersonTest in turn. Confirm one EnvironmentRoot/Sky/Weather/GroundHaze in Remote tree; test rain/snow, time, shared wind, haze on/zero/off/reactivation, movement across slopes and scene reload. In FPS, check Escape/UI clicks/world recapture, one panel/no F8/F9, all categories/copy buttons and distance-fog cutoff. Ground haze still approximates terrain on a 4m grid and does not exclude building interiors; inspect close FPS wisps and GPU/froxel trails locally. No terrain rebake required.

## Day / night lighting and rays — awaiting local validation

Both active views use the same solar lighting. SkyController reads Sky3D's actual sun elevation after the existing time signal finishes updating celestial transforms. It blends shared ambient colour/energy and publishes a runtime daylight factor. Distance fog blends NightFogColor → DuskFogColor → each view's own daytime fog colour. FPS explicitly receives the shared runtime atmosphere, retaining its independent distances, colour profile and far-plane link.

FPS now reveals the existing Sky3D moon/stars/clouds above the horizon: FpsSkyFogOpacity controls the residual sky tint (0 clear, 1 obscured). The horizon stays opaque to meet distant fogged terrain. No new skybox or WorldEnvironment. Moon visibility depends on Sky3D's moon position/date/cloud cover; this does not force a full moon overhead at every nighttime hour.

Defaults live in Configuration/GameplayEnvironmentMaster.tres. Both left overlays expose **Environment → Lighting / atmosphere**: responsive fog toggle; night/dusk fog colours; day/night ambient colours and energy; moonlight; FPS sky opacity; rays toggle, density/range, sun/moon scattering strength and anisotropy. Copy settings includes these values. Edits are runtime-only; transfer chosen values into the master to save them.

Light rays use a thin global volumetric baseline in the existing Forward+ pass and the existing shadow-casting sun/moon. GroundHaze coordinates its cached baseline with the ray controls: switching rays off/zero removes global density while retaining enabled local wisps; disabling haze retains enabled rays; both off disable the pass. Its overhead distance reserve can make the effective range longer than the ray range slider. No new FogVolumes, lights, entity scans, CPU frame loops or resolution increases. Only Sky3D's existing clock signals and explicit UI edits update atmosphere; pausing solar time stops clock-driven changes, while GPU haze/cloud animation continues. Muzzle light scattering is zero to avoid temporal reprojection trails.

Rays require shadow-casting geometry between light and camera/fog; expect subtle shafts between trees and around roof edges, not beams at every angle. The late distance overlay progressively conceals far shafts. The volumetric pass adds GPU cost even at low density; toggle rays and haze separately on the target PC. Thin foliage may not cast fine volumetric shadows at the existing resolution. No custom shadow shader, cloud-shadow projection, interior fog exclusion or automatic exposure was added.

Brief checks: source/resource/API/loop/lifetime review only; no Godot/.NET executable available, so compilation, shader compilation and visual/performance acceptance remain unverified. Locally run `dotnet build war-machine.csproj`, then F6 WORLD/World.tscn and DEBUG/TEST/FirstPerson/FirstPersonTest.tscn. Scrub time through noon, sunset, midnight and dawn while paused and running; confirm darker night fog, readable silhouettes, smooth transitions and a visible FPS sky when looking upward. Test rays on/off/zero, haze on/off/zero in all combinations, reactivation, scene reload and changed fog distances. Inspect shafts through trees/buildings at several sun/moon angles, FPS horizon seams, moon/cloud visibility, temporal trails and GPU frame time. No terrain rebake.

## Planned interior exclusions

See [INTERIOR_EXCLUSION_IMPLEMENTATION_PLAN.md](INTERIOR_EXCLUSION_IMPLEMENTATION_PLAN.md). Proposed IE0–IE4 register interior/shelter data independently of camera fading, exclude local ground haze spatially in both views, and hide rain only in first person. Top-down rain remains visible in either projection; global distance fog is preserved. Snow/addon mist/motes retain their existing behavior until separately opted in. Plan only; no environment behavior changed.
