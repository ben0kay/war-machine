# First-person world test

Open `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`, build C#, then press **F6**. Same branch: `implement-td0-td1`. This is an inherited version of `WORLD/World.tscn`, using the same saved terrain, depot, pines/collisions, zombies, robot, environment, equipment and damage. No new terrain bake is needed when the active world already runs. F5 and the active top-down scene are unchanged.

## Controls

WASD walk, Shift run, Right Ctrl toggle crouch, mouse look, left click/hold fire. Escape releases the pointer; click to capture again (that click does not fire). Focus loss releases the pointer and cancels firing. A centre crosshair marks the camera aim ray; the real muzzle/cover obstruction still decides the shot.

## Ownership

All new FPS-specific implementation lives here:

- `FirstPersonWorld.cs`: only composition differences, no copied world generator or entity logic.
- `CAMERA/`: eye camera, mouse look/capture and event-driven crouch height.
- `PLAYER/`: WASD and camera-yaw movement adapter. Reuses shared acceleration, gravity, collision and standing-clearance crouch; hides the third-person mesh.
- `EQUIPMENT/`: centre-screen aiming/trigger adapter. Instances the configured shared firearm under the eye camera; reuses damage, hit masks, cooldown and effects.
- `CONFIGURATION/FirstPersonMaster.tres`: FOV, mouse sensitivity, pitch limit, eye inset and gun offset.
- `VEGETATION/GrassMaster.tres`: independent FPS grass settings, shared renderer/shaders. 32m radius, narrower blades, four sections and 30Hz GPU simulation; overhead footprint expansion is bypassed.

Three shared files expose small reusable extension hooks: WORLD/WorldController, PLAYER/PlayerController and CAMERA/Camera. Their normal top-down defaults remain the same. Future shared scene changes appear in both views. This is an isolated presentation experiment, not a self-contained copy of the game; a future Git branch can retain these shared dependencies.

## Performance and limits

Camera updates only on mouse input, actor movement or crouch interpolation. Player physics wakes on movement/look/capture/stance/focus events and sleeps after grounded settling. Gun queries run only on requested shots; its existing one-shot cooldown wakes held automatic fire. No FPS frame loop, idle aim ray, scene scan or obstruction-fading manager. GPU grass/wind and active enemies/weather keep their existing intentional simulation. Grass remains bounded by its 120k budget; the same batched pines stream around the player.

The top-down tuning panel is omitted. Building camera-occluder proxies may still exist, but no FPS fading manager queries them. This first pass has no visible hands, reload, recoil, ADS, weapon collision animation or full-body first-person rendering. Rifle uses the real world depth buffer and can visually enter nearby scenery, while its obstruction ray prevents shooting through that cover. Zombies retain simple direct pursuit rather than navigation around obstacles.

Validation: resource paths, inherited scene node overrides, input/capture and lifecycle source review, preserved top-down defaults and `git diff --check`. Godot/.NET are unavailable in this environment, so compilation, inherited scene loading, mouse capture, crouch, aiming/cover hits, grass appearance and performance await local validation. Also rerun WORLD/World.tscn briefly to check the shared extension hooks.
