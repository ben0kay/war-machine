# FPS-HB0 — First-person walking head bob

> **Status: Implemented in source (2026-10-28), awaiting local Godot/.NET build and visual acceptance.** Dedicated to the inherited first-person test; top-down player and camera are unchanged. Development branch: `implement-td0-td1`.

| Phase | Deliverable | Status |
| --- | --- | --- |
| **FPS-HB0** | Distance-driven walk/run eye bob, subtle crouch, smooth return to neutral, zero idle processing | **Implemented; Awaiting Validation** |

## Confirmed implementation and ownership

```text
DEBUG/TEST/FirstPerson/
  CAMERA/
    FirstPersonCamera.cs              existing FPS camera, composes bob with interpolated eye
    FirstPersonHeadBob.cs             NEW plain C# state helper, not a Godot scene node
    HEAD_BOB_IMPLEMENTATION_PLAN.md   this status and validation handoff
    README.md
  PLAYER/
    FirstPersonPlayer.cs              samples real horizontal displacement after shared MoveAndSlide
  CONFIGURATION/
    FirstPersonSettings.cs            grouped Head bob (FPS only) exports
    FirstPersonMaster.tres            initial tuning, master defaults
```

- `FirstPersonPlayer._PhysicsProcess()` records its world position before and after the single existing shared movement step. It passes **post-collision horizontal displacement**, grounded state, input intent, stance and delta to `FirstPersonCamera.RecordPhysicsPose`. There is no new movement, collision or physics callback.
- `FirstPersonHeadBob` is a plain class owned by the FPS camera. Distance travelled advances a stride phase; actual speed blends between walking and running amplitude/stride. Crouch applies half intensity, while prone, airborne, stationary, blocked, dead, uncaptured and unfocused states stop new steps.
- `FirstPersonCamera._Process()` retains its render interpolation of the base eye (standing/crouch/prone and jumping). On active samples it adds a tiny camera-right X sway and world-up Y bounce; after stopping it exponentially returns to **exact zero** and sleeps. Any large teleport resets the phase and offset. No second camera node, timer or independent update loop exists.
- Bob only changes the final **camera position**; it never writes `RotationDegrees`, `_lookPitchDegrees`, `ApplyWeaponRecoilKick` or physics velocity. FPS centre-ray aiming uses the actual small eye translation, so close-wall shooting/shove still warrant checking. Existing FPS mouse sensitivity stays **0.10**.
- Releasing mouse capture or losing window focus stops new bob and allows the remaining offset to settle. No extra callbacks should run for an idle character after the return finishes. The top-down MMB orbit is not affected.

## Current defaults (FirstPersonMaster.tres)

| Setting | Value |
| --- | ---: |
| `HeadBobEnabled` | true |
| `WalkVerticalAmplitude` / `WalkHorizontalAmplitude` | 0.025m / 0.012m |
| `RunVerticalAmplitude` / `RunHorizontalAmplitude` | 0.04m / 0.02m |
| `WalkStrideLength` / `RunStrideLength` | 1.6m / 2.0m |
| `HeadBobReturnSeconds` | 0.18s |

Settings are authored only in existing FPS configuration; enable=false is an easy comparison baseline. The helper bounds unreasonable amplitude/stride values. Values are a **first pass** and may need subjective tuning for comfort.

## Acceptance checklist and open limitations

- `dotnet build war-machine.csproj`, then run `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` (F6). Check walk, reverse, strafe, sprint, crouch, prone, leap/fall, stop-start, walking into a wall, slope/stairs, released pointer/Escape and inventory input block.
- Verify no bob just from mouse look, no wobble when stationary or wall-blocked, gentle return to true neutral after stopping, no lost mouse aim, no cancellation of permanent recoil, unchanged gun/muzzle obstruction, weapon alignment and crosshair ray, no eye clipping through nearby cover.
- Inspect FPS camera processing when idle: the render callback should turn off once eye interpolation and bob have settled. Check `WORLD/World.tscn` top-down camera and MMB rotation as regression.
- No local compilation or in-engine motion/performance measurements were available from the authoring environment; acceptance and comfort tuning remain to be tested by the user.

**Out of scope:** head tilt/roll, breathing/idle sway, camera shake, landing thud, footsteps/audio, hands, full-body IK, weapon-specific motion and changed movement physics. Do not silently start those features.
