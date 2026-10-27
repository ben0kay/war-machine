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

SO2 preserves this inherited camera index and FPS input/aiming. Environment/vegetation/zombies/props now use shared organizational roots. The new top-down PlayerDistanceFog is explicitly omitted by the FPS composition hook. Local inherited-scene and shutdown validation remains pending.

The shared world rock/cliff addition also appears here: collidable boulders and a ramp/plateau south of spawn, with building/path exclusions. FPS-specific movement, aiming and fog behavior are unchanged. Local collision/terrain-join checks remain pending.

The shared cliff test now uses real Terrain3D heightmaps rather than a separate mesh ramp/plateau. Both views share the same south mountain and native collision; FPS camera/input remain unchanged. No rebake needed; local slope/edge tests remain pending.

Top-down lodge floor presentation is installed only in World's InitializeOcclusion hook, which FPS overrides with an empty method. FPS retains normal opaque floors/walls/roofs; station section proxies have no active fading consumer in FPS. No interior content sleeping/culling added. Local inherited scene check pending.
