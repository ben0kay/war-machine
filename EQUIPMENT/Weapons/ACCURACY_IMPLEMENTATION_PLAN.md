# Weapon accuracy — ACC0/ACC1/ACC2 implementation plan

> **Status:** ACC0 implemented (subject to local Godot/.NET validation); ACC1 and ACC2 are proposals only. `implement-td0-td1` branch. This is a cross-cutting weapon/player feature plan, not authorization to implement future phases automatically.

## Objective

Make weapons slightly inaccurate rather than always hitting the centre of the crosshair. Inaccuracy uses a **world-space angular firing cone**: the same shot direction can miss farther-away objects by more distance than close ones. Eventually **crouching improves accuracy**, while **moving and sprinting make it significantly worse**. Preserve intuitive aim: mouse cursor/crosshair remains precise; only the actual fired shot deviates. Apply the same ballistic mechanic in top-down and inherited FPS without copy/paste.

## Code ownership / folders

```text
EQUIPMENT/
  Weapons/
    ACCURACY_IMPLEMENTATION_PLAN.md     # this document
    Shared/
      ShotSpread.cs                     # ACC0: reusable pure shot direction helper
      FirearmSettings.cs                # ACC0: base spread angle per weapon .tres
      FirearmController.cs              # ACC0: apply after checking shot eligibility
    Rifles/Rifle_001/
      Configuration/RifleMaster.tres    # ACC0: initial rifle 0.30° max half-angle
PLAYER/
  Equipment/
    PlayerWeaponController.cs          # ACC1: top-down supplies player movement modifier
    Accuracy/                          # ACC1 ONLY; not created until phase authorized
      PlayerAccuracy.cs                # calculates multiplier from actual movement/stance
      PlayerAccuracySettings.cs        # editor-tunable multipliers
      README.md
DEBUG/TEST/FirstPerson/
  EQUIPMENT/FirstPersonWeapon.cs       # ACC1: supplies same player movement modifier
```

The **player controls accuracy effects due to stance/movement**, while **weapon owns its intrinsic accuracy and hit calculation**. Do not put weapon spread/raycast code inside `PlayerController.cs`; avoid species-specific knowledge in `FirearmController`. Any future armed NPC can use the firearm math with its own accuracy multiplier.

## ACC0 — basic firearm spread — IMPLEMENTED, LOCAL VALIDATION PENDING

- Expose `FirearmSettings.BaseSpreadDegrees`, a **maximum angular deviation (half-angle)** in degrees, default **0** for legacy/custom firearm safety.
- Set `EQUIPMENT/Weapons/Rifles/Rifle_001/Configuration/RifleMaster.tres` to **0.30°**. Inspector setting **0° restores exact original accuracy**.
- Add `ShotSpread.Apply(Vector3 aimDirection, float maxDegrees, RandomNumberGenerator rng)`, which samples once inside a cone using a random disk at the muzzle; raycast distance automatically amplifies the visible offset. Near-vertical directions remain stable. No unbounded Gaussian/external assets.
- In `FirearmController.TryFire()`, **check equip/muzzle/cooldown first**, normalize aim and apply spread exactly once, then perform the existing probe-to-muzzle obstruction ray and hit ray. Existing weapon damage, typed hit delivery, effect tracer endpoints, audio, timer, target position, aim adapters and collision mask are preserved.
- Reuse a per-weapon `RandomNumberGenerator`, initialized once. No additional firing raycast, polling callback, recoil component, input action, scene node or timer.
- Both top-down `PLAYER/Equipment/PlayerWeaponController.cs` and inherited FPS `DEBUG/TEST/FirstPerson/EQUIPMENT/FirstPersonWeapon.cs` already call the **same** `FirearmController.TryFire`; they need no ACC0 changes.
- Do not randomize the crosshair position or camera direction; the tracer should correctly show the deviation. A 0.30° max deviation at 30m is up to ~0.16m sideways, at 60m ~0.31m.

## ACC1 — player movement and crouch accuracy — FUTURE / NOT STARTED

Add a small stateless (unless smoothing is needed) `PLAYER/Equipment/Accuracy/PlayerAccuracy` helper and optional `PlayerAccuracySettings` Resource, with editable multipliers. Suggested defaults (for discussion, not implementation):

| State | Multiplier (base 0.30°) |
| --- | --- |
| Standing still | 1.00x (~0.30°) |
| Crouching still | 0.65x (~0.20°) |
| Walking | 2.00x (~0.60°) |
| Sprinting | 3.50x (~1.05°) |
| Crouch walking | 1.30x (~0.39°) |

- Use **actual horizontal velocity/speed**, not solely the movement input keys. Stance is from existing `PlayerController.IsCrouching`. If interpolation is desired, read the actual crouch fraction/height safely, rather than adding another per-frame loop.
- Make the caller supply a **spread multiplier** to the firearm (default 1 for backward compatibility); `FirearmController` remains agnostic about player movement. Ensure same integration in top-down and FPS adapters.
- Combine values only once per shot and clamp extreme spread to a sensible cap. Preserve old call sites and avoid requiring changes for NPCs.
- No extra physics tick or new signal needed to know accuracy; calculate from current movement when a shot passes its gate, and preserve current projectile/obstruction/hit logic.

## Recoil is a separate plan (2026-10-28)

**Do not merge ACC0 spread with cumulative recoil.** Random dispersion remains
`ShotSpread.Apply` / `BaseSpreadDegrees`. The owner separately requests **persistent,
per-shot aim climb** that changes actual hit rays and rewards burst firing, plus
top-down and FPS weapon movement. This is tracked under
[RECOIL_IMPLEMENTATION_PLAN.md](RECOIL_IMPLEMENTATION_PLAN.md) as RC0–RC2;
those phases are planned only. ACC1 continues to mean stance/movement-based
random accuracy multipliers. ACC2's possible *recoil-induced bloom* below is
additional optional random spread, **not** the gameplay recoil system.

## ACC2 — optional feel/polish — FUTURE / NOT STARTED

Only if requested: transient recoil-induced bloom for sustained automatic fire, gradual accuracy recovery, dynamic crosshair expansion, aiming/weapon attachments and/or debug aim cone visualization. Any smoothing must wake only while needed and sleep when settled. No automatic scope creep into ammo/reload/projectiles.

## Minimal QA / handoff

1. `dotnet build war-machine.csproj` in a Godot .NET SDK environment, then F6 `WORLD/World.tscn`.
2. Shoot nearby and distant targets while standing still; deviation should be modest close-up but noticeable at longer distances. Try `BaseSpreadDegrees = 0` then `0.30` in RifleMaster.
3. F6 `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`: same rifle spread; FPS centre-screen aim remains unmoved.
4. Check shoulder-to-muzzle obstruction at walls/cover; no shooting through a barrel obstruction. Confirm accurate hit damage and tracers still terminate at actual impact.
5. Check holding fire, cooldown, ALT-TAB/focus loss and hovering UI: no accidental extra shots; no idle callbacks added.
6. After ACC1 only: crouch still improves; crouch moving is worse than crouch still; walking and sprinting are worse than standing.

**Do not modify player movement, camera, zombie AI or terrain during ACC0.** Update feature README and report only checks actually performed; prefer a brief review/build, leaving extensive playtesting to the owner. One commit per job; push to `implement-td0-td1`, not main.
