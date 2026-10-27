# COMBAT — cross-actor impacts and reactions

**CM0/CM1 and CM2-A/B1/B2/C implemented in source, awaiting local validation. CM2-D and CM3–CM5 remain planned.** Q shove is wired in both camera modes; attack animations, bat, knife, zombie attacks and player health remain future work.

## One contact, separate damage and impact

Shared/CombatImpact.cs supplies a readonly payload and optional IImpactReceiver. DamagePacket/IDamageReceiver/HealthComponent/DamageResolver remain in ENTITIES/Shared/Combat, unchanged. Do not create a second damage pipeline.

Shared/HitTargetResolver.Dispatch(collider, source, nullableDamage, nullableImpact) walks at most 32 parents, resolves the first supported actor once, excludes self and applies positive damage **before** optional impact. Scenery still blocks the original hitscan; the dispatcher never adds a ray or searches behind cover. A freed/queued actor receives no subsequent impact. Receivers also guard their own dead/inactive state.

For an impact-only contact, pass null damage (or nonpositive damage) plus CombatImpact. No HP event/fake damage is needed. A damage-only contact can omit impact; legacy receivers without IImpactReceiver keep receiving damage. Bullet direction uses source-to-contact projected onto XZ, with a finite forward fallback for coincident/vertical hits. Strength/duration are finite/clamped. Zombies now respond to Bullet and Shove. Shove now causes collision-aware backward travel (CM2-B2); Melee/Blast remain reserved.

## CM1 zombie response

Both slow and runner controllers share reaction logic in ZombieController. Health resolves first; a survivor can accept one Bullet impact. Reactions immediately damp horizontal velocity, then target about 10% normal travel speed for the short duration. Gravity, MoveAndSlide, terrain contact, current goals and existing sight timer continue. The latest AI goal cannot bypass the reaction speed limit. A small additive visual-root recoil overlays the species gait/lean without altering its collider or permanently accumulating rotation. After expiry, movement recovers with configurable acceleration, then normal movement/sleep resumes.

Reactions/HitReaction.cs is a plain value-state helper advanced by the existing active physics callback. An active reaction is never refreshed/extended. Each accepted hit reserves at least duration + MinRecoverySeconds before re-flinch (and respects MinFlinchInterval), with a per-reaction duration cap. This guarantees recovery gaps under fast fire; damage still applies on every accepted shot even when reaction is refused. Immunity/resistance/threshold are target policy. Death/deactivate/reset clear reaction and additive pose; there is no separate reaction node/timer/idle poll.

### Starting tuning

| Profile | Impact strength | Requested stagger |
| --- | --- | --- |
| RifleMaster | 1.0 | 0.20s |
| MachineGunMaster | 0.65 | 0.14s |

Both zombie masters expose BulletReactionsEnabled, ImpactResistance, FlinchThreshold, MaxStaggerSeconds, MinFlinchInterval, MinRecoverySeconds, StaggerMoveMultiplier, ImpactVelocityRetention, StaggerBrakingAcceleration, RecoveryAcceleration, RecoverySeconds and FlinchDegrees. Runner resistance starts at 0.1; normal zombie at 0. Resistance reduces strength and duration, and 1 is immune. Default threshold 0.2, max duration 0.3s, minimum interval 0.45s and recovery gap 0.2s. Bullet rotation is cosmetic; bullet impacts still do not physically push. Shove impacts now do.

Damage stays rifle 95 / MG 35 against default 100HP: two rifle hits / three MG hits kill. Check the first nonlethal shot to see stagger; the lethal shot still removes the zombie normally. To inspect repeated long bursts locally, temporarily raise test HP and restore it afterward rather than changing production gun damage.

### Validation handoff

Focused C# loop/structure, contact ordering/self/scenery guards, settings bindings and a numeric reaction/cooldown model checked. **Compilation and Godot runtime/visual checks remain unverified: no Godot/.NET executable here.**

Locally run `dotnet build war-machine.csproj`, then F6 World and FPS. Shoot a slow zombie and runner: first rifle hit flinches/slows and chase resumes, second kills once. Check MG bursts, stationary target, moving/turning target, cover/misses, immunity/disabled reaction and scene unload. Exercise Dispatch with null damage + Bullet impact: HP unchanged, eligible actor reacts; Shove does not yet move a zombie. Confirm inactive/dead actors ignore impacts, horde work stays bounded, settled actors sleep, normal animations/vision/terrain/audio/muzzle lights still work. Existing physics queries are unchanged. Exact recoil appearance and balance need owner acceptance.

No combat singleton, actor registry, scene scans, added hit rays, status framework or per-hit heap allocation. New value payloads and cached actor state have zero idle callbacks. Existing ground-recovery and sensing paths are retained.

## CM2-A — melee detection foundation (2026-10-27)

**Implemented in source; awaiting local build/runtime validation.** `Melee/MeleeSettings.cs`, `Melee/MeleeAttack.cs`, `Melee/README.md` and `Configuration/QuickShoveMaster.tres` provide one-target active-window detection, configurable range/arc/height/timing, obstacle checks and both overhead-horizontal/FPS-pitch aim modes, without requiring input or animation. `MeleeAttack` is instantiated by the future caller and only queried while an attack is active. It returns eligible contact data; CM2-C will dispatch it through the existing CM0 resolver. CM2-B1 recipient stagger is implemented and CM2-B2 now adds physical knockback.

The premature CM2-B code/settings from the previous commit were reverted following the phase-letter correction; both zombie types retain CM1 bullet behaviour. No Godot/.NET build or runtime validation was performed; see `Melee/README.md` for test handoff.

## CM2-B1 — simple zombie shove pause (2026-10-27)

**Implemented in source, awaiting local validation.** Zombies now accept `CombatImpactKind.Shove` through their existing `IImpactReceiver`. In the original B1 implementation, a zero-damage shove stopped XZ velocity; CM2-B2 replaces that stationary pause with a collision-aware backward slide. Gravity, `MoveAndSlide()`, normal vision/goal decisions and floor collisions continue; existing recovery acceleration resumes chasing or wandering afterward. Both normal zombies and runners share this behaviour through `ZombieController`.

Both zombie masters expose `ShoveReactionsEnabled=true`, `ShoveResistance` (0 normal, 0.1 runner), `ShoveFlinchThreshold=0.2`, and `ShoveMaxStaggerSeconds=0.45`. A QuickShoveMaster strength of 1/duration 0.4s yields a 0.4s pause for the normal zombie and 0.36s for runner, provided the reaction gate accepts it. Existing CM1 anti-stun-lock interval/recovery and impact immunity apply to both kinds. Bullet damage, slow and visual flinch are unchanged.

**No new animation:** shove uses the existing standing/chase pose; existing CM1 bullet flinch is skipped for shoves. CM2-B2 adds backwards velocity without a new animation system; visual poses remain deferred to CM2-D. Deactivation, death and spawn reset clear the shove state. No new physics loop, timer, allocation, ray or per-entity idle processing is added.

**Not player-testable yet:** CM2-A detects contacts but player inputs/impact dispatch are not wired until CM2-C. For local checks, temporarily dispatch a no-damage `CombatImpactKind.Shove` via `HitTargetResolver.Dispatch` to a live zombie (use strength=1, duration=0.4s), then remove the debug caller. Verify it stops, falls normally on slopes, recovers/chases, takes no HP damage, respects resistance/disabled settings, rejects repeated quick impacts and does not change bullet staggering. Run `dotnet build war-machine.csproj` and quick top-down/FPS scene checks. **No local Godot/.NET build or visual test performed.**

## CM2-B2 — collision-aware physical shove (2026-10-27)

**Implemented in source; awaiting local build and visual validation.** Accepted `Shove` impacts now apply a horizontal velocity in the sanitized attack direction. `HitReaction.Strength` includes existing shove resistance. `ShovePushSpeed = 4.0` m/s per strength and `ShoveMaxPushSpeed = 6.0` m/s cap determine initial velocity; `ShoveBrakingAcceleration = 9.0` m/s² reduces it each existing active physics tick. All values are exposed on both zombie masters and bounded when consumed. These are starting values, not fixed displacement: physics, resistance, obstacles and reaction duration determine travel.

Existing `CharacterBody3D.MoveAndSlide()` handles collisions, ground and other bodies. No transform teleport, vertical launch, new node/timer, new query or permanent idle processing. The zombie slides while keeping its prior facing and neutral gait; CM1 bullets and their flinch remain unchanged. On expiry the existing recovery acceleration resumes chase/wander; the existing anti-stun-lock, death, deactivate and spawn reset rules still apply.

**CM2-C connects Q shove** in both top-down and FPS; CM2-D view-specific shove animation remains future work.

**Local checks:** `dotnet build war-machine.csproj`, then use a temporary debug sender to dispatch `HitTargetResolver.Dispatch(zombieCollider, player, null, new CombatImpact(player, contactPoint, horizontalDirection, CombatImpactKind.Shove, 1f, 0.4f))`. Confirm both zombie types push away, do not rotate toward backward travel, collide with walls and nearby zombies, preserve falling on slopes and recover pursuit. Check immunity, disabled settings, bullet reactions and deactivation. Build/Godot runtime checks were not performed here.

## CM2-C — live Q shove in overhead and FPS (2026-10-27)

**Implemented in source, awaiting local build/runtime acceptance.** `PLAYER/Equipment/PlayerQuickMelee.cs` caches one player `MeleeAttack` and a duplicated `QuickShoveMaster.tres`. Both existing equipment adapters recognize the same **Q** physical key, separate from left-click firearm shooting. Top-down uses mouse-world XZ aim and the player's upper-body weapon probe; FPS uses the captured centre-screen camera ray (including pitch) and an upper-body contact origin. The shared short-lived CM2-A overlap/occlusion query returns one eligible contact, the helper dispatches zero damage and `Shove` through CM0, and CM2-B2 drives knockback/recovery. Key hold/repeat cannot re-trigger; independent cooldown applies.

Only active attacks wake each adapter's existing physics callback. Weapon switching, lost focus, UI selection, FPS Escape/capture release, and scene exit cancel the attack without resetting cooldown; both views return to existing idle sleep. No second damage/physics system or new animation. CM2-D is deferred.

**Local handoff:** `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn` and `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`. Face a zombie at ~1–1.5m, press Q, verify backward motion and recovery without HP loss. Check walls/doors, elevation, FPS high/low aim misses, top-down horizontal forgiveness, cooldown, one zombie per shove, firearm shots/held fire, switching 1/2, debug UI focus, FPS Escape/alt-tab and idle physics. No .NET/Godot build/runtime was performed here.

## Quick shove tuning — movement-dependent force (2026-10-27)

**Implemented in source, awaiting local gameplay validation.** Q continues to work whether the player is stationary, walking, strafing or running in either camera mode. `PlayerQuickMelee.TryStart()` samples the player's actual XZ velocity **once when Q is pressed**, then applies a linearly interpolated strength multiplier only to the eventual `Shove` impact. It does not modify shove damage (zero HP by default), hit volume, windup, cooldown, stagger duration or rifle/MG hits. Because movement is sampled on attack start, stopping during windup cannot lower the pending shove; moving after initiating a stationary shove does not retroactively increase it. There is no continuous idle momentum polling.

Editable `COMBAT/Configuration/QuickShoveMaster.tres` fields: `StationaryStrengthMultiplier = 0.45`, `RunningStrengthMultiplier = 2.25`, `FullMomentumSpeed = 8.0` m/s; these multiply the existing base `ImpactStrength = 1.0` with linear interpolation between 0 and 8 m/s. With the existing 4 m/s zombie shove scale, the approximate *initial* push velocities for a resistance-free shambler are **1.8 m/s at rest**, **6.3 m/s at 5 m/s walk**, and **9 m/s at 8 m/s run**, before braking/collision. Runners retain 10% shove resistance, reducing their reaction strength and displacement. The per-zombie `ShoveMaxPushSpeed` authoring cap rises from **6 to 10 m/s** in both masters and `ZombieSettings`, allowing stronger running impacts through. These are *initial velocity targets*, not guaranteed travel distances; `ShoveBrakingAcceleration = 9` m/s² remains unchanged.

**Activation / cost:** Work still begins only on Q. One constant-time horizontal speed sample and interpolation per accepted shove, no new timers, collision queries, per-frame loops or allocations. Same cancellation/shutdown and collision-aware `MoveAndSlide()` behaviour as CM2-C/B2.

**Focused acceptance:** Build `dotnet build war-machine.csproj`, then in World and FPS tests press Q while stationary, walking, and genuinely moving at run speed into a nearby zombie. Stationary should barely shift it; walking clearly moves it; running is strongest. Check that Q still works while moving, stronger knocks stop at walls, runner resistance holds, impact leaves HP unchanged and rifle/MG flinch/fire is untouched. Verify moving slowly due to a wall gives proportionately weaker shove despite holding Shift, and timing/cooldown still work. Build/runtime not run in this environment.
