# Weapon recoil — RC0/RC1/RC2 implementation plan

> **Status (2026-10-28): Planning only. RC0, RC1 and RC2 are NOT implemented.** This document records approved design direction, **not permission to implement all stages automatically**. Each stage needs explicit authorization, one focused commit and a separate validation handoff. Repository: `ben0kay/war-machine`, branch **`implement-td0-td1`**; never target `main` without the owner's explicit instruction.

## Design goal

Add **real, cumulative, controllable weapon recoil** to both the primary **angled top-down 3D view** and the inherited **debug FPS view**. Especially for the **Machine Gun 001**, holding fire must make bullets progressively climb **above the direction the player intends to aim**, even with a stationary mouse or FPS look. The weapon also visibly kicks backward and pitches up. A short burst should have better practical grouping than spraying continuously; changing mouse aim (downward compensation) and releasing to recover provide player control.

**Critical separation: spread is NOT recoil.**
- **Spread (existing ACC0):** independent random per-shot angular dispersion inside `ShotSpread.Apply(...)`, configured by `BaseSpreadDegrees`. Rifle 0.30°; machine gun 0.75°; no persistence from shot to shot.
- **Gameplay recoil (RC):** deterministic/predictable *persistent* angular displacement that accumulates **only after a shot is successfully accepted**, persists between shots and recovers over time when firing pauses. It changes the **real shot ray, hit location and tracer**, relative to the player's desired aim. The first shot is near base aim, later shots ride the accumulated offset.
- **Visual recoil (RC):** brief backward translation, muzzle rise and small arm/upper-body absorption. It should **correspond to the gameplay direction**, but visual animation must not separately bend the shot ray a second time. Camera kick in FPS is optional presentation; prevent double-counting gameplay recoil if it rotates the actual view.
- **Hit reaction / enemy stagger:** a different existing system (`BulletImpactStrength` / `BulletStaggerSeconds`) and **must remain unaffected**.

This is **not** optional accuracy bloom from ACC2, not random shot spread, and not Q-shove knockback. Keep those separate. See [accuracy plan](ACCURACY_IMPLEMENTATION_PLAN.md) for ACC0/ACC1 and note that ACC2's optional recoil *bloom* is deferred and not part of these phases.

## Confirmed baseline in current source

- `EQUIPMENT/Weapons/Shared/FirearmController.cs` owns per-weapon cooldown via one-shot Timer, `TryFire(Vector3 aimPoint, Vector3 probeOrigin)`, existing shoulder-to-muzzle obstruction and hitscan/damage/impact/tracer pipeline. It currently derives a direction from the actual Muzzle to the supplied aim point and then calls `ShotSpread.Apply` once for a shot that passed eligibility. `ReadyToFire` triggers the held-fire adapter for another shot without idle polling.
- `FirearmSettings.cs` exposes intrinsic accuracy and shot/stagger values; `RifleMaster.tres` and `MachineGunMaster.tres` are the *per-weapon* masters. Existing data: **rifle 95 physical damage / 0.65s interval / 0.30° spread**; **machine gun 25 physical / 0.10s interval / 0.75° spread**.
- `PLAYER/Equipment/PlayerWeaponController.cs` obtains mouse cursor -> world-space aim using a camera ray on relevant events. It also supplies the `WeaponMount` and `WeaponProbeOrigin`; `PlayerController` owns the hand/arm/upper-body procedural rig.
- `DEBUG/TEST/FirstPerson/EQUIPMENT/FirstPersonWeapon.cs` obtains a centre-screen ray and world-space aim **when a shot is requested**, independent of top-down camera ray logic. Both adapters fire through the same `FirearmController.TryFire`.
- `EQUIPMENT/Shared/WeaponSlots.cs` selects test slot 1/2, unequips/hides the previous gun and reuses its scene. `PlayerQuickMelee` handles Q-shove with its own cancel/pose lifecycle; recoil must not fight that pose.
- `ShotSpread.cs` already samples RNG on accepted shots only. **No recoil state, aim-climb system, camera kick or recoil tuning currently exists.**

## Starting tuning for discussion (not yet in resources)

| Setting | Rifle 001 | Machine Gun 001 |
| --- | ---: | ---: |
| Gameplay vertical recoil per accepted shot | **2.0°** | **0.55°** |
| Maximum accumulated vertical offset | **3.0°** | **4.5°** |
| Gameplay horizontal recoil | None / negligible | Small, bounded (e.g. alternating ±0.08° per shot; cap ±0.6°) |
| Visual backward kick (toward shoulder) | **0.09 m** | **0.035 m** |
| Visual muzzle-up pitch impulse | **5.0°** | **2.0°** |
| Visual settle/recovery target | **~0.22s** | **~0.12s** (overlapping pulses accumulate safely) |
| Gameplay aim recovery | **~12°/s** | **~7°/s** |
| Delay after last shot before gameplay recovery | **~0.10s** | **~0.12s** |

Keep **gameplay recoil** (aim climb) and **visual recoil** (model kick) separately tuneable in `FirearmSettings` as grouped export fields, backed by each weapon's existing `.tres`. Values are starting points for local feel, **not applied code**. Don't introduce a new standalone recoil master unless a real reusable profile is justified. Use sensible finite/clamped values; zero per-shot gameplay recoil must reproduce the previous shot-direction behaviour (including spread).

## Contract shared by all recoil phases

1. **One source of truth:** the owner holds one small `RecoilState` per weapon, or equivalently an owned lightweight helper. It holds accumulated yaw/pitch, optional small active presentation impulse, and time since last accepted shot. It never requires a game-wide manager. New weapons are data-only participants through the existing firearm settings.
2. **Shot order is unambiguous:** obtain desired aim -> read **previous accumulated** gameplay recoil -> apply that offset to actual shot direction -> call the *existing* `ShotSpread.Apply` **once** -> existing muzzle obstruction + hit ray -> damage/impact and tracer from the resolved ray -> report successful shot -> add exactly **one recoil impulse**. Thus shot #1 begins near aim; shot #2 and later climb. Rejected attempts (cooldown, missing muzzle, invalid source, disabled weapon) produce **no new recoil impulse**. Keep current `TryFire` contract backwards-compatible for NPCs/future callers where possible.
3. **Separate view aim and final ballistic direction:** Do not silently mutate the original cursor ray/crosshair or actor locomotion/mouse-facing aim. Recoil is a controlled *shot* offset and a temporary visual pose. Both must reference the **same effective gameplay direction** on each accepted shot, avoiding double recoil caused by visually rotating the muzzle and *also* adding a ray offset.
4. **Top-down semantics:** the fixed cursor remains the player's intended aim. Recoil drifts the actual shot *upward on screen* (using the active camera's projected aim-space up/right basis and a stable reference from the muzzle) rather than blindly increasing world Y and making top-down aim/terrain offsets counterintuitive. Convert the offset into the final 3D shot ray; moving the mouse slightly downward must meaningfully counteract climb. For angled/rotated/perspective or orthographic cameras, derive a view-consistent direction and avoid aiming *behind* the player. The gun model/arms still visibly pitch and move backward in world 3D. Keep cover and terrain stopping the final ray.
5. **FPS semantics:** center-screen look remains the desired aim; accumulate yaw/pitch recoil in camera-relative angular coordinates for the real shot ray. Optionally give the FPS view a small upward kick, but choose *either* camera rotation affecting gameplay or a separate ballistic offset as the authoritative source, **never both added together**. Player can counter by looking down / burst firing. No FPS-only fork of firearm damage/raycast/spread.
6. **Accuracy:** `BaseSpreadDegrees` remains independent randomness around the **recoil-adjusted** ray; recoil alone is cumulative and recovers. ACC1 stance/movement multipliers (still planned) may adjust random spread independently. Explicitly **exclude** additional bloom or crosshair expansion from RC0–RC2.
7. **Lifecycle:** cancel/reset/recover appropriately on weapon switch, unequip, player death (when PH0 exists), Q-shove start, focus loss, opening UI, Escape/mouse uncapture in FPS, and scene unload. At Q, choose a deterministic pause/cancel policy so shove pose exclusively owns hands/weapon. No impulses while not actually firing; no stale recoil when returning to a swapped weapon.
8. **Performance:** no always-running recoil loop. A successful shot wakes the relevant recoil presentation and recovery callback; callbacks stop when the animation and accumulated aim offset settle. During a sustained held trigger, reuse the existing shot scheduling; no extra shot raycasts, queries, timers per bullet, scene scans, mesh allocations, frame-by-frame randomization or background activity when idle.

## RC0 — Shared recoil state and shot integration

**Status: Planned.** Add a small reusable `EQUIPMENT/Weapons/Shared/FirearmRecoil.cs` (or similar plainly named helper), plus grouped `FirearmSettings` exports and per-weapon master values. No new parent folder required. Use a compact value state with bounded pitch/yaw accumulation, post-shot impulse, active recovery, `Cancel/Reset` and a settled flag. No Godot Node or timer if a lightweight helper and the existing active-only update owner suffice.

Keep `FirearmController` as the sole accepted-shot authority: recoil must **not** increment on rejected TryFire attempts, focus loss, cooldown or an unaccepted held-fire request. Preserve the current damage/cover/tracer pipeline, the current `ReadyToFire` scheduler and `ShotSpread` randomness. Add only a narrow, backward-compatible aim-offset API/callback needed by view adapters; do not copy FirearmController into player or FPS code. First shot uses pre-impulse recoil; subsequent shots use accumulated state. Prefer a predictable shot pattern so burst handling is learnable.

**RC0 scope boundary:** data/state/accepted-shot plumbing only. Do **not** add player camera shake, actual top-down visual recoil, FPS visual recoil, ACC1 movement accuracy or input changes until RC1/RC2 are approved.

**Pass:** new state/setting maths clamps finite values, recovers predictably, zero-recoil matches previous ray, only successful shots add an impulse, cooldown doesn't generate recoil, separate weapon instances reset cleanly; rifle/machine-gun stats and `ShotSpread.Apply` continue unchanged. Build `dotnet build war-machine.csproj`; basic top-down/FPS firearm regression.

## RC1 — Primary top-down gameplay + visual recoil

**Status: Planned. Depends on RC0.** Adapt `PLAYER/Equipment/PlayerWeaponController.cs` to supply the camera-relative up/right reference (or a validated recoil-aware target direction) into the shared recoil system. Keep the desired `_aimPoint` and cursor stable; recoil instead displaces the actual outgoing ray. Ensure aim correction by moving the cursor *down* works in orthographic and perspective modes, while camera yaw rotations and pitch changes do not unexpectedly reverse the recoil direction.

Animate firearm backward kick / upward muzzle pitch plus small shoulder/elbow reaction using **cached pivots and existing player rig**. The recoil animation is additive and temporary: do not permanently alter `WeaponMount`, player collision, locomotion or underlying `SetWeaponAim`. Q-shove two-handed sideways pose has precedence and must restore the correct ordinary weapon pose when it ends. Avoid moving the actual Muzzle before the shot's direction is resolved; render visual recoil **after** the accepted shot, so shot origin/cover checks are stable. Light rifle thump versus many smaller MG impulses, bounded so 600-RPM fire cannot accumulate unlimited mesh rotation.

**Pass:** shoot at an immobile reference target while cursor stays on it: first MG shot near target, later sustained shots drift visibly **up on screen**, including tracer/hit results (not just weapon mesh). Short bursts are closer to aim; release settles to neutral; moving the mouse down compensates. Compare with the rifle's stronger single kick. Test camera yaw (0°, 90°, 180°, 270°), overhead tilt/zoom, slopes and cover, sprint/crouch, weapons 1/2, Q shove and UI focus. No more physics work while fully idle.

## RC2 — FPS gameplay + view-model recoil

**Status: Planned. Depends on RC0; ideally RC1 validated first.** Adapt `DEBUG/TEST/FirstPerson/EQUIPMENT/FirstPersonWeapon.cs` and its camera/view-model pose using the **same** accumulated recoil state and shot ordering as RC1. While firing, real bullets climb relative to the centre-screen desired aim; restore after a break; mouse-look down can counter. Model slides back and lifts; optional very small camera kick, with a single authoritative recoil source and no second offset. Keep the physical world-space muzzle, shoulder ray and nearby wall blocking intact.

**Pass:** single rifle shot kicks once, MG 600-RPM builds predictable capped climb, burst firing resets grouping after recovery, moving the mouse down counters, spread still applies around final direction. Test near/far obstacles, centre-screen aiming, pause/switch/uncapture/Escape/focus loss, Q shove cancellation and no leftover pitch/camera offsets. The FPS mode remains a debug comparison: share underlying mechanics but avoid excessive FPS-only polish.

## Integration and test handoff for Codex

Read root `README.md`, `CodePreferences.md`, this plan, `Weapons/ACCURACY_IMPLEMENTATION_PLAN.md`, `Weapons/Shared/README.md` and the **live source and masters** before each authorized phase. Update the own README and this plan with exact status, values, limitations and checks after implementing. Don't infer earlier planning snippets are live behavior.

- Use **one GitHub commit per authorized task** on `implement-td0-td1`. No edits to `main`.
- After implementation, when available compile C# then run F6 `WORLD/World.tscn` and `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`, with brief baseline checks before owner visual/balance tuning.
- Validate shots that miss, hit terrain, hit zombies, hit cover, are blocked by shoulder-to-muzzle protection, or occur near a wall. Tracers must follow **actual recoil-plus-spread result**, never the old cursor aim.
- Verify held automatic fire, one-click short bursts, recovery during quiet periods, selected weapon switching and false triggers; no accumulated state leaking into unrelated weapons.
- **No extra projectile simulation, camera aim ray per idle frame, per-bullet scene creation, new input keys, ammo/reload, new projectile physics, change to damage/stagger, ACC1/ACC2 or Q shove redesign.** Clearly say if any local build/gameplay checks were unavailable.

**Next suggested implementation:** RC0 only, then RC1, then RC2 after explicit approval. This entire document is **a plan only**; it makes no gameplay changes by itself.
