# ADS — Aim Down Sights implementation plan

**Repository/branch:** `ben0kay/war-machine` → `implement-td0-td1`  
**Owner:** `EQUIPMENT/Weapons/FirstPerson/FirstPersonWeapon.cs`, FPS player and shared firearm settings.  
**Direction:** Hold **middle mouse button (MMB)** in captured FPS gameplay to raise an equipped firearm and align its existing sights near the camera centre. Release to return to hip fire. Do not bind RMB (existing jump).

## Verified existing foundation (2026-10-31)

- `FirstPersonWeapon` owns FPS input, camera-centre shot requests, weapon switching, and accepted-shot visual/camera recoil. It sleeps when its animation/recoil work is finished.
- `WeaponSlots` equips rifle, machine gun, or slot-3 bat; each gun has its own `FirearmSettings` master. `Visual` is the recoil-only child; the gun root also owns the real `Muzzle` marker and shot effects.
- `FirstPersonPlayer` overrides shared movement and reads FPS camera settings; shared `PlayerMovement` composes stance, sprint, lean, and corpse slowdown. FPS view is `WORLD/FirstPerson/FirstPersonWorld.tscn` (F6), not F5.
- Rifle and MachineGun currently have **procedural iron-sight geometry**, not authored optic/lens assets. Their sight-centre placements require local visual acceptance.

## Phases and authorization

| Phase | Purpose | Status |
| --- | --- | --- |
| **ADS0 — held sight alignment** | MMB held animation, camera FOV, per-gun animation/movement multipliers, transition cancellation and existing recoil compatibility | **Implemented in source; awaiting local validation** |
| **ADS1 — weapon carrying weight** | An independent, always-equipped weapon movement penalty per firearm, composed with ADS/stance/lean/corpse/sprint without double-applying | **Implemented in source; awaiting local validation** |
| **ADS2 — optics/accuracy polish** | Future optics/SMG replacements, sway and optional accuracy modifiers; decide sprint/ADS rules, reticles and head-bob treatment | **Planned; not authorized** |

## ADS0: implementation contract

**Input/state:** An unhandled MMB press in captured, focused, unblocked FPS gameplay requests ADS for the active firearm only. An MMB release is watched in `_Input` even when UI consumes it. Interaction focus, Q shove, Escape, focus loss, gameplay blocking, slot swaps, and teardown reset to hip state; melee slot cannot ADS. Releasing MMB animates out normally; interrupted/unequipped guns reset immediately and require a new press.

**Presentation:** FPS adapter animates the firearm **root** from existing hip-fire position to `hip + FpsAdsOffset`; the optional per-gun `FpsAdsPitchDegrees` keeps sight-line adjustments tunable. This root is the same node that parents `Muzzle` (no separate fake muzzle). Existing `FirearmVisualRecoil` still animates only the `Visual` child, and FPS camera recoil keeps its existing persistent mouse-look pitch. Animate FPS `Camera3D.Fov` from its original value to the gun's `FpsAdsFieldOfView` without changing the camera aim or shot ray.

**Timing:** One common `FirstPersonSettings.AdsBaseTransitionSeconds` (0.20s). Actual transition duration = **base duration / per-weapon FpsAdsAnimationSpeedMultiplier** for both raising and lowering (default 1.0×). Use a smoothstep pose/FOV easing along a linear timeline. A direction reversal partway through should reverse from the current blend, not teleport.

**Movement:** The equipped firearm's `FpsAdsMovementMultiplier` applies only while ADS is held. It multiplies the shared speed **after** stance, sprint, lean and corpse modifiers; default 1.0×. The FPS player sets this value on state changes and wakes the existing shared physics callback; shared/top-down player defaults to 1.0×. Always restore 1.0× on cancellation.

**Authored defaults (initial visual guesses, not accepted alignments):**

| Master | ADS root offset (m) relative to hip | Animation | Move | FOV |
| --- | --- | ---: | ---: | ---: |
| RifleMaster | (-0.22, +0.06, +0.03) | 1.10× | 0.70× | 70° |
| MachineGunMaster (future SMG model) | (-0.29, +0.24, +0.20) | 0.90× | 0.85× | 72° |

Iron-sight centres were estimated from the current procedural builders; retune `FpsAdsOffset` / `FpsAdsPitchDegrees` in each master after visually checking the gun at standing, crouching, leaning and moving. This phase does **not** remodel guns, change magazines, make a scope, apply ADS accuracy/recoil bonuses, alter ammo/damage, or implement the ADS1 always-carried weight penalties as part of ADS0 itself. ADS1 is defined separately below.

**Performance:** MMB edges, selection and interruption events wake work. FPS `FirstPersonWeapon._Process` already drives temporary recoil; ADS advances there only during transitions and then sleeps. The movement penalty is updated only on held-state changes; no per-weapon physics loop, additional raycast, timer, manager, scene node or material.

## ADS0 local acceptance

1. Build `dotnet build war-machine.csproj`; open `WORLD/FirstPerson/FirstPersonWorld.tscn` and run **F6**.
2. Hold/release **MMB** on rifle and machine gun; model must center near real sights and return to exact previous hip position/FOV. Reverse MMB while mid-animation. Verify rifle 1.1× feels slightly quicker and machine gun 0.9× slightly slower.
3. Walk/run/crouch/prone/lean while holding MMB; aiming slows rifle to 70% and MG to 85% of the otherwise computed movement target. Release restores it immediately. No movement change in shared top-down `WORLD/World.tscn`.
4. Fire while aiming (including MG automatic bursts), reload with R, switch 1/2/3 mid-transition, Q shove, Escape, open inventory/interactions, alt-tab, die and return focus. No stuck FOV/ADS/gun pose; MMB does not fire or jump.
5. Shoot beside cover while aiming to confirm muzzle obstruction and tracer origin remain plausible; inspect sights at 75° hip FOV / authored ADS FOVs. Local sight tuning may be necessary due to current procedural geometry.

## ADS1 — carried movement penalty and subtle ADS FOV tuning (2026-10-31)

**Implemented in source; awaiting local build and runtime verification.**

- Add `FirearmSettings.EquippedMovementMultiplier` (default **1.0×**), a separate property from `FpsAdsMovementMultiplier`. Rifle starts at **0.90×**, MachineGun/future SMG at **0.97×** while held in FPS. The slot-3 bat retains **1.0×** in this phase; a later broader equipment-weight design may extend this contract.
- On a **successful weapon selection**, FPS `FirstPersonWeapon` sends the new firearm's carried multiplier to `FirstPersonPlayer`; empty/non-firearm selection uses **1.0×**. Failed switches leave the previous multiplier unchanged. On view/equipment teardown reset the FPS player's carried multiplier to 1.0×.
- `FirstPersonPlayer` keeps two separate state fields, carried and ADS; its existing movement-speed virtual hook returns their **product**. The one shared `PlayerMovement` path still multiplies it after stance, sprint, lean and corpse slowdown. Input blocking cancels ADS but does **not** erase the equipped weight; returning to gameplay should preserve the currently selected firearm's penalty. Top-down remains unchanged in this FPS-focused phase.
- No per-frame equipment polling, allocation, new movement loop or second slowdown in the firearm. The existing physics driver wakes only when its cached multiplier changes.
- Fine-tune existing ADS0 **per-gun FOV**, rather than introduce another zoom manager or competing FOV multiplier: base FPS camera **75°**, rifle ADS **70°**, MachineGun ADS **72°**. The same existing ADS pose/FOV animation blends both directions. This is a subtle unscoped zoom, not a scope module or true fixed-power magnification; it does not change camera pitch, shots, recoil, iron-sight offsets or collision.
- **Future optics design (ADS2 only, not implemented):** attachments may supply an *override* for the effective ADS FOV, taking precedence over the firearm's fallback iron-sight FOV; higher-power scopes can use optical magnification or a calculated effective FOV rather than multiplying FOV degrees naively. Avoid stacking permanent FOV multipliers from unrelated owners. Reuse the current FPS adapter's existing one camera-FOV writer, including interrupt/reset rules.

**ADS1 acceptance:**

1. Build `dotnet build war-machine.csproj`, F6 `WORLD/FirstPerson/FirstPersonWorld.tscn` and compare ordinary walking/sprint with rifle (0.90×), MachineGun (0.97×) and bat (1.0×), switching 1/2/3 repeatedly. Movement should respond promptly and not accumulate reductions.
2. Hold MMB: rifle final movement multiplier **0.90×0.70 = 0.63×** of the otherwise computed target; MachineGun **0.97×0.85 = 0.8245×**. Release MMB restores only the ADS component (0.90× / 0.97× carried remain). Crouch/prone/sprint/lean modifiers still compose correctly.
3. Check the subtle ADS FOV (75→70 rifle, 75→72 MachineGun) on press/release, mid-transition reversal, aim/fire/reload, swapping guns, Q, Esc and focus/menu interruptions. Ensure the original FOV and previous sight alignment are restored.
4. F6 `WORLD/World.tscn` once to confirm the shared top-down view retains the original movement defaults and firing behaviour.

**Validation status:** ADS0 and ADS1 are now present in source; **no Godot/.NET build or in-game verification performed** here. Runtime gun alignment, zoom comfort and movement feel require local acceptance.

