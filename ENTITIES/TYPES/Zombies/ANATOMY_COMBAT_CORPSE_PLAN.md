> **ELD1 source implementation (2026-10-28), awaiting local validation:** [Limb condition, deterministic severing and injury-preserving ZD0 handoff](../../Shared/Anatomy/README.md). Original vitality/weapon HP damage is unchanged. ELD2 locomotion, ELD3/CM5 attacks, ELD4 debris and ZD1–ZD3 remain deferred. Existing World/FPS scenes are the acceptance venue; no new test scenes. This supersedes older ELD1-Planned/no-severing statements below.

> **ELD0 implemented in source (2026-10-28), awaiting local validation.** [Implementation, configuration and World acceptance](../../Shared/Anatomy/README.md). Both zombie variants use precise firearm region shapes; current damage remains unchanged. ELD1–ELD5 remain Planned. Owner prefers testing in World/FPS; no dedicated anatomy test scenes required. Top-down probability remains a separately tunable future option, not part of ELD0. This status supersedes the original all-Planned wording below.

# War Machine — Unified Entity Combat, Anatomy, Locomotion and Corpse Architecture

**Status:** Approved overall planning direction (2026-10-28). ELD0–ELD5 remain **Planned**; no production implementation authorized by this document. Existing PH0/PH1 and ZD0 remain source implemented, awaiting local validation.

**Repository:** `ben0kay/war-machine`

**Branch:** `implement-td0-td1`

## 1. Overall design

Build one reusable entity damage and anatomy foundation. Species-specific controllers determine how an injury affects movement, attacks and appearance. The existing corpse system takes over upon death.

**Ownership boundaries:**

- **Combat:** Determines hits, damage, weapon strength and impact.
- **Anatomy:** Tracks body-region condition, missing limbs and capability changes.
- **Locomotion:** Selects walking, limping, hopping, crawling or immobilized movement.
- **Attacks:** Determines which physical attacks remain available.
- **Visuals:** Hide or detach limbs, select poses and display wounds.
- **Corpses:** Handle death, falling, piling, aging, sinking and removal.

The system must distinguish **alive but severely injured** from **dead**.

## 2. Proposed folder hierarchy

Existing files should remain in their current locations unless a narrowly scoped change genuinely requires relocation.

`[EXISTING]` means implemented code. `[NEW]` means proposed.

```text
COMBAT/
  Shared/
    CombatImpact.cs             [EXISTING]
    HitTargetResolver.cs        [EXISTING]
    README.md

  Melee/
    MeleeAttack.cs              [EXISTING]
    MeleeSettings.cs            [EXISTING]
    README.md

ENTITIES/
  Shared/
    Combat/
      DamagePacket.cs           [EXISTING]
      DamageResolver.cs         [EXISTING]
      HealthComponent.cs        [EXISTING]
      HealthSettings.cs         [EXISTING]
      IDamageReceiver.cs        [EXISTING]

    Anatomy/
      BodyRegion.cs             [EXISTING, MOVED IN ELD0]
      AnatomyComponent.cs       [NEW]
      AnatomyHitboxes.cs        [ELD0 SOURCE IMPLEMENTED]
      AnatomyDamage.cs          [NEW]
      IAnatomyVisualAdapter.cs  [NEW]
      Configuration/
        AnatomySettings.cs      [NEW]
      README.md                 [NEW]

    Corpses/
      CorpseManager.cs          [EXISTING]
      CorpseBody.cs             [EXISTING]
      ICorpsePoseAdapter.cs     [EXISTING]
      CorpseSlowArea.cs         [ZD1 PROPOSED]
      DetachedPartManager.cs    [LATER]
      Configuration/
        CorpseSettings.cs       [EXISTING]
        CorpseMaster.tres       [EXISTING]
      README.md

  TYPES/
    Zombies/
      ZombieController.cs       [EXISTING]
      ZombieSettings.cs         [EXISTING]
      ZombieAnatomy.cs          [NEW]
      ZombieLocomotion.cs       [NEW]
      ZombieAttacks.cs          [LATER / CM5]
      ZombieCorpsePose.cs       [EXISTING, EXTEND]

      C/
        ZombieMeshBuilder.cs    [EXISTING, EXTEND]
        ZombieHumanGeometry.cs  [EXISTING]

      Configuration/
        ZombieMaster.tres       [EXISTING, EXTEND]

      Zombie_002/
        ZombieRunnerController.cs       [EXISTING]
        C/ZombieRunnerMeshBuilder.cs    [EXISTING, EXTEND]
        Configuration/
          ZombieRunnerMaster.tres       [EXISTING]

      ANATOMY_COMBAT_CORPSE_PLAN.md     [NEW]

    Wildlife/
      Deer/                      [EXISTING]
        # Future optional anatomy adapter

      Bear/                      [FUTURE]
        # Future optional anatomy adapter

    Humans/                      [FUTURE]
      # Potential reuse of biped anatomy/locomotion
```

The existing `PLAYER/Stats/BodyRegion.cs` enum already defines the six regions. When ELD0 is implemented, move that **same enum** to the shared Anatomy folder without changing its type name or numeric values. Update its owners and metadata as required. Do not introduce a competing region enumeration.

Do not create empty future species folders or implement hypothetical systems before they are needed.

## 3. Shared anatomy rules

Each supported entity has one overall `HealthComponent` and an optional `AnatomyComponent`.

`AnatomyComponent` tracks six regions:

- Head
- Torso
- LeftArm
- RightArm
- LeftLeg
- RightLeg

Each limb has condition and state: `Functional`, `Damaged`, `Disabled`, or `Severed`. Head and torso have separate critical-damage rules.

One overall entity-health component remains authoritative for death. Six body-region conditions must **not** become six independently mortal HealthComponents.

Anatomy may publish state-change events such as `LimbDamaged`, `LimbDisabled` and `LimbSevered`. Other systems subscribe to these events rather than repeatedly polling anatomy.

Species with different anatomies can provide different region definitions later, without introducing zombie logic into shared combat or corpse code.

## 4. Combat integration

Use the existing `FirearmController`, `HitTargetResolver`, `DamagePacket`, `DamageResolver` and `HealthComponent`.

For anatomically accurate hits, add a dedicated group of nonblocking region hurtboxes following the animated visual joints. Prefer one lightweight `Area3D` with multiple region-labelled shapes per actor, rather than six independent monitoring systems.

The gameplay movement capsule must remain separate from region detection.

A physical firearm shot must still respect walls, doors and other solid cover. Any new hurtbox collision mask or area-ray configuration must be tested to ensure actor movement capsules do not intercept all region hits before they reach the anatomy shapes.

On a successful hit:

1. Resolve the actual struck entity and anatomical region.
2. Read the weapon's normal damage and optional severing power.
3. Resolve armour/resistance exactly once through the existing damage pipeline.
4. Apply the resulting region-condition change.
5. Determine whether a limb becomes damaged, disabled or severed.
6. Apply the configured contribution to overall vitality without double damage.
7. If still alive, notify locomotion, attacks and visual adapters.
8. If killed, hand the final injury state and visual hierarchy to the existing corpse system.

Preserve the original `IDamageReceiver` contract for entities without anatomy support. Avoid new duplicate firearm raycasts or separate damage pipelines.

The death-triggering shot must be capable of severing a limb and preserving that missing limb in the resulting corpse.

### Weapon severing power

Introduce a separate editable `SeverPower` or equivalent field for weapons that support dismemberment.

A limb's severing probability should depend on:

- Weapon severing power, independent of basic HP damage.
- Actual anatomical region struck.
- Accumulated limb condition damage.
- Species-specific resistance.
- A configurable severing threshold and probability.

An ordinary weak hit should not reliably remove a healthy limb. Powerful weapons and repeated hits should increase the chance.

Separate overall vitality damage from limb-condition damage so a zombie can survive long enough to demonstrate injury behaviours. Preserve existing weapon balance until the owner explicitly approves changes.

## 5. Zombie locomotion integration

`ZombieController` continues to own the shared zombie AI, target selection, perception and existing movement lifecycle.

`ZombieLocomotion` should determine the available mobility mode from anatomy state without duplicating pursuit or investigation logic.

| Anatomy | Locomotion mode |
|---|---|
| Both legs functional | Walk or run |
| One leg badly damaged | Limp |
| One leg severed | Hop / supported shuffle |
| Both legs severed, at least one functional arm | Crawl |
| Both legs severed, both arms disabled/severed | Immobilized |

**Important:** Crawling remains a living actor state. Do not use `CorpseBody` or activate death ragdoll physics for a living crawler.

The crawling actor needs an appropriate lower body collision shape and movement pose, ground clearance, and lower perception/attack origin where needed. Shape transitions must not overlap terrain, walls or other actors.

The existing AI still chooses destinations. Locomotion only changes how the zombie moves toward them, including speed, acceleration, gait and clearance constraints.

Support both standard zombies and ZombieRunner002 using the same state rules, with different movement tuning.

## 6. Zombie attack integration — CM5

When CM5 becomes active, reuse the existing shared `COMBAT/Melee` executor and profiles. Do not create a zombie-only damage or hit-detection system.

Define separate attacks:

| Attack | Suggested reach | Requirement |
|---|---|---|
| Left claw | 1.4m | Functional left arm |
| Right claw | 1.4m | Functional right arm |
| Bite | 0.65m | Functional head |
| Crawling claw/grab | 0.9m | Crawling and functional arm |

These are initial gameplay tuning suggestions, not finalized balance values.

The anatomy state determines which attacks are available.

One missing arm removes that arm's claw attack. Both missing arms remove claw attacks, but bite remains possible.

Both missing legs permit crawling attacks if functional arms remain.

An immobilized zombie can still bite if an eligible target comes into its short attack range.

CM5 remains responsible for attack scheduling, windup, cooldown, reach validation and actual player damage. Anatomy only supplies capabilities and constraints.

Do not implement CM5 in an anatomy phase unless separately authorized. CM5 also retains its existing dependency on player health.

## 7. Visual severing integration

Both procedural zombie builders already provide named arm, leg and knee pivots. Reuse these rather than constructing an entirely new skeletal rig.

Each visual adapter should support:

- Disabling a severed limb's visible geometry.
- Preserving clothing/material variation on the remaining body.
- Displaying a minimal stump or damaged attachment where appropriate.
- Optional spawning of a detached limb as short-lived physics debris.
- Selecting compatible standing, hopping and crawling poses.
- Persisting the missing-limb appearance into death.

A removed limb must not spontaneously reappear when the existing `Pose()` method runs again.

Avoid permanently simulating independent physics bodies for every attached limb. Detached physics objects should be time-limited, pooled or managed under a shared budget when implemented.

Visual removal must agree with gameplay: a missing arm must not have an active claw attack or active anatomical hurtbox.

## 8. ZD0–ZD3 corpse integration

**Retain the existing corpse implementation. Do not introduce a second ragdoll/corpse framework.**

### Living zombie

While alive, the zombie remains a `CharacterBody3D` using its existing AI/locomotion lifecycle. Its anatomy can change, including severing limbs, without triggering corpse ownership.

### Death handoff

Upon actual death:

1. Latch death once through the existing HealthComponent/ZombieController path.
2. Capture an immutable injury snapshot: severed regions, visual state and final hit direction.
3. Stop living AI, attacks and movement.
4. Transfer the final visual hierarchy to `CorpseManager` through the existing `ICorpsePoseAdapter` boundary.
5. Preserve missing limbs throughout the fall and settled pose.
6. Release the old living actor without an invisible upright collision capsule.

Ensure the existing pose-reset/deactivation ordering cannot restore missing limbs or erase the final injury state.

### ZD phases

- **ZD0 (existing):** One-body physics-assisted fall. Extend the zombie pose adapter to respect the final anatomy snapshot.
- **ZD1 (planned):** Corpse piles and mild movement slowdown. Missing limbs must not create phantom solid obstacles.
- **ZD2 (planned):** Five-minute corpse aging, sinking and removal. Detached pieces should follow their own bounded cleanup policy.
- **ZD3 (planned):** Streaming/crowd/physics budgets and performance acceptance.

A corpse missing two legs should still be visibly missing those legs when it sinks into the terrain.

`CorpseManager` and `CorpseBody` must not depend on specific zombie joints, limb names, locomotion states or weapon types.

## 9. Implementation order

| Phase | Deliverable | Dependencies |
|---|---|---|
| **ELD0** | Shared anatomical region detection and damage routing | Existing combat pipeline |
| **ELD1** | Limb condition, sever thresholds, visual removal and mandatory missing-limb death preservation — **Implemented; Awaiting Validation** | ELD0 and existing ZD0 |
| **ELD2** | Limp, hop, crawl and immobilized locomotion | ELD1 |
| **ELD3** | Claw/bite attack capability filtering | ELD1 and CM5 |
| **ELD4** | Optional bounded detached debris and additional corpse presentation polish | ELD1 and ZD0 |
| **ELD5** | Balance, performance, debug tools and all-variant regression | Prior ELD phases |

**Recommended implementation sequence:** ELD0 → ELD1 (including lethal-hit corpse preservation) → ELD2, then optional ELD4. ELD3 waits for separately authorized CM5. Apply focused ELD5 checks in each phase, then final regression. All ELD phases are Planned. Codex / ChatGPT Work is recommended for these multi-file integrations; Regular Chat for planning/review, and Copilot for local build/runtime diagnostics.

ZD1–ZD3 remain separate work and do not block the initial anatomy system. Do not rewrite the existing ZD0 collapse just to implement locomotion.

## 10. Performance and architecture requirements

- One authoritative combat damage route.
- One optional anatomy component per participating entity.
- One authoritative overall-health component.
- No always-active per-limb scripts, raycasts or rigid bodies.
- Anatomical detection on actual attack queries, not periodic scans.
- Cache body-region shapes and visual references during creation.
- Anatomy state changes are event-driven.
- Update movement and attack capability only when a limb state changes.
- Keep existing low-rate zombie AI timers and movement sleep behaviour.
- No permanent physics simulation for detached limbs or settled corpses.
- Preserve streaming/unload cleanup and current collision masks except narrowly required hurtbox additions.
- Use species-specific `.tres` settings for tuning, duplicated at runtime.
- Keep procedural mesh creation in `C/`; gameplay components belong in their owning feature folder.

## 11. Validation examples

Test both zombie variants in top-down and FPS.

- Shoot a left arm: only its condition changes.
- Sever left arm: the correct arm disappears, no longer receives hits and cannot claw.
- Sever right leg: zombie attempts supported one-leg movement rather than normal running.
- Sever both legs: zombie transitions to a low crawler without dying.
- Sever both arms while crawling: no arm-assisted crawling; switch to immobilized behaviour.
- A zombie without functional arms can still bite if alive and in range.
- Kill a partially dismembered zombie: its corpse falls and settles with the same missing parts.
- Shoot behind a solid wall or closed door: real cover still blocks damage.
- Kill a group of zombies: no lingering hurtboxes, invisible colliders, excessive physics bodies or repeated damage.
- Existing deer, player health, robot, gun reactions and ordinary zombies remain functional.

## 12. Plan authority and linked owners

This document is the overall direction for **entity anatomy, injury-driven zombie locomotion/attack capabilities and injury-preserving corpse handoff**. It does not supersede unrelated world streaming, player survival, weapon balance or robot roadmaps. Older conflicting proposals in the documents below defer to these ownership boundaries; their implementation status and phase authorization gates remain intact.

- [Combat master plan](../../../COMBAT/IMPLEMENTATION_PLAN.md): owns damage/impact delivery, shared melee and CM5 scheduling. Anatomy supplies region context and capabilities.
- [Player health/body-condition plan](../../../PLAYER/IMPLEMENTATION_PLAN.md) and [body-condition design](../../../PLAYER/Stats/BODY_CONDITION_DESIGN.md): retain PH0/PH1 and player-specific vitality/healing rules. PH-HIT should reuse ELD0 detection when separately authorized.
- [ZD0–ZD3 corpse plan](DEATH_RAGDOLL_IMPLEMENTATION_PLAN.md): owns fall, piles, slowdown, aging and budgets. ELD1 extends only the injury-preservation boundary; ZD1–ZD3 remain separate.
- [Entity roadmap](../../IMPLEMENTATION_PLAN.md): retains existing entity/robot responsibilities; no new species integrations are authorized.

## 13. Code-review refinements and migration gates

These requirements refine the earlier sections and proposed hierarchy where current code suggests a simpler integration.

1. **Carry shape identity through the existing hit.** Current DamagePacket has no region metadata, and HitTargetResolver.Dispatch receives a collider without the hit-shape index. ELD0 must carry region/shape context backward-compatibly, map shape owners to cached regions, and retain one damage route. Existing firearm queries need narrowly scoped area/mask support. Actor movement capsules must not absorb every precise hit; audit firearm, cover, AIM1 and melee filtering before choosing layers. No extra anatomy ray per shot. Existing intentional firearm recasts are not authorization for additional queries.
2. **Reuse the player foundation.** Move the existing global BodyRegion enum with Unknown=-1 and Head=0 through RightLeg=5 unchanged; preserve its .uid where applicable. Evaluate the six-value BodyCondition storage and PlayerVitals coordination before adding another condition store. Do not migrate player state merely to implement zombie anatomy. Player limb hits currently do not directly reduce vitality, while head/torso hits do; preserve this policy, healing and event-driven HUD.
3. **Resolve once and commit before death notification.** HealthComponent.ApplyResolvedDamage already exists. Anatomy/visual state must be committed before invoking the lethal health path, so synchronous Died callbacks capture the final sever. Guard reentrancy and emit consistent snapshots. Existing ordinary IDamageReceiver callers retain their behaviour.
4. **ELD1 includes corpse correctness.** The same shot can sever and kill. Preserve limb visibility and disable severed hurtboxes before the ZD0 handoff, including ZombieController.Deactivate()/Pose(0). ELD4 must not be needed to fix disappearing/reappearing limbs. Keep valid pivots for pose code rather than freeing nodes still referenced by either builder or ZombieCorpsePose.
5. **Split ELD2 acceptance.** First validate limp and one-leg supported movement, then crawl and immobilization. Both share the current AI goals/reaction lifecycle. Test lowered collision transitions against floors, low ceilings, walls and actors; no independent crawler AI or living ragdoll.
6. **Start with biped regions.** Reuse the six-region contract for initial zombies and compatible humans. A configurable quadruped region schema is deferred until an actual consumer requires it. Shared corpse lifecycle remains independent of that schema.
7. **Demonstrate injuries without changing weapon balance.** Use explicit debug region-damage/sever controls in an isolated test scene. Keep vitality contribution configurable but preserve normal production damage until separately approved. Prefer deterministic thresholds initially; any probability must be bounded, seeded/testable and authored in settings.

## 14. Per-phase implementation handoff

Every phase must update this plan and owning READMEs with commit, actual checks, deferred local acceptance and limitations. No phase proceeds automatically.

| Phase | Owning changes / scope | Activation, idle and shutdown | Acceptance / exclusions |
| --- | --- | --- | --- |
| ELD0 | Shared region metadata/resolution; cached zombie visual-following hurtboxes; existing firearm/dispatch integration; enum migration | Detection only on accepted attack queries; hurtbox transforms follow active animation, including reaction/final pose changes, using the existing actor lifecycle; dormant actors add no polling; unregister on death/unload | Correct six-region hits on both variants; walls/doors block; ordinary receivers and both views preserved. No limb damage behaviour or player PH-HIT implementation |
| ELD1 | Optional condition/state component or reused storage; zombie adapter/settings; visual removal; minimal ZD0 snapshot extension | Hits change state; capability/visibility events only on changes; no idle limb callbacks; disable shapes and clear subscriptions on death/unload | Single mitigation/vitality application; correct limb disappears; lethal sever remains missing during fall/settling. No detached debris, new movement modes or attacks |
| ELD2 | Zombie locomotion helper plus standard/runner poses and collision transitions | Recompute mode on condition changes; animate/move in existing active physics; immobilized actors retain only required existing sensing; safe wake/shutdown | Limp/hop then low crawl/immobilization; retain pursuit, investigation, impacts and sleep. No CM5 or corpse simulation for living actors |
| ELD3 | Capability selection consumed by separately authorized CM5/shared melee | Recompute available attacks on anatomy changes; actor-owned scheduling sleeps with actor | Missing arm removes matching claw; bite eligibility respected; current reach/cover/damage rules. Requires CM5; does not authorize implementing it |
| ELD4 | Optional shared detached-part lifecycle and species visual adapter polish | Spawn only on sever; bounded active physics and total count; settle/expire/unload cleanup; no permanent simulation | Matching appearance, resource cleanup and bounded debris cost. No second corpse framework, inventory or pickup |
| ELD5 | Focused debug fixtures, configuration review and regression handoff | Debug tools explicit opt-in; use existing diagnostics where practical | Both variants/views, ordinary targets/player, cover, lethal sever, crowds and repeated unload. Record measured results; no implied profiling success |

Each authorized implementation starts by re-reading current source, root README, CodePreferences and linked owners. Keep source-implemented versus locally accepted status distinct. Review file boundaries at implementation time; the hierarchy above proposes responsibilities, not mandatory placeholder classes. All commits target implement-td0-td1; main is outside scope.
