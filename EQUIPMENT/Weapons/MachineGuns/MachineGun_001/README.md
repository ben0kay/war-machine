# MachineGun_001 — low-poly light machine gun

**Status:** Authored, pending local Godot/.NET build and visual/gameplay validation. A new **independent weapon choice**, now wired into the shared test weapon slots.

- `MachineGun_001.tscn` reuses the same `FirearmController.cs` as Rifle_001.
- `Configuration/MachineGunMaster.tres` uses the common `FirearmSettings` with a faster fire rate, different spread, damage and range.
- `C/MachineGunMeshBuilder.cs` creates original, removable low-poly weapon art once, with heavier silhouette, ammunition box, barrel shroud, carrying handle and folded bipod. A future imported GLB can replace the visual without changing firing.
- `Artwork/` is reserved for replacements and source assets.
- **FPS framing:** `MachineGunMaster.tres` sets `FirstPersonOffset = (0.07, -0.18, -0.23)` and `FirstPersonScale = 0.78`, relative to the default FPS weapon offset. This lowers and reduces the bulky gun in the camera without altering its top-down model, damage or actual muzzle alignment. The rifle retains its original FPS presentation.

## Proposed initial balance, now authored

| Setting | MachineGun_001 | Existing Rifle_001 |
| --- | --- | --- |
| Shot interval | 0.10 s (600 RPM) | 0.65 s (~92 RPM) |
| Automatic | Yes | Yes |
| Damage | 25 physical | 95 physical |
| Range | 85 m | 90 m |
| Base spread | 0.75° | 0.30° |
| Tracer visual lifetime cap | 0.30 s | 0.35 s |
| Reusable tracer slots | 4 | 1 (default) |
| Flash lifetime | 0.05 s | 0.07 s (default) |
| Sound | Optional, unset | Optional, unset |

Shots share `ShotSpread`, the existing shoulder-to-muzzle obstruction check, hit/damage pipeline, automatic trigger timer and `FirearmShotEffects` pooled tracers. No ammunition count, belt-feed simulation, reload, weapon switching, new input bindings, overheating or special NPC handling has been added.

## Weapon-specific bullet stagger tuning

The shared `FirearmSettings` resource already gives each firearm its own `BulletImpactStrength` and `BulletStaggerSeconds`; no separate MG receiver/attack code is needed. The current MG master uses **25 physical damage**, **0.35 impact strength** and **0.08s requested stagger**, versus rifle **95 physical**, **1.0 impact strength** and **0.20s**.

The zombie's existing CM1 recipient now scales both the instant retained movement velocity and temporary chase-speed penalty with the effective impact strength (after recipient impact resistance). Strong rifle hits still retain the original near-stop behaviour, while the MG makes a smaller visible flinch with a much softer, shorter slowdown. The shared anti-stunlock `MinFlinchInterval` / `MinRecoverySeconds` remains unchanged: not every 600-RPM bullet interrupts the zombie, although **every accepted bullet still damages**.

Against an unarmoured default 100-HP zombie, **four MG hits** are needed (25 each). Approximate sustained damage is **250 HP/s** assuming all shots connect. This is a balance/configuration pass, not a change to firing, visuals, hitscan, recoil physics or other enemy types. Build and test in both camera modes locally.

## How to test without implementing weapon switching

Edit `EQUIPMENT/Configuration/EquipmentMaster.tres` and set `DefaultWeaponScene` to `res://EQUIPMENT/Weapons/MachineGuns/MachineGun_001/MachineGun_001.tscn` in Godot. The default **remains Rifle_001** in source control, with SecondaryWeaponScene now selecting this machine gun. This configuration affects both top-down and the inherited FPS test using `EquipmentMaster`. The current input adapter already supports holding the trigger for automatic fire.

Press F6 in `WORLD/World.tscn`, check the heavier mesh/hand placement, muzzle/tracer start, sustained 600 RPM, damage, spread and nearby-cover blocking. F6 `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` for the FPS alternative.

**Important:** Automatic fire now launches short traveling streaks instead of full-length yellow lines. `TracerEveryNShots=2` displays one visible streak per two actual bullets; all accepted bullets still damage, flash/light and play their existing sound. Four pooled core/halo pairs allow overlap without creating firing-time meshes. The muzzle flash remains one reused mesh. The optional shot audio remains **one** `AudioStreamPlayer3D`, so overlapping machine gun sound/loop polish is a separate future task. No runtime/render test claimed here.

Performance: `TracerPoolSize=4` pre-creates four tracer meshes at weapon initialization. Each shot only cycles an existing slot; no per-shot nodes, physics queries, materials or allocations from the VFX layer. `FirearmShotEffects._Process` runs only while a flash or at least one tracer is active, then sleeps. Stop/unequip hides every slot.

Test slots now implemented: press **1** for rifle, **2** for this machine gun in either view. No need to replace DefaultWeaponScene manually. Shared WeaponSlots handles switching; weapon mechanics and pooled effects are unchanged.

## Procedural model polish — geometry only

The existing light machine gun now uses chamfered/tapered receiver and stock forms, a more integrated cheek rest, open trigger guard, side feed-box with hinges/latch/reinforcing ribs, revised ejection/bolt cover, recessed cooling slots, real round heavy barrel/collar/brake, an open-air carry handle, raised sights and refined folded bipod. These details keep its heavy appearance without increasing the weapon's original envelope. Its `Muzzle` marker remains at `(0, 0.06, -1.20)` along local -Z. The authored FPS offset `(0.07, -0.18, -0.23)` and scale `0.78` are untouched.

Geometry shares `EQUIPMENT/Weapons/Shared/WeaponVisualGeometry.cs` with the rifle; colours remain their original simple palettes with **no first-person material/texture/shader pass**. One-time mesh construction, cached static forms, no animations/process loops, no new collision. Controller, rate of fire, accuracy, shot sound/light and muzzle flash stay unchanged. **Local Godot/.NET build and top-down/FPS visual checks pending**.

## Moving tracer pass

MachineGunMaster now uses `TracerSpeed=360` m/s, `TracerLength=1.4`m, `TracerWidth=0.012`m, `TracerEveryNShots=2`, `EffectDuration=0.30`s max and `TracerPoolSize=4`. Setting `TracerEveryNShots=1` shows every shot again. These values affect appearance only; hitscan, impact strength/stagger, muzzle flash, lighting, sounds and cadence remain unchanged. Check fast turning, MG burst overlap and FPS/top-down visibility after compiling Godot C#.

## RC0 cumulative recoil configuration (2026-10-28)

The master now authors `RecoilPitchPerShotDegrees=0.55`, `RecoilMaxPitchDegrees=4.5`, `RecoilYawStepDegrees=0.08`, `RecoilMaxYawDegrees=0.6`, `RecoilRecoveryDegreesPerSecond=7`, `RecoilRecoveryDelaySeconds=0.12`. Future RC1/RC2 view-model settings: 0.035m backward, 2° muzzle pitch, 0.12s visual recovery. The shared `FirearmRecoil` state is implemented and accumulates per accepted shot. **No current bullet trajectory, camera or model displacement yet**: the view adapters must opt into the recoil-up overload in RC1/RC2. Keep 25 damage and 0.75° intrinsic spread unchanged.

## RC2 FPS recoil (2026-10-28)

The existing +0.55°/shot capped 4.5° recoil now climbs in **real FPS 3D shot direction**, independently of random spread; it is not just a flat crosshair effect. A shared Visual-only helper animates 0.035m backward and 2° up per accepted shot with 0.12s recovery, pooling/capping overlapping 600-RPM impulses. The actual muzzle remains stationary relative to the firearm root. Both top-down and FPS use the same core and per-gun master. No additional FPS camera kick was added. Build and playtest still required; RC0-only notes above are historical.

## FPS camera recoil refinement (2026-10-28)

MachineGunMaster now has `FpsCameraRecoilFollowDegreesPerSecond=10.0` and `FpsVisualClimbMultiplier=0.7`. FPS full-auto progressively tilts the **actual camera** upward toward the existing RC0 4.5° maximum, and the Visual gun pitches a little further up as it kicks; the player can drag mouse down to compensate. FPS bullets use the physically updated camera aim plus the original independent random spread, without doubling recoil. Primary top-down behavior does not change. Source only; Godot visual testing pending.

## FPS one-way 25° burst climb (2026-10-28)

The MG now has `FpsMaxCameraRecoilBurstDegrees=25.0` and `FpsRecoilBurstResetSeconds=0.35` (independent of unchanged top-down max 4.5°). Each accepted 600-RPM shot adds 0.55° to a **positive-only** camera-rise queue, delivered up to 10°/s. A continuous burst can accumulate about 25° after ~46 shots (~4.6 seconds), without automatically lowering the camera afterward. Moving the mouse is the only normal way to correct the settled view angle; releasing resets the burst budget only after the idle gap, never camera pitch. Existing independent random spread/25 damage/stagger are untouched. Test locally.

## AM0 magazine & reload (2026-10-28)

`MachineGunMaster.tres`: `MagazineCapacity=100`, `ReloadSeconds=4.5`, `AutoReloadWhenEmpty=true`, `DebugUnlimitedReserveAmmo=true`, `InitialReserveRounds=0`. Magazine empties after 100 accepted shots (including misses), then the timer blocks further fire until reloaded. R manually tops off. Debug only makes reserve infinite, not firing instantaneous; 600-RPM/recoil/spread/impact values remain unchanged. No HUD/animation yet; local validation pending.
