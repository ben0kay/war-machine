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
