# Current FPS testing controls

**Environment is now shared with top-down**, including ground haze, via the EnvironmentRoot inherited from World.tscn. One left-hand expandable panel replaces F8/F9 windows. Escape releases mouse for controls; click the world to resume. See [current environment controls](ENVIRONMENT/README.md) and [shared scene guide](../../../ENVIRONMENT/SHARED_ENVIRONMENT_README.md). Older F8/F9 panel descriptions below are historical. Compilation/visual acceptance pending locally.

# First-person world test

Open `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`, build C#, then press **F6**. Same branch: `implement-td0-td1`. This is an inherited version of `WORLD/World.tscn`, using the same saved terrain, all three buildings, open-door car, pines/collisions, zombies, robot, environment, equipment and damage. No new terrain bake is needed when the active world already runs. F5 and the active top-down scene are unchanged.

## Controls

arrow-key walk, Shift run, Right Ctrl toggle crouch, **inverted vertical mouse look by default** (mouse up looks down; mouse down looks up), left click/hold fire. Escape releases the pointer; click to capture again (that click does not fire). Focus loss releases the pointer and cancels firing. A centre crosshair marks the camera aim ray; the real muzzle/cover obstruction still decides the shot.

## Ownership

All new FPS-specific implementation lives here:

- `FirstPersonWorld.cs`: only composition differences, no copied world generator or entity logic.
- `CAMERA/`: eye camera, mouse look/capture and event-driven crouch height.
- `PLAYER/`: arrow-key and camera-yaw movement adapter. Reuses shared acceleration, gravity, collision and standing-clearance crouch; hides the third-person mesh.
- `EQUIPMENT/`: centre-screen aiming/trigger adapter. Instances the configured shared firearm under the eye camera; reuses damage, hit masks, cooldown and effects.
- `CONFIGURATION/FirstPersonMaster.tres`: FOV, mouse sensitivity, **InvertMouseY=true** default, pitch limit, eye inset and gun offset. Set `InvertMouseY=false` there to restore standard mouse look.
- `VEGETATION/GrassMaster.tres`: independent FPS grass settings, shared renderer/shaders. 32m radius, narrower blades, four sections and 30Hz GPU simulation; overhead footprint expansion is bypassed.

Three shared files expose small reusable extension hooks: WORLD/WorldController, PLAYER/PlayerController and CAMERA/Camera. Their normal top-down defaults remain the same. Future shared scene changes appear in both views. This is an isolated presentation experiment, not a self-contained copy of the game; a future Git branch can retain these shared dependencies.

## Performance and limits

Camera rotation updates on mouse input; translation renders between completed physics poses while moving/crouching, then sleeps. Player physics wakes on movement/capture/stance/focus events and sleeps after grounded settling. Gun queries run only on requested shots; its existing one-shot cooldown wakes held automatic fire. The camera render callback runs only while translation is settling; no idle aim ray, scene scan or obstruction-fading manager. GPU grass/wind and active enemies/weather keep their existing intentional simulation. Grass remains bounded by its 120k budget; the same batched pines stream around the player.

The top-down tuning panel is omitted. Building camera-occluder proxies may still exist, but no FPS fading manager queries them. This first pass has no visible hands, reload, recoil, ADS, weapon collision animation or full-body first-person rendering. Rifle uses the real world depth buffer and can visually enter nearby scenery, while its obstruction ray prevents shooting through that cover. Zombies retain simple direct pursuit rather than navigation around obstacles.

Validation: resource paths, inherited scene node overrides, input/capture and lifecycle source review, preserved top-down defaults and `git diff --check`. Godot/.NET are unavailable in this environment, so compilation, inherited scene loading, mouse capture, crouch, aiming/cover hits, grass appearance and performance await local validation. Also rerun WORLD/World.tscn briefly to check the shared extension hooks.

## Shared hierarchy update — SO1

The inherited Player override now targets WorldActorsRoot/PersistentEntitiesRoot/Player (index 0). Camera remains at World/Camera (root index 2). Crosshair setup uses the shared DebugUIRoot/Controls accessor. No FPS controls/settings or gameplay changes. Source path/index checks passed; local build/F6 is pending. SO2 now routes runtime objects into the shared scene roots.

## FPS movement/camera refinement

The old camera copied eye position only on physics ticks, which can look stepped above the physics frame rate. FPS camera now linearly interpolates the last two completed physics eye positions during rendering, including smooth capsule-height crouching. Rotation remains immediate in mouse input; no rotation smoothing or exponential follow lag. Camera automatic interpolation is disabled locally to avoid double interpolation, with no project-wide physics changes. Translation has the normal one-physics-tick interpolation delay; a final settled pose snaps exactly into place and stops the render callback. Large teleports reset the pair rather than sweeping through the world.

Captured aiming uses ScreenRelative with the existing configurable InvertMouseY=true, pitch limits and sensitivity. Mouse look alone no longer wakes resting player physics. Shared gun/damage/capture behavior and arrow/Shift/Right Ctrl controls remain. No bob or sway added in this mechanics pass.

Tune CONFIGURATION/FirstPersonMaster.tres in the Inspector:

| Setting | Default | Purpose |
| --- | --- | --- |
| InterpolateTranslation | true | Render between physics poses; false gives raw tick positions for comparison |
| TeleportResetDistance | 8m | Reset interpolation for large position discontinuities |
| Acceleration / Deceleration | 35 / 45 m/s² | Responsive starts and firmer stops |
| DirectionChangeAcceleration | 50 m/s² | Faster reversal while current velocity opposes requested movement |
| WalkSpeed / RunMultiplier / CrouchSpeedMultiplier | 5 / 1.6 / 0.45 | FPS-only movement speeds |
| FloorSnapDistance | 0.4m | Keep contact over small downward terrain changes |
| MaximumSlopeDegrees | 46° | Walkable slope limit |
| CollisionSafeMargin | 0.01m | Small collision recovery margin |
| MouseSensitivity / InvertMouseY / MaximumPitch | 0.12 / true / 85° | Immediate mouse look |

FPS enables constant ground speed with floor snapping and stops sliding on slopes. It retains the shared capsule/standing-clearance behavior and collision masks; no automatic step climbing or vaulting. Safe margin/snap defaults are starting points requiring local terrain testing, not a guaranteed cure for every mesh edge.

All FPS overrides stay here. Shared PlayerController gained only one virtual horizontal-velocity method whose default is the exact previous acceleration formula; top-down camera, masters, floor settings and movement defaults are unchanged. Player settings are duplicated before applying FPS speed overrides. Initialization uses the existing inherited scene paths from SO1.

Validation: source/resource/Inspector binding checks, interpolation endpoint/idle lifecycle review, preserved inverted direction and shared default formula, and git diff --check. Godot/.NET unavailable, so build, inherited scene loading and runtime feel remain unverified. Locally run `dotnet build war-machine.csproj`, then F6 FirstPersonTest: strafe while turning, reverse/stop/run, crouch while stationary/moving, traverse the hill and doorway/trunks, shoot near cover, release/capture and alt-tab. Compare InterpolateTranslation OFF/ON; inspect camera processing settling at idle. Briefly F6 WORLD/World.tscn to confirm top-down feel remains unchanged. No rebake needed.

Implementation references: Godot's advanced physics interpolation camera guidance and InputEventMouseMotion ScreenRelative documentation. No measured FPS or visual-quality claim.

SO2 preserves this inherited camera index and FPS input/aiming. Environment/vegetation/zombies/props now use shared organizational roots. The FPS composition hook now supplies its own camera-distance fog instead of the top-down ring. Local inherited-scene and shutdown validation remains pending.

The shared world rock/cliff addition also appears here: collidable boulders and a ramp/plateau south of spawn, with building/path exclusions. FPS-specific movement, aiming and fog behavior are unchanged. Local collision/terrain-join checks remain pending.

The shared cliff test now uses real Terrain3D heightmaps rather than a separate mesh ramp/plateau. Both views share the same south mountain and native collision; FPS camera/input remain unchanged. No rebake needed; local slope/edge tests remain pending.

Top-down lodge floor presentation is installed only in World's InitializeOcclusion hook, which FPS overrides with an empty method. FPS retains normal opaque floors/walls/roofs; station section proxies have no active fading consumer in FPS. No interior content sleeping/culling added. Local inherited scene check pending.

## Linked FPS distance fog and rendering

F6 FirstPersonTest.tscn. **F9** opens/closes mouse-friendly fog controls: enabled, start/end metres, hidden render buffer and colour. Changes apply immediately. The readout shows effective distances (end is always at least start+1m); Copy fog settings sends the effective values to clipboard for choosing later defaults. Closing restores mouse capture if it was captured before opening. UI-consumed clicks do not shoot. Arrows/Shift/Right Ctrl and inverted mouse look remain unchanged.

FirstPersonMaster owns DistanceFogEnabled=true, FogStartDistance=1, FogEndDistance=100, FogCullBuffer=120 and FogColor=Color(0.39, 0.46, 0.48, 1). The FPS-only FirstPersonFog controller copies this profile and EnvironmentConfig; no shared environment/master edits. Fog reaches full opacity in all directions at 100m, and camera Far becomes 220m. The FPS F9 cull-buffer slider now supports 8–200m, and its larger 120m default means additional geometry may still render unseen behind the opaque fog. Changing end automatically changes Far; the >=8m buffer hides the clip plane. Disabling restores the previous Far (currently 1800m) and hides the overlay. Start controls the gradient, end controls the visibility/culling horizon. Re-enable reapplies the current settings.

Actual main-view frustum culling now limits distant trees, rocks, buildings, terrain and actors; large/chunk bounds intersecting the far plane may still be submitted/clipped. Existing close-range grass and pine LOD remain. No per-object callback, streaming-radius rebuild, collision removal, AI sleeping or shadow-distance changes. Fog itself costs one fullscreen depth-overlay pass while enabled; this is not a measured FPS improvement. No volumetric simulation or per-frame C# fog callbacks. Runtime resources and camera-local overlay are released with the scene; camera far restores on controller exit.

Shared fog gained only an optional camera-distance mode, default false. Top-down ring/configuration/fading remain unchanged, and FPS still skips building/occlusion fades. Static tint and opaque-depth limitations (glass/effects) carry over; night colour can be tuned independently in F9.

Validation here: source/scene wiring, distance clamps/off restoration, no-loop lifecycle, numeric spherical-fog/clip ordering and whitespace review. No Godot/.NET installed: build and shader/runtime validation pending locally. Briefly test F9 sliders, toggle OFF/ON, copy, looking up/down/turning/zoom FOV, fog edge while moving past buildings/pines and night, plus one top-down load. No rebake needed.

Shared test equipment/population: **1 = Rifle_001, 2 = MachineGun_001**; rifle starts selected, switching cancels held fire and reuses cached gun scenes. Press/click again to shoot after switching. Both scenes now have 12 existing slow zombies plus 3 Zombie_002 runners, with independent WorldMaster.RunnerSpawnOffsets at (18,22), (-15,30), (32,-18) from spawn. Runners wander slowly and sprint at their authored 8.3m/s only when sight confirms the player; no attacks/hearing added. F7 shows vision; tree/rock spawn clearings include runners. Local build/weapon placement/shooting/runner checks pending. Existing FPS fog profile and other recent commits preserved.

## F8 — FPS weather and time tuning

The FPS test now has a **F8 weather/time** debug panel separate from **F9 fog/render distance**. The F8 scroll panel can set the hour, pause/unpause time, day length, weather enabled, rain, snow, local mist, airborne motes, wind, gusts, turbulence, particle size/fall speed and quality. "Copy weather & time settings" exports runtime values to the clipboard; none are written to authoring resources. This reuses the exact shared Sky3D/weather/vegetation owners used by the top-down world.

F8/F9 are mutually exclusive. Opening releases mouse capture, swapping panels keeps it released, closing restores capture only if it was captured beforehand. Escape closes the panel while leaving the mouse released. UI clicks cannot trigger camera recapture/weapon fire. Everything is input/signal-driven; no new update loop. See [FPS environment README](ENVIRONMENT/README.md). **Local validation pending:** build, F6 FPS, test day/night and weather toggles, swap F8/F9, escape/focus/input, and briefly load the overhead world.
