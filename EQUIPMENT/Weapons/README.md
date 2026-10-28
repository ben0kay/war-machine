> **ELD1 source implementation (2026-10-28), awaiting local validation:** [Limb condition, deterministic severing and injury-preserving ZD0 handoff](../../ENTITIES/Shared/Anatomy/README.md). Original vitality/weapon HP damage is unchanged. ELD2 locomotion, ELD3/CM5 attacks, ELD4 debris and ZD1–ZD3 remain deferred. Existing World/FPS scenes are the acceptance venue; no new test scenes. This supersedes older ELD1-Planned/no-severing statements below.

> **ELD0 source implementation (2026-10-28):** [Shared region detection](../../ENTITIES/Shared/Anatomy/README.md) now routes firearm shape hits for both zombie variants in both views. BodyRegion moved to ENTITIES/Shared/Anatomy with values unchanged. Existing damage balance/player rules and melee stay unchanged; no injuries, severing or new attacks. Local build/runtime validation pending.

> **FPS ownership:** Active FPS presentation is promoted out of DEBUG/TEST. See the [FPS ownership guide](../../WORLD/FirstPerson/README.md). Shared gameplay and current tuning are unchanged; local validation pending.

# Weapons

Shared/ holds the reusable hitscan controller, settings, shot spread and a
bounded reusable tracer pool.
Rifles/Rifle_001 is the starter firearm; item art and stats remain local.
[MachineGuns/MachineGun_001](MachineGuns/MachineGun_001/README.md) is a separately
authored 600 RPM light machine gun using the same controller and a four-slot
tracer pool. The rifle remains the configured starter gun; weapon switching is
**not** part of this change.
Other firearm scenes can reuse the same mechanics and typed Resource.
[ACCURACY_IMPLEMENTATION_PLAN.md](ACCURACY_IMPLEMENTATION_PLAN.md) tracks the shot-spread
foundation (ACC0) and proposed player-specific stance/movement accuracy (ACC1).
[RECOIL_IMPLEMENTATION_PLAN.md](RECOIL_IMPLEMENTATION_PLAN.md) separately plans **RC0 shared
recoil, RC1 top-down aim climb + animation, RC2 FPS recoil**, with deterministic
shot-to-shot displacement and burst/recovery. **Recoil is not random spread**;
RC0's shared state and settings are implemented in source, awaiting local validation;
RC1 top-down and RC2 first-person actual 3D aim climb and model kick are implemented in source (awaiting local validation).
[AMMO_IMPLEMENTATION_PLAN.md](AMMO_IMPLEMENTATION_PLAN.md) describes **AM0 shared finite magazines and timed reloading (implemented in source; local validation pending)**, AM1 compact event-driven ammo HUD (implemented in source; local visual validation pending), and AM2 physical ammo/magazines/animations (planned). Manual reload is R; debug unlimited reserve still requires actual magazine reloads.
Projectile weapons, physical ammo inventory and reload animation remain future tasks.

## Moving bullet-tracer visuals — Godot validation pending

The original stretched muzzle-to-hit box is replaced with a **short moving bullet streak**. `FirearmShotEffects` now pools a bright thin unshaded core and a translucent amber halo per tracer slot. It stores the hitscan path once and advances only the active visual tip, displaying at most `TracerLength` metres, stopping at the actual hit point or `EffectDuration` upper visual time limit. `TracerSpeed`, `TracerLength`, `TracerWidth` and `TracerEveryNShots` are exported in the shared firearm settings. Visuals skip every N-1 shots on fast automatic weapons if configured; **all shots still deal immediate hitscan damage, collide with cover, flash/light and trigger normal audio.** No projectile physics, additional rays or per-shot mesh/material allocations were added.

Rifle defaults: **300m/s**, **1.7m** streak, **0.014m** core width, every shot, 1 pooled slot, 0.35s maximum travel feedback. Machine gun: **360m/s**, **1.4m**, **0.012m**, every second shot, 4 pooled slots, 0.30s maximum travel feedback. The rendered hit location may appear a fraction of a second after damage (cosmetic only).

Existing pooled effects still disable recycled physics interpolation, and muzzle flash follows the interpolated muzzle only while needed. `Stop` hides every slot and the shared feedback loop sleeps when no tracer, flash or muzzle light is active. No effect loops run on idle weapons.

**Godot/local checks:** compile the C# project, run top-down and FPS, fire rifle/machine gun into nearby walls and distant targets while walking/turning, and verify no full-length lines or through-wall tracer tips. Check flash, sound, hits, spread, cover checks, switching and focus-loss cancellation. High frame times can show visible frame-stepping at these fast cosmetic speeds. Source review only: actual render appearance/build/performance are unverified.

## Shared light and rifle audio foundation

FirearmSettings.EmitMuzzleLight opts each weapon into a cached warm, shadowless real light driven by existing FirearmShotEffects lifetime; both current masters explicitly enable it. LightingMaster owns light colour/energy/range/duration and the active cap. ShotCue opts a firearm into the pooled world audio service supplied through WeaponSlots; RifleMaster uses AUDIO/OneShots/Weapons/RifleShot.tres with an intentionally empty Stream. Assign a WAV/OGG there to hear it. Machine gun has no sound assigned and needs its own cue. Legacy ShotSound only runs without ShotCue, preventing double playback. Accepted-shot damage/spread/cover/cadence remain unchanged. See AUDIO/README.md and ENVIRONMENT/Lighting/README.md for APIs, settings, limits and brief local checks. Compilation/visual/listening acceptance pending locally.
