> **ELD2 source implementation (2026-10-28), awaiting local validation:** [Injury locomotion and World/FPS acceptance](../../TYPES/Zombies/LOCOMOTION_README.md). Both variants share limp, one-leg shuffle, low living crawl and immobilization. Debug panel buttons demonstrate injuries without HP changes. Existing attacks/ELD3 and ZD1–ZD3 remain deferred. This supersedes older ELD2-Planned/upright-legless descriptions below.

> **CM3 bat source implementation (2026-10-28), awaiting local validation:** [Slot 3 / LMB horizontal multi-target bat](../../../EQUIPMENT/Melee/BaseballBat_001/README.md). Damage 11, cap 3, mild melee flinch, ELD-aware region damage, SeverPower=0; Q remains separate. This supersedes older firearm-only/two-slot/CM3-Planned descriptions. No knife, inventory or outgoing zombie attacks.

# ELD1 — limb condition and severing

**Implemented in source; awaiting local compilation and World/FPS acceptance (2026-10-28).** ELD0 has owner-provided FPS evidence for Head, Torso and RightLeg hits. This is not full ELD0 regression acceptance. ELD2–ELD5 remain Planned. The ELD0 notes below are historical where they say there is no condition/severing yet.

`AnatomyComponent` is an optional plain C# helper with six flat condition/stress/state entries. It has no nodes, physics, timer or health authority. `Functional → Damaged → Disabled → Severed` is monotonic for limbs; head/torso can be damaged but are never severed or independently mortal. Zero head/torso condition still relies on existing overall health for death. Unknown hits affect ordinary vitality only. `StateChanged` fires only on a state transition; consumers can read condition and capture an immutable packed state snapshot. No player-state migration: existing player body condition/healing remain unchanged.

Zombie coordination resolves armour/resistance **once**, commits anatomy/visual events, then calls `HealthComponent.ApplyResolvedDamage` once with the same result. Its reentrancy guard spans the entire transaction. Existing 100 HP, rifle 95 and machine-gun 25 damage are preserved. Region damage currently contributes 1:1 to vitality; no survival extension, multipliers or new critical rules are enabled.

## Tuning and expected behaviour

Both zombie masters reference `ENTITIES/TYPES/Zombies/Configuration/ZombieAnatomyMaster.tres`, a species profile duplicated for runtime state. They can select different profiles later. Default region condition=80; Damaged at <=75%, Disabled at <=25%. Severing requires exhausted condition, a positive current hit SeverPower and accumulated resolved damage × SeverPower >=65×SeverResistance. These deterministic thresholds implement the first pass; no random selection/chance yet. A legacy weapon with SeverPower=0 can damage/disable but never sever. Mitigated/zero damage cannot injure or add sever stress.

RifleMaster SeverPower=1; MachineGunMaster=0.65. An unarmoured fresh zombie's rifle limb hit therefore severs and leaves 5 HP. With MG hits all on the same fresh limb: hit 1 damages (55 condition), hit 3 disables (5 condition), hit 4 severs **and kills**. Hits elsewhere do not build that limb's stress. These are initial editable anatomy settings, not changes to gun HP damage. With different armour/resistance or anatomy profiles the results change. Weak hits cannot remove a fresh healthy limb; repeated same-limb hits accumulate.

`ZombieAnatomy` caches the four limb mesh groups and adds four hidden shared-mesh stump caps at their pivots. On sever it hides that limb's geometry and reveals its stump. Existing joints remain valid for both procedural Pose implementations. Remaining clothing/material variation is preserved. All corresponding anatomical shape owners disable, including thigh AND shin for a leg. Hidden limb shapes never return through SyncPose. Reset/reuse restores visuals, shapes and flat injury state explicitly.

The lethal hit commits severing before Died. ZombieCorpsePose captures the immutable final injury state; the actor reapplies final visibility after Deactivate/Pose(0), and transfers the original visual hierarchy with its hidden parts/stumps. Shared CorpseManager/CorpseBody are unchanged and know no limb names. No detached limb/debris bodies, extra corpse framework, corpse aging or piles are introduced.

**A legless living zombie still uses the existing upright movement/capsule and gait until ELD2.** Disabled limbs currently expose state only. Attacks/capability filtering wait for CM5/ELD3. This phase does not claim realistic living legless movement or remove the movement capsule; only precise anatomical query shapes are disabled. That broad capsule also remains the existing AIM1/melee target.

## Test in the existing worlds

Build `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`. No new test scenes or keybindings.

1. Shoot one fresh zombie arm or leg with the rifle: the correct limb disappears, stump remains, and `[ELD1] ... state=Severed` appears. Remaining body continues existing AI at 5 HP. Kill it with a subsequent shot: missing limb remains absent during fall and rest.
2. On another zombie, place four MG rounds into one limb: inspect Damaged, Disabled and then lethal Severed. Ensure the corpse keeps the final sever rather than restoring geometry. Use short taps; recoil can move later bullets to another region.
3. Repeat on standard and runner variants and both left/right limbs, with movement and stagger. Visible Collision Shapes should show no anatomical boxes on the removed limb (the intentional movement capsule remains). Aim through the newly empty gap: no phantom limb hit; torso/world behind can be hit normally.
4. Shoot head/torso and behind walls/closed rear door: existing damage and cover remain. Check Q shove, reload, player body HUD, ordinary targets and no duplicate death/corpse. Restart World/FPS and inspect for errors/orphans. To exercise multiple surviving amputations, temporarily increase the zombie HealthProfile MaxHealth locally, then restore it; no permanent balance changes are necessary.

Godot/.NET are unavailable here. Only focused source/resource/whitespace checks were performed; compilation, same-frame shape disabling, runtime visual/corpse ordering and performance remain unverified. Please return any build/runtime error before proceeding to ELD2.

## Performance handoff

Wake: accepted positive resolved hits. State events update cached visuals/shapes only on transitions. No new per-frame callbacks, queries, timers or limb rigid bodies. Existing ELD0 active pose sync remains eight owners; invisible meshes still retain lightweight rig pivots for safe pose reuse. Each actor adds six state entries and four dormant stump mesh instances sharing mesh/material; stumps render only when severed. State dies with the actor; the transferred visual and immutable snapshot follow the existing corpse lifetime. No detached debris cost or cleanup manager.

---

# Shared anatomy — ELD0

ELD0 is implemented in source, awaiting local build and in-world validation. ELD1–ELD5 remain Planned. [Master direction](../../TYPES/Zombies/ANATOMY_COMBAT_CORPSE_PLAN.md).

`BodyRegion` is the original player enum moved here (Unknown=-1, Head=0 through RightLeg=5). Existing player region storage, critical vitality policy, HUD and healing are unchanged. DamagePacket adds optional Region=Unknown; old constructors and IDamageReceiver remain compatible.

`AnatomyHitboxes` is one non-monitoring/non-monitorable Area3D per participating actor, with native shape owners rather than per-limb nodes/scripts. Standard and runner zombies build eight boxes from cached mesh bounds: head, torso, each arm, each thigh, each knee/shin. Both leg segments map to the same leg region. These are conservative box approximations of the procedural artwork, not triangle-accurate skin collision. Labels are the actor's own left/right. Shapes follow the existing physics gait and reaction pose; stationary actors add no polling. The constructor scans art once; no per-shot scene searches.

The living movement capsule remains unchanged for player movement, zombie collisions, melee/shove, sight and cursor AIM1 targeting. Firearm queries exclude registered anatomy capsules and include their region areas on the same actor layer. The exclusion cache refreshes only on registration/version changes or equip. Deer/robot/player capsules are not registered and remain ordinary targets. This adds no raycasts: existing shoulder-to-muzzle cover probe and ballistic query remain. Cover layers, damage/armour, ammo, spread and recoil are unchanged. Precise firearm rays can now miss the gaps outside the artwork boxes even when a capsule would have been hit. Cursor targeting still uses the broad capsule; it does not guarantee a ballistic hit.

Region is read from the actual ray shape index by HitTargetResolver before its existing single receiver dispatch. ZombieController records LastHitRegion and AnatomicalHitCount, then calls existing health once with unmodified damage. No region multipliers, condition loss, severing, limb penalties, attacks, random region substitution or player precision hurtboxes are implemented. Existing melee remains Unknown. Region areas disable on death and stay under the freed actor, never transfer with corpse visuals. Registry cleanup runs on exit; re-entry restores registration.

## Configuration and World acceptance

`Configuration/AnatomyMaster.tres` is duplicated per actor. Padding=0.005m expands fitted boxes slightly. LogRegionHits=true emits `[ELD0] ... region=LeftArm incoming=... point=...` on actual anatomical damage dispatch. Set false to disable logs (especially for profiling). Unknown hits do not print. No standalone test scenes were added.

Run `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`. Shoot both zombie variants at head, torso and each limb; inspect logs/collision shapes while they move, turn and react. An arm directly in front of the torso should receive the first hit. Verify walls and closed rear door stop bullets, gaps can miss, Q shove still works, health/death remains unchanged, corpses have no living hit area, and deer/robot/player systems remain functional. Reload both scenes repeatedly and check for stale RID/errors. Use existing world zombies, not a new fixture.

Source/resource/whitespace checks only here: Godot/.NET are unavailable, so compilation, physics-server pose timing, actual region hits, cover, both views and performance are unverified. Native area shapes may have normal physics broadphase cost while moving; no measured performance claim. No new engine frame callbacks or timers, no per-limb scripts/rigid bodies. Active zombies update eight cached transforms once per physics tick; dormant shapes stay queryable without polling; unload releases area and registry entries.
