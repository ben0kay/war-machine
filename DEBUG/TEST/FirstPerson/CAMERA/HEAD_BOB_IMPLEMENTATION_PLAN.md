# FPS-HB0 — Lightweight first-person walking head bob

> **Status: Planned only; no head-bob gameplay code implemented.** This is an FPS-only presentation feature for `DEBUG/TEST/FirstPerson/`, independent of the top-down camera and player physics. Do **not** implement until expressly authorized. Work on `implement-td0-td1` and follow root `CodePreferences.md`.

## Phase

| Phase | Goal | Suggested implementer | Status |
| --- | --- | --- | --- |
| **FPS-HB0** | Subtle positional camera sway when actually walking/running, returning gently to neutral on stopping | Codex / Work (integration); Regular Chat for values; Copilot for local visual tests | **Planned** |

## Current code confirmed

- `CAMERA/FirstPersonCamera.cs` owns mouse-look yaw/pitch, permanent upward recoil through `ApplyWeaponRecoilKick`, and **render-interpolated eye position**. It explicitly sleeps its `_Process` callback once eye translation settles. It currently has **no head bob**.
- `PLAYER/FirstPersonPlayer.cs` calls `_view.RecordPhysicsPose(CapsuleHeight)` after `base._PhysicsProcess(delta)`. The shared `PlayerController` owns gravity, collisions, running, crouch, prone and input blocking. Read its current modular movement API before integrating.
- `EQUIPMENT/FirstPersonWeapon.cs` aims via the FPS camera's centre-screen ray; weapon visuals are attached under the eye camera and real camera pitch is affected by recoil. **Do not drive aiming by a second camera orientation or turn bob into aim recoil**.
- Defaults live in `CONFIGURATION/FirstPersonSettings.cs` / `FirstPersonMaster.tres`; mouse sensitivity just changed **0.12 → 0.10**, separately from any future bob.

## Proposed small folder structure

```text
DEBUG/TEST/FirstPerson/
  CAMERA/
    FirstPersonCamera.cs                EXISTING — input, eye interpolation, bob composition
    FirstPersonHeadBob.cs               NEW — one plain C# state/helper, NO scene node
    HEAD_BOB_IMPLEMENTATION_PLAN.md     THIS DOCUMENT
    README.md
  PLAYER/
    FirstPersonPlayer.cs                EXISTING — supplies physics-step travel + stance/ground
  CONFIGURATION/
    FirstPersonSettings.cs              EXISTING — grouped Bob exports
    FirstPersonMaster.tres              EXISTING — authored defaults
```

**Responsibilities:** `FirstPersonHeadBob` receives physics-step horizontal distance/travel, grounded state, stance and speed; keeps one phase and current/target amplitudes, and computes a local **camera-position** offset (`X` and `Y` only). `FirstPersonCamera` composes that offset **after** calculating/interpolating its ordinary eye height, without changing mouse-look or recoil rotations. `FirstPersonPlayer` supplies small movement facts at the existing post-physics hook, not a new FPS movement subsystem.

## Proposed effect and tuning

- Small side-to-side step sway and twice-per-stride vertical bounce; prefer a **distance-travelled phase** over a wall-clock sine so bob stops advancing when the actor hits a wall, presses movement into an obstacle or stands still.
- Initial tuning proposal (not yet authored): `HeadBobEnabled=true`, `WalkVerticalAmplitude=0.025m`, `WalkHorizontalAmplitude=0.012m`, `RunVerticalAmplitude=0.04m`, `RunHorizontalAmplitude=0.02m`, `WalkStrideLength=1.6m`, `RunStrideLength=2.0m`, `ReturnToCenterSeconds=0.18`. Keep all amplitudes mild and clamp invalid settings. Speed scales amplitude smoothly.
- Crouching: half normal walking amplitude; **prone: bob disabled initially** (later optional crawl sway), also disabled while jumping/falling, dead, paused or gameplay-input-blocked. Do not bounce eye based on vertical terrain movement or animate while purely rotating mouse.
- Only **translation**, not camera yaw/pitch/roll: no extra weapon aim climb, recoil cancellation, FOV pumping, artificial player movement, or gameplay collision. The crosshair remains centred; the centre-ray origin may move a few centimetres as the real camera eye moves, so confirm near-cover shots and shove ranges remain correct.
- Keep bob below the existing eye-height interpolation/stance smoothing rather than adding offsets permanently to `_previousEye`/`_currentEye` each tick. Avoid drift by recomputing base eye and adding a **single** sampled offset; handle teleport/reset by resetting the bob state. Use physics samples/render interpolation or another bounded smooth approach, never duplicate/compete with the camera's existing `_Process` loop.
- On stopping, ease the offset back to `Vector3.Zero` then **sleep**. The camera's one existing render callback may stay awake for this short reset but must not remain active while idle. No extra nodes, timers, scene-tree searches or allocations in the active tick. Optional later weapon sway should be independent, not bundled into FPS-HB0.

## Proposed implementation sequence (only when approved)

1. Add plain `FirstPersonHeadBob.cs` and grouped `[ExportGroup("Head bob")]` settings to existing FPS master. Read the latest `FirstPersonPlayer` and FPS camera first.
2. Pass actual post-`MoveAndSlide` horizontal travel/grounded/stance from the player into the FPS camera's **existing** `RecordPhysicsPose` call (adjust signature carefully). Integrate only once per physics step; preserve current standing, crouch and prone eye heights and teleport reset.
3. In `FirstPersonCamera`, apply a short smooth local bob offset to the interpolated base eye. Start/wake only when grounded motion requires it; settle/sleep promptly when still. Never modify `_lookPitchDegrees`, `RotationDegrees` or `ApplyWeaponRecoilKick`.
4. Brief build/source review, update owning READMEs, and test the inherited FPS scene plus top-down regression. One commit to dev for a separately authorized implementation job.

## Acceptance and exclusions

Pass: walking feels subtly weighted, sprint is a little stronger/faster, crouching gentler, prone/jumping/static/blocked input not bobbing; turning in place has no bob; walking against a wall does not keep stepping the camera; stopping returns to exact neutral without a pop; transitions between stances, slopes and stairs do not jitter. Recoil and aim orientation remain permanent/consistent, near-wall firing and shove still work, and the FPS camera sleeps after stopping. Top-down `CAMERA/Camera.cs` and its new middle-mouse orbit are unchanged. Check FPS mouse-look sensitivity at 0.10 separately.

Out of scope: actual animation of hands/feet, footsteps/audio, breathing sway, screen shake, landing impacts, weapon inertia, camera tilt, motion blur, or new movement mechanics.

**Local test when authorized:** `dotnet build war-machine.csproj`; F6 `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` then `WORLD/World.tscn`. Godot visual tuning and comfort evaluation stay with the user. This document is design only.
