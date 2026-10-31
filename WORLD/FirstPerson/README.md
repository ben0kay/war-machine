# First-person world

Open `WORLD/FirstPerson/FirstPersonWorld.tscn`, build C#, and run the current scene (F6). This is a supported gameplay view inheriting `WORLD/World.tscn`, not a separate world simulation. F5 startup remains unchanged.

## Ownership

- `WORLD/FirstPerson/`: inherited scene, composition and view settings in `Configuration/`.
- `PLAYER/FirstPerson/`: FPS input/movement adapter over shared player physics and stances.
- `CAMERA/FirstPerson/`: eye camera, mouse look, interpolation and head bob.
- `EQUIPMENT/Weapons/FirstPerson/`: FPS aiming, recoil and weapon presentation over shared weapons/damage.
- `ENVIRONMENT/FirstPerson/`: FPS fog/render envelope.
- `VEGETATION/Grass/FirstPerson/`: close-view grass profile using the shared renderer.
- `DEBUG/Overlay/FirstPerson/`: optional tuning UI helpers only.

Top-down currently uses the existing `WORLD/World.tscn`, `CAMERA/Camera.cs` and default player/equipment adapters. These are also shared extension bases; do not rename/move them into top-down-only folders as if they contained no common lifecycle. FPS subclasses explicitly own view differences. Shared health, damage, AI, collisions, inventory, corpse and streaming systems remain authoritative for both views.

## Controls and validation

Arrows move; Shift runs; Left Ctrl/C crouches; Right Ctrl goes prone; RMB jumps; mouse looks; LMB fires; **MMB toggles ADS** for a firearm; Q shoves; R reloads. Escape releases the pointer; click the world to resume. Shared debug panels and inventory retain their current behaviour. Authored FPS tuning is unchanged.

Promotion is source implemented, awaiting local build and World/FPS acceptance. No extra test scenes were added. Build `dotnet build war-machine.csproj`, then F6 this scene: verify capture/focus, movement/stances/jump, head bob, firing/recoil/reload/shove, terrain/content, fog/grass and UI. F6 `WORLD/World.tscn` to check top-down movement/aiming/fading. No terrain rebake. Check idle camera/player callbacks still sleep; no new callbacks or queries were introduced.

[Earlier implementation notes](History.md) retain historical validation/tuning details; ownership statements there are superseded by this guide. The older `PLAYER/LegacyFirstPerson/` experiment remains parked and unchanged.

## L0 held cosmetic leaning

Hold Numpad 1 left / Numpad 2 right; releasing either smoothly levels the FPS view and its camera-mounted gun (including slot-3 bat). A 16° roll plus a 0.20m lateral eye shift is configured in `Configuration/FirstPersonMaster.tres`; shared blend duration, 0.55× movement multiplier and top-down torso pose are in `PLAYER/PlayerSettings.cs`. Top-down only tilts the player model, not its camera. Prone, blocked UI, Escape uncapture and focus loss suppress lean. No physics collision or zombie perception changes. Visual/compile tests pending.

**L0a:** FPS lean is now smoothed at render frequency (extra 0.045s easing) rather than directly assigning the view angle each physics tick. The camera and mounted weapons move together, and a held side offset follows mouse/recoil rotation even when the player is otherwise still. Top-down torso lean increased to 15° / 0.10m; its camera remains unchanged. Build and FPS/top-down checks pending.

**L0b:** latest authored FPS lean defaults are **18° roll / 0.35m sideways eye shift** (was 16° / 0.20m). Updated the camera's 0.25m clamp and Inspector range to 0.50m so the larger offset is effective. Shared/top-down body is **18° / 0.13m**; camera roll remains FPS-only. Same smoothing and 0.55× movement multiplier. More visible corner-peeking, but increased visual clipping risk in tight spaces; no physical exposure yet.
