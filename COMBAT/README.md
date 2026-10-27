# COMBAT — cross-actor impacts and reactions

**CM0/CM1 implemented; CM2-B (zombie shove-response only) implemented in source, awaiting local validation. CM2-A/C/D and complete CM2–CM5 are not yet implemented.** There is still no player shove key, melee contact detector, bat, knife, zombie attack or player health.

## One contact, separate damage and impact

Shared/CombatImpact.cs supplies a readonly payload and optional IImpactReceiver. DamagePacket/IDamageReceiver/HealthComponent/DamageResolver remain in ENTITIES/Shared/Combat, unchanged. Do not create a second damage pipeline.

Shared/HitTargetResolver.Dispatch(collider, source, nullableDamage, nullableImpact) walks at most 32 parents, resolves the first supported actor once, excludes self and applies positive damage **before** optional impact. Scenery still blocks the original hitscan; the dispatcher never adds a ray or searches behind cover. A freed/queued actor receives no subsequent impact. Receivers also guard their own dead/inactive state.

For an impact-only contact, pass null damage (or nonpositive damage) plus CombatImpact. No HP event/fake damage is needed. A damage-only contact can omit impact; legacy receivers without IImpactReceiver keep receiving damage. Bullet direction uses source-to-contact projected onto XZ, with a finite forward fallback for coincident/vertical hits. Strength/duration are finite/clamped. Shove now drives physical receiver-side pushback for zombies (CM2-B); Melee/Blast are still reserved for later phases.

## CM1 zombie response

Both slow and runner controllers share reaction logic in ZombieController. Health resolves first; a survivor can accept one Bullet impact. Reactions immediately damp horizontal velocity, then target about 10% normal travel speed for the short duration. Gravity, MoveAndSlide, terrain contact, current goals and existing sight timer continue. The latest AI goal cannot bypass the reaction speed limit. A small additive visual-root recoil overlays the species gait/lean without altering its collider or permanently accumulating rotation. After expiry, movement recovers with configurable acceleration, then normal movement/sleep resumes.

Reactions/HitReaction.cs is a plain value-state helper advanced by the existing active physics callback. An active reaction is never refreshed/extended. Each accepted hit reserves at least duration + MinRecoverySeconds before re-flinch (and respects MinFlinchInterval), with a per-reaction duration cap. This guarantees recovery gaps under fast fire; damage still applies on every accepted shot even when reaction is refused. Immunity/resistance/threshold are target policy. Death/deactivate/reset clear reaction and additive pose; there is no separate reaction node/timer/idle poll.

### Starting tuning

| Profile | Impact strength | Requested stagger |
| --- | --- | --- |
| RifleMaster | 1.0 | 0.20s |
| MachineGunMaster | 0.65 | 0.14s |

Both zombie masters expose BulletReactionsEnabled, ImpactResistance, FlinchThreshold, MaxStaggerSeconds, MinFlinchInterval, MinRecoverySeconds, StaggerMoveMultiplier, ImpactVelocityRetention, StaggerBrakingAcceleration, RecoveryAcceleration, RecoverySeconds and FlinchDegrees. Runner resistance starts at 0.1; normal zombie at 0. Resistance reduces strength and duration, and 1 is immune. Default threshold 0.2, max duration 0.3s, minimum interval 0.45s and recovery gap 0.2s. Bullet rotation amplitude is cosmetic; bullets still cause **no physical push**.

Damage stays rifle 95 / MG 35 against default 100HP: two rifle hits / three MG hits kill. Check the first nonlethal shot to see stagger; the lethal shot still removes the zombie normally. To inspect repeated long bursts locally, temporarily raise test HP and restore it afterward rather than changing production gun damage.

### Validation handoff

Focused C# loop/structure, contact ordering/self/scenery guards, settings bindings and a numeric reaction/cooldown model checked. **Compilation and Godot runtime/visual checks remain unverified: no Godot/.NET executable here.**

Locally run `dotnet build war-machine.csproj`, then F6 World and FPS. Shoot a slow zombie and runner: first rifle hit flinches/slows and chase resumes, second kills once. Check MG bursts, stationary target, moving/turning target, cover/misses, immunity/disabled reaction and scene unload. Exercise Dispatch with null damage + Bullet impact: HP unchanged, eligible actor reacts; Shove does not yet move a zombie. Confirm inactive/dead actors ignore impacts, horde work stays bounded, settled actors sleep, normal animations/vision/terrain/audio/muzzle lights still work. Existing physics queries are unchanged. Exact recoil appearance and balance need owner acceptance.

No combat singleton, actor registry, scene scans, added hit rays, status framework or per-hit heap allocation. New value payloads and cached actor state have zero idle callbacks. Existing ground-recovery and sensing paths are retained.

## CM2-B — zombie shove reception (2026-10-27)

**Implemented in source; awaiting local build/runtime acceptance.** The existing `IImpactReceiver` on `ZombieController` now recognizes `CombatImpactKind.Shove` as well as Bullet. Accepted shoves give a finite horizontal speed from the existing contact's direction, decelerate over the existing stagger window, then return to the current pursuit/wander goal using existing recovery acceleration. The existing `CharacterBody3D.MoveAndSlide()` is the only movement/collision path, so walls, terrain and actors still obstruct sliding; no transform teleport, vertical impulse, physics body or new tick loop. During knockback the visual body stays facing its original direction rather than turning to face backward travel. Zombie_002 inherits the same code and can use a different shove resistance.

Editable fields under **Shove reaction** in both zombie masters: `ShoveReactionsEnabled` (true), `ShoveResistance` (0 normal / 0.1 runner), `ShoveStrengthThreshold` (0.2), `ShoveMaxStaggerSeconds` (0.45s), `ShovePushSpeed` (4 m/s for an accepted strength of 1 before resistance), `ShoveMaxPushSpeed` (6 m/s cap), and `ShoveBrakingAcceleration` (9 m/s²). Existing `MinFlinchInterval`, `MinRecoverySeconds`, `RecoverySeconds`, `RecoveryAcceleration` and `FlinchDegrees` are reused; the same reaction gate prevents refreshing/stun-locking, including across bullet/shove kinds. These are initial feel-tuning values, **not measured displacement**.

**Activation:** Only an accepted impact starts knockback and wakes already-available physics. **Shutdown:** normal reaction expiry, death, deactivation and scene unload clear shove state; the existing brain/movement sleep rules apply. **Idle overhead:** zero added callbacks, timers, per-frame scans, collision queries or allocations. Incoming `Melee` and `Blast` still do nothing until separately authorized. Bullet behaviour and damage are unchanged.

**Important phase boundary:** This is **recipient behaviour only**. No new hitbox, player `V` input, FPS/top-down adapters, shove animation or melee profile exists yet; those remain CM2-A/C/D. To test before those are added, use a temporary caller (not committed) to dispatch `HitTargetResolver.Dispatch(zombieCollider, player, null, new CombatImpact(player, contactPosition, playerToZombieXZ, CombatImpactKind.Shove, 1f, 0.4f))`, then remove it.

**Focused local handoff:** `dotnet build war-machine.csproj` then F6 World and FPS. Inject one temporary zero-damage Shove impact into a live shambler and runner on flat terrain, beside a solid wall and at a modest slope; verify push moves away ~short distance but never passes through blockers, does not decrease HP, maintains facing, and chase resumes. Repeat while stationary, after bullets, with resistance=1, disabled shoves, death/unload and multiple attackers. Check existing rifle/MG stagger unchanged, idle zombies still sleep. Compilation/visual physics not run here; leave as **awaiting local validation**.
