# COMBAT — cross-actor impacts and reactions

**CM0/CM1, CM2-A and CM2-B1 implemented in source, awaiting local validation. CM2-B2/C/D and full CM2–CM5 remain planned.** No player shove input, zombie physical knockback, bat, knife, zombie attack or player health yet.

## One contact, separate damage and impact

Shared/CombatImpact.cs supplies a readonly payload and optional IImpactReceiver. DamagePacket/IDamageReceiver/HealthComponent/DamageResolver remain in ENTITIES/Shared/Combat, unchanged. Do not create a second damage pipeline.

Shared/HitTargetResolver.Dispatch(collider, source, nullableDamage, nullableImpact) walks at most 32 parents, resolves the first supported actor once, excludes self and applies positive damage **before** optional impact. Scenery still blocks the original hitscan; the dispatcher never adds a ray or searches behind cover. A freed/queued actor receives no subsequent impact. Receivers also guard their own dead/inactive state.

For an impact-only contact, pass null damage (or nonpositive damage) plus CombatImpact. No HP event/fake damage is needed. A damage-only contact can omit impact; legacy receivers without IImpactReceiver keep receiving damage. Bullet direction uses source-to-contact projected onto XZ, with a finite forward fallback for coincident/vertical hits. Strength/duration are finite/clamped. Zombies now respond to Bullet and Shove. Shove briefly stops horizontal movement (CM2-B1); Melee/Blast remain reserved and physical knockback awaits CM2-B2.

## CM1 zombie response

Both slow and runner controllers share reaction logic in ZombieController. Health resolves first; a survivor can accept one Bullet impact. Reactions immediately damp horizontal velocity, then target about 10% normal travel speed for the short duration. Gravity, MoveAndSlide, terrain contact, current goals and existing sight timer continue. The latest AI goal cannot bypass the reaction speed limit. A small additive visual-root recoil overlays the species gait/lean without altering its collider or permanently accumulating rotation. After expiry, movement recovers with configurable acceleration, then normal movement/sleep resumes.

Reactions/HitReaction.cs is a plain value-state helper advanced by the existing active physics callback. An active reaction is never refreshed/extended. Each accepted hit reserves at least duration + MinRecoverySeconds before re-flinch (and respects MinFlinchInterval), with a per-reaction duration cap. This guarantees recovery gaps under fast fire; damage still applies on every accepted shot even when reaction is refused. Immunity/resistance/threshold are target policy. Death/deactivate/reset clear reaction and additive pose; there is no separate reaction node/timer/idle poll.

### Starting tuning

| Profile | Impact strength | Requested stagger |
| --- | --- | --- |
| RifleMaster | 1.0 | 0.20s |
| MachineGunMaster | 0.65 | 0.14s |

Both zombie masters expose BulletReactionsEnabled, ImpactResistance, FlinchThreshold, MaxStaggerSeconds, MinFlinchInterval, MinRecoverySeconds, StaggerMoveMultiplier, ImpactVelocityRetention, StaggerBrakingAcceleration, RecoveryAcceleration, RecoverySeconds and FlinchDegrees. Runner resistance starts at 0.1; normal zombie at 0. Resistance reduces strength and duration, and 1 is immune. Default threshold 0.2, max duration 0.3s, minimum interval 0.45s and recovery gap 0.2s. Rotation amplitude is cosmetic; **no physical push** occurs yet.

Damage stays rifle 95 / MG 35 against default 100HP: two rifle hits / three MG hits kill. Check the first nonlethal shot to see stagger; the lethal shot still removes the zombie normally. To inspect repeated long bursts locally, temporarily raise test HP and restore it afterward rather than changing production gun damage.

### Validation handoff

Focused C# loop/structure, contact ordering/self/scenery guards, settings bindings and a numeric reaction/cooldown model checked. **Compilation and Godot runtime/visual checks remain unverified: no Godot/.NET executable here.**

Locally run `dotnet build war-machine.csproj`, then F6 World and FPS. Shoot a slow zombie and runner: first rifle hit flinches/slows and chase resumes, second kills once. Check MG bursts, stationary target, moving/turning target, cover/misses, immunity/disabled reaction and scene unload. Exercise Dispatch with null damage + Bullet impact: HP unchanged, eligible actor reacts; Shove does not yet move a zombie. Confirm inactive/dead actors ignore impacts, horde work stays bounded, settled actors sleep, normal animations/vision/terrain/audio/muzzle lights still work. Existing physics queries are unchanged. Exact recoil appearance and balance need owner acceptance.

No combat singleton, actor registry, scene scans, added hit rays, status framework or per-hit heap allocation. New value payloads and cached actor state have zero idle callbacks. Existing ground-recovery and sensing paths are retained.

## CM2-A — melee detection foundation (2026-10-27)

**Implemented in source; awaiting local build/runtime validation.** `Melee/MeleeSettings.cs`, `Melee/MeleeAttack.cs`, `Melee/README.md` and `Configuration/QuickShoveMaster.tres` provide one-target active-window detection, configurable range/arc/height/timing, obstacle checks and both overhead-horizontal/FPS-pitch aim modes, without requiring input or animation. `MeleeAttack` is instantiated by the future caller and only queried while an attack is active. It returns eligible contact data; CM2-C will dispatch it through the existing CM0 resolver. CM2-B1 recipient stagger is implemented, while CM2-B2 physical knockback remains unimplemented.

The premature CM2-B code/settings from the previous commit were reverted following the phase-letter correction; both zombie types retain CM1 bullet behaviour. No Godot/.NET build or runtime validation was performed; see `Melee/README.md` for test handoff.

## CM2-B1 — simple zombie shove pause (2026-10-27)

**Implemented in source, awaiting local validation.** Zombies now accept `CombatImpactKind.Shove` through their existing `IImpactReceiver`. A valid zero-damage shove stops XZ velocity immediately and keeps horizontal speed zero throughout the current `HitReaction` window. Gravity, `MoveAndSlide()`, normal vision/goal decisions and floor collisions continue; existing recovery acceleration resumes chasing or wandering afterward. Both normal zombies and runners share this behaviour through `ZombieController`.

Both zombie masters expose `ShoveReactionsEnabled=true`, `ShoveResistance` (0 normal, 0.1 runner), `ShoveFlinchThreshold=0.2`, and `ShoveMaxStaggerSeconds=0.45`. A QuickShoveMaster strength of 1/duration 0.4s yields a 0.4s pause for the normal zombie and 0.36s for runner, provided the reaction gate accepts it. Existing CM1 anti-stun-lock interval/recovery and impact immunity apply to both kinds. Bullet damage, slow and visual flinch are unchanged.

**No new animation:** shove uses the existing standing/chase pose; existing CM1 bullet flinch is skipped for shoves. There is no backward velocity, displacement effect or additional animation system until CM2-B2/D. Deactivation, death and spawn reset clear the shove state. No new physics loop, timer, allocation, ray or per-entity idle processing is added.

**Not player-testable yet:** CM2-A detects contacts but player inputs/impact dispatch are not wired until CM2-C. For local checks, temporarily dispatch a no-damage `CombatImpactKind.Shove` via `HitTargetResolver.Dispatch` to a live zombie (use strength=1, duration=0.4s), then remove the debug caller. Verify it stops, falls normally on slopes, recovers/chases, takes no HP damage, respects resistance/disabled settings, rejects repeated quick impacts and does not change bullet staggering. Run `dotnet build war-machine.csproj` and quick top-down/FPS scene checks. **No local Godot/.NET build or visual test performed.**
