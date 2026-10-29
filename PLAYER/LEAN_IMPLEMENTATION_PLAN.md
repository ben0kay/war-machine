# L0 — Held lean implementation and handoff

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| L0 | Held keypad lean, smooth cosmetic pose, movement slowdown in both views | Regular Chat | **Implemented in source — awaiting local validation** |
| L1 | Physically reachable cover peek, obstruction-safe camera and upper-body offsets | Codex / ChatGPT Work | Planned; not authorized |
| L2 | Lean-aware perception, limb exposure, shot clearance and injury restrictions | Codex / ChatGPT Work | Planned; not authorized |

## Confirmed baseline

Active branch: `implement-td0-td1`. The shared player owns physics, stance and top-down procedural rig. FPS subclasses it, hides the rig, and mounts weapons directly under its camera. FPS permanent recoil pitch, mouse look, head bob and camera translation already have independent owners.

## L0 implementation

Numpad 1 = held left, Numpad 2 = held right, both = neutral, and key release always returns toward zero. GUI-first key presses and unconditional key releases prevent stuck poses after UI interactions. `PLAYER/Movement/PlayerLean.cs` is a plain nested C# helper with no independent Node/process. Movement's existing physics callback advances the lean over 0.18s until settled; 0.55× speed applies while leaning, even when sprinting/crouched, but not prone. Both modes share these rules.

Top-down applies only a waist-level upper-body local Z roll (15°) and sideways shift (0.10m); its camera does not change. FPS applies a 16° camera roll and 0.20m lateral eye offset, naturally rotating its camera-mounted gun/bat. The camera's existing yaw, pitch, shot recoil and head-bob logic remain authoritative; the horizontal movement axis is derived from the forward vector so roll does not steer strafing. No collider, AI, perception, anatomy, damage or weapon-ballistics changes.

**Owning code:** `PLAYER/PlayerController.cs`, `PLAYER/Input/PlayerInput.cs`, `PLAYER/Movement/PlayerMovement.cs`, `PLAYER/Movement/PlayerLean.cs`, `PLAYER/Visuals/PlayerAnimation.cs`, `PLAYER/PlayerSettings.cs`, `PLAYER/FirstPerson/FirstPersonPlayer.cs`, `CAMERA/FirstPerson/FirstPersonCamera.cs`, `WORLD/FirstPerson/Configuration/FirstPersonSettings.cs`, `FirstPersonMaster.tres`.

## Runtime budget and validation

Wake on lean key down/up, focus or input-block transition. Existing physics processing does the transition; once stationary, grounded and settled it sleeps. No new always-active loop, allocation, scene scan or physics query.

**Local checks left to the owner:** `dotnet build war-machine.csproj`. F6 `WORLD/World.tscn` and F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Try each keypad key, both held, release, moving/running/crouching/prone/jumping, weapons slots 1/2/3, firing sustained recoil while leaning, Q shove, debug/backpack UI, Escape capture, Alt-Tab, death and idle physics callback sleep. Confirm FPS gun and sight tilt, top-down model-only tilt, movement direction unchanged, no stuck held state. Test near thin walls for eye clipping.

**Known limitation:** cosmetic lateral eye shift is not collision-checked, so the camera could visually clip close walls and zombie sight will not account for leaning. L1/L2 require separate authorization. The GitHub edit has not been built or played locally; source is **awaiting local validation**.

**Implementation:** one commit on `implement-td0-td1`; see Git history for the actual commit SHA.

## L0a — stronger and render-smooth FPS lean (2026-10-28)

**Implemented in source; awaiting local validation.** Initial L0 fed smoothed lean to `FirstPersonCamera.SetLeanAmount` during the player's physics tick and assigned camera Z rotation immediately. Even when the FPS eye's position was otherwise interpolated between physics snapshots, the lean roll/offset jumped in discrete physics increments; steady lean also needed a one-frame positional refresh after mouse look.

**Revision:** retain the existing shared physics lean target and 0.18s blend (top-down animation and movement rules unchanged). The FPS camera now stores that lean as a target and smooths both **roll and sideways eye position** inside its existing render `_Process` at an independent, frame-rate-aware 0.045s smoothing time. No additional callback or node is created. The camera wakes for a new lean target and for a mouse-look/recoil change while fully leaned; once translation, bob and visible lean have settled, rendering sleeps again. This preserves mouse yaw, recoil/pitch, weapon parenting, and physics collision/perception.

**New authored defaults:** FPS roll 16° (was 9°), camera shift 0.20m (was 0.12m); top-down torso roll 15° (was 9°), torso shift 0.10m (was 0.06m). Shared movement penalty remains 0.55×. FPS render smoothing 0.045s is newly configurable in `FirstPersonMaster.tres`.

**Validation:** build C#, test Numpad 1 and 2 at idle and 60/120+ FPS, both directions, quick reversals, free mouse look while fully leaned, recoil and sustained shooting while leaning, shift/crouch/prone, key-release and focus/menu return. Check that any visible stutter disappears and camera sleeps after settling. The larger offset may clip nearby geometry; physical peeking remains out of L0. No local engine/build verification performed through the GitHub connection.
