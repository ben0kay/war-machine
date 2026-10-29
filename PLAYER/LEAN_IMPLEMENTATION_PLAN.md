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

Top-down applies only a waist-level upper-body local Z roll (9°) and sideways shift (0.06m); its camera does not change. FPS applies a 9° camera roll and 0.12m lateral eye offset, naturally rotating its camera-mounted gun/bat. The camera's existing yaw, pitch, shot recoil and head-bob logic remain authoritative; the horizontal movement axis is derived from the forward vector so roll does not steer strafing. No collider, AI, perception, anatomy, damage or weapon-ballistics changes.

**Owning code:** `PLAYER/PlayerController.cs`, `PLAYER/Input/PlayerInput.cs`, `PLAYER/Movement/PlayerMovement.cs`, `PLAYER/Movement/PlayerLean.cs`, `PLAYER/Visuals/PlayerAnimation.cs`, `PLAYER/PlayerSettings.cs`, `PLAYER/FirstPerson/FirstPersonPlayer.cs`, `CAMERA/FirstPerson/FirstPersonCamera.cs`, `WORLD/FirstPerson/Configuration/FirstPersonSettings.cs`, `FirstPersonMaster.tres`.

## Runtime budget and validation

Wake on lean key down/up, focus or input-block transition. Existing physics processing does the transition; once stationary, grounded and settled it sleeps. No new always-active loop, allocation, scene scan or physics query.

**Local checks left to the owner:** `dotnet build war-machine.csproj`. F6 `WORLD/World.tscn` and F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Try each keypad key, both held, release, moving/running/crouching/prone/jumping, weapons slots 1/2/3, firing sustained recoil while leaning, Q shove, debug/backpack UI, Escape capture, Alt-Tab, death and idle physics callback sleep. Confirm FPS gun and sight tilt, top-down model-only tilt, movement direction unchanged, no stuck held state. Test near thin walls for eye clipping.

**Known limitation:** cosmetic lateral eye shift is not collision-checked, so the camera could visually clip close walls and zombie sight will not account for leaning. L1/L2 require separate authorization. The GitHub edit has not been built or played locally; source is **awaiting local validation**.

**Implementation:** one commit on `implement-td0-td1`; see Git history for the actual commit SHA.
