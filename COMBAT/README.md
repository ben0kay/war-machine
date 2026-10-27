# COMBAT — cross-actor impacts and reactions

**CM0/CM1 implemented; CM2-A melee detection/profile implemented in source, awaiting local validation. CM2-B/C/D and full CM2–CM5 remain planned.** No live player shove, zombie physical knockback, bat, knife, zombie attack or player health yet.

## One contact, separate damage and impact

Shared/CombatImpact.cs supplies a readonly payload and optional IImpactReceiver. DamagePacket/IDamageReceiver/HealthComponent/DamageResolver remain in ENTITIES/Shared/Combat, unchanged. Do not create a second damage pipeline.

Shared/HitTargetResolver.Dispatch(collider, source, nullableDamage, nullableImpact) walks at most 32 parents, resolves the first supported actor once, excludes self and applies positive damage **before** optional impact. Scenery still blocks the original hitscan; the dispatcher never adds a ray or searches behind cover. A freed/queued actor receives no subsequent impact. Receivers also guard their own dead/inactive state.

For an impact-only contact, pass null damage (or nonpositive damage) plus CombatImpact. No HP event/fake damage is needed. A damage-only contact can omit impact; legacy receivers without IImpactReceiver keep receiving damage. Bullet direction uses source-to-contact projected onto XZ, with a finite forward fallback for coincident/vertical hits. Strength/duration are finite/clamped. Shove/Melee/Blast remain payload values only. Zombies still respond to Bullet, pending CM2-B.

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

**Implemented in source; awaiting local build/runtime validation.** `Melee/MeleeSettings.cs`, `Melee/MeleeAttack.cs`, `Melee/README.md` and `Configuration/QuickShoveMaster.tres` provide one-target active-window detection, configurable range/arc/height/timing, obstacle checks and both overhead-horizontal/FPS-pitch aim modes, without requiring input or animation. `MeleeAttack` is instantiated by the future caller and only queried while an attack is active. It returns eligible contact data; CM2-C will dispatch it through the existing CM0 resolver. CM2-B victim knockback remains unimplemented.

The premature CM2-B code/settings from the previous commit were reverted following the phase-letter correction; both zombie types retain CM1 bullet behaviour. No Godot/.NET build or runtime validation was performed; see `Melee/README.md` for test handoff.
