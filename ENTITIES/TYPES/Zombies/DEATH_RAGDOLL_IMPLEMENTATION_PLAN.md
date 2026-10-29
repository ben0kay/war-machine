> **ZD4 articulated deaths — implemented in source, awaiting local validation (2026-10-29):** Nearby, budgeted deaths of both variants now use up to 11 joint-connected pelvis/torso/head/limb bodies, including elbows/knees; missing limbs allocate no bodies. Minimum 1.4s, stable 0.6s, maximum 4s, then bake the exact pose and remove active physics/joints. Existing static distance/overflow fallback, pile slowdown, 300s lifetime and budgets remain. [Current ownership, costs and validation](../../Shared/Corpses/README.md). Earlier one-box-only statements below describe the historical ZD0 implementation.

> **ZD1–ZD3 source implemented / awaiting local validation:** Shared frozen pile proxies and capped event-driven slowdown (zombies enabled, player opt-in off), 300s lifetime/8s sinking, 96 retained/8 active-fall budgets and supported static fallback. Current ownership/settings/limitations/checks: [shared corpse lifecycle](../../Shared/Corpses/README.md). Older deferred statements below are historical; ELD3/ELD4 and articulated ragdolls remain separate.

> **ELD2 source implementation (2026-10-28), awaiting local validation:** [Injury locomotion and World/FPS acceptance](LOCOMOTION_README.md). Both variants share limp, one-leg shuffle, low living crawl and immobilization. Debug panel buttons demonstrate injuries without HP changes. Existing attacks/ELD3 and ZD1–ZD3 remain deferred. This supersedes older ELD2-Planned/upright-legless descriptions below.

> **ELD1 source implementation (2026-10-28), awaiting local validation:** [Limb condition, deterministic severing and injury-preserving ZD0 handoff](../../Shared/Anatomy/README.md). Original vitality/weapon HP damage is unchanged. ELD2 locomotion, ELD3/CM5 attacks, ELD4 debris and ZD1–ZD3 remain deferred. Existing World/FPS scenes are the acceptance venue; no new test scenes. This supersedes older ELD1-Planned/no-severing statements below.

> **Anatomy/combat/corpse direction (2026-10-28):** [Overall master plan](ANATOMY_COMBAT_CORPSE_PLAN.md). ZD0–ZD3 retain fall/pile/lifetime/budget ownership and current statuses. ELD1 must preserve severed appearance on the death-triggering hit through the existing ZD0 adapter; optional debris is ELD4. This narrowly extends the older amputations non-goal only when ELD1 is explicitly authorized; it does not authorize ZD1–ZD3 or a second corpse framework.

# ZD — Zombie death, lightweight ragdoll falls and persistent corpse piles

> **Status: ZD0 implemented in source, awaiting local build/physics/visual validation. ZD1–ZD3 remain proposed.** ZD0 explicitly authorized by owner; implementation on `implement-td0-td1`. This is **not** a true `Skeleton3D` / `PhysicalBoneSimulator3D` articulated ragdoll; it deliberately begins with a **physics-assisted ragdoll-like collapse** for the project's existing procedural `Node3D` limb joints. Real articulated ragdolls can be assessed separately after performance/visual testing.
>
> **Intent:** A zombie dies, falls with direction/limb variation, lies in place, contributes to a visible pile and a *soft* obstacle/slowdown for moving actors, then starts sinking **300 seconds (5 minutes) after death**, disappears and releases all resources. Zombies 001 and runner 002 should reuse the same implementation.

## Confirmed code and integration points (read source again before editing)

- `ZombieController : EntityCore, IDamageReceiver, IImpactReceiver` handles both variants. `HealthComponent.Died += OnKilled`; today `OnKilled(DamagePacket hit)` simply calls `Deactivate(); CollisionLayer = CollisionMask = 0; Hide(); QueueFree();`. Replace **only the death presentation/lifecycle**, not shared damage or hit maths.
- `ZombieRunnerController` inherits the same controller but swaps `BuildArt()` and `Pose()`; both builders have cached `LeftArm/RightArm/LeftLeg/RightLeg/LeftKnee/RightKnee` `Node3D` pivots. The standard art also builds a `Head` node. There is currently **no physics skeleton**, and no physical bones.
- `EntityCore.Deactivate()` disables actor processing via `ProcessMode.Disabled`. **Do not leave the corpse beneath the deactivated zombie**: it will not have a reliable active lifecycle. Detach/transfer its visual subtree into a separate corpse owner before freeing the actor, preserving global transform and spawn-seeded materials. Ensure deactivation's `Pose(0)` reset cannot erase the intended death pose (capture useful transforms first or apply the fall only after detaching).
- `DamagePacket` already includes source, position and direction. Use its sanitized, horizontal hit direction as an optional fall/impulse hint; fall back to deterministic left/right/forward/back variation if absent or nonfinite. No duplicate damage delivery or reliance on the order of later `ReceiveImpact` calls.
- `WORLD/WorldController.cs` currently places zombies in `WorldActorsRoot/PersistentEntitiesRoot/Zombies`, using terrain support checks. Give corpses a **separate** scene/world owner under `PersistentEntitiesRoot` (e.g. `Corpses`) managed on world unload. Do not edit Terrain3D data or the active player/weapon systems merely to create a corpse.
- Zombie `CharacterBody3D` collision is layer 3 (value 4), mask 23; the living actor's upright capsule must **not** remain as an invisible blocker after death. `ZombieMaster.tres` and runner master are distinct; preserve their current settings, appearance variation, health, reactions and behaviours.

## Phase overview / implementation order

| Phase | Deliverable | State |
| --- | --- | --- |
| **ZD0** | Death handoff, varied low-cost physics-assisted falling, settle into frozen visual pose | **Implemented; Awaiting Validation** |
| **ZD1** | Stationary corpses that visibly pile up, with mild event-driven movement slowdown for passing entities | **Implemented; Awaiting Validation** |
| **ZD2** | Five-minute corpse lifetime, timed sinking/removal, reliable collision and resource cleanup | **Implemented; Awaiting Validation** |
| **ZD3** | Crowd/streaming budget, debugging and regression/performance acceptance | **Implemented; Awaiting Validation** |
| **ZD4** | Short-lived articulated procedural ragdolls, frozen limb poses and cheap distance/overflow fallback | **Implemented; Awaiting Validation** |

Codex may implement **ZD0–ZD3 together as one bounded job when explicitly asked to implement the entire corpse feature**, or individually when authorized by phase. The plan itself is not authorization to implement anything.

## Proposed reusable folder structure — species-independent foundation

**Build the shared corpse feature once, not a zombie-only corpse system.** Zombie 001 and runner 002 are the **only initial death integrations**. Deer, bears and humans are examples of *future consumers*, not authorization to implement those species' death logic now.

```text
ENTITIES/
  Shared/
    Corpses/                               REUSABLE for all future living entity types
      CorpseManager.cs                     one registration, TTL, budget and cleanup owner
      CorpseBody.cs                        fall -> settle -> sink -> free lifecycle
      CorpseSlowArea.cs                    soft overlap-based movement modifier
      ICorpsePoseAdapter.cs                species/rig-independent pose adapter contract
      Configuration/
        CorpseSettings.cs                  common typed tunables, safe value limits
        CorpseMaster.tres                  shared 300s rest + sink/budget defaults
        README.md                          ownership of shared settings
      README.md                            explain shared mechanics and species adapters
  TYPES/
    Zombies/
      ZombieController.cs                  one-shot death handoff to shared manager
      ZombieSettings.cs                    optional species-specific corpse tuning
      ZombieCorpsePose.cs                  maps existing procedural biped joints to fall pose
      Zombie_002/
        ZombieRunnerController.cs          reuses ZombieCorpsePose / shared lifecycle
      DEATH_RAGDOLL_IMPLEMENTATION_PLAN.md this document
    Wildlife/
      Deer/                                EXISTING; later add DeerCorpsePose.cs (NOT NOW)
      Bear/                                FUTURE; later add BearCorpsePose.cs (NOT NOW)
    Humans/                                FUTURE; later add HumanCorpsePose.cs (NOT NOW)
WORLD/
  WorldController.cs                       wires one CorpseManager and independent Corpses root
```

The ZD0 manager/body/pose/settings subset now exists; slowdown, lifetime and other later-phase files shown remain **proposed**. Do **not** create unused Deer/Bear/Humans folders, placeholders, scenes or logic merely because they appear in this diagram. Keep `C/` for generated art only. Plain C# helpers are preferred; the shared manager and active fall owner may be nodes where Godot physics requires them. No extra scene nodes or `_Process` loops per settled corpse, no one-scene-per-species duplicate systems, and no permanent per-limb rigidbody simulation.

### Shared contract vs. species-specific anatomy

- **`CorpseManager` and `CorpseBody` must not know about `ZombieController`, `LeftArm`, `RightKnee`, `ZombieMeshBuilder`, or any concrete animal/human class.** Their input should be a detached visual hierarchy, a small pose adapter, initial world transform, sanitized death-hit impulse and optional settings/footprint data.
- **`ICorpsePoseAdapter` is the boundary.** It exposes the minimum actions the shared fall owner needs: initialise/capture pivots on a provided visual hierarchy, apply a normalized fall/limb-relaxation pose while active, and preserve the settled pose. Its implementations may work with procedural `Node3D` pivots **or** an imported skeletal rig later. Keep the API small and avoid a generic animation framework, per-frame scene searches or zombie-specific enum values in `Shared`.
- **`ZombieCorpsePose` handles only biped presentation.** It caches the existing visual joints once and supports both standard and runner zombie artwork; the runner may supply a small tuning override but must not duplicate TTL, physics, slowdown or cleanup. Preserve spawn-seeded clothing and appearance resources in the transferred visual.
- **Future deer** would supply a quadruped adapter (front/hind legs, neck/head); **future bears** a heavy quadruped adapter with a wider footprint/response; **future humans** could reuse the biped adapter or have a `Skeleton3D` adapter. These future adapters should plug into the same manager/body without modifications to lifecycle code.
- **Mass, fall response, footprint and optional movement-slow tuning** may differ by species through a small per-spawn settings snapshot/override. Shared `CorpseMaster.tres` remains the default owner for lifetime (300s), sink time, physics budgets and removal; don't hardcode zombie-specific constants in the manager.
- **Soft slowdown is an opt-in mover capability**, not a dependency on any zombie AI or player class. A future deer/bear/human mover can consume the same overlap penalty without importing zombie code. If adding an interface is useful, make it a narrow movement-modifier receiver; never let corpse areas write directly into unrelated character velocity without a cleanup/overlap contract.

**Future extension acceptance:** it should be possible to add a hypothetical deer corpse with a `DeerCorpsePose` adapter + species settings/handoff, **without editing** `CorpseManager.cs`, `CorpseBody.cs` or `CorpseSlowArea.cs`. Do not actually implement deer deaths in this ZD job.

## ZD0 — One-shot fall / ragdoll-lite

1. On the first lethal `HealthComponent.Died` notification, **latch death once** and stop roaming, chasing, sight timer, investigation, reaction, debug cone, collision and incoming combat. Preserve existing `HealthComponent` ownership and NPC health feedback events. Avoid the old immediate hide-and-free until the corpse visual has been handed off.
2. Transfer the original `VisualRoot` or a lightweight pose-preserving copy to the independent `CorpseBody`, and pass a cached `ICorpsePoseAdapter` with the visual. The original character/collision can then be removed. Preserve the runner's lean body and existing clothing/blood material variations; no new model, mesh regeneration or duplicated material palette. Neither `CorpseBody` nor `CorpseManager` may fetch hardcoded zombie joint names.
3. Start a bounded **physics-assisted collapse (~1–2.5 seconds; tune)**: one temporary simplified torso/pelvis `RigidBody3D` (or equivalent small physics proxy), contact with terrain and already-settled corpses, modest hit-direction impulse and angular damping. Call the **species pose adapter** during the short active collapse so limbs/head swing or relax with cached joint rotations. A front/back/side fall and irregular limbs should look natural. This is **not** independent physics per limb; keep expensive actual full-body ragdolls as an optional future milestone.
4. Keep torso/fall geometry from tunnelling through ground and walls; do not snap bodies up onto roofs or move them across terrain. Follow confirmed collision ground. If there is no safe support (unloaded terrain, steep cliff, cave edge, invalid height), use a bounded fallback collapse/dismissal rather than a long-lived floating/falling body. Avoid unrealistic impulse sizes.
5. Settle when contact and low velocity are stable or when a hard short timeout is reached. Freeze the final joint positions and transform; replace/disable moving rigid physics. **A settled corpse must have zero per-frame fall/gait/AI processing.** If killed while running, do not continue the runner's gait or rotate it toward the player.

**Suggested defaults:** enable=true, fall duration cap 2.5s, stable threshold ~0.15m/s, angular damping to taste, small seed-based variation. Use existing `DamagePacket.Direction` when valid. Store authored values in typed master resources; no magic `Time.GetTicksMsec` calculations scattered through actors.

## ZD1 — Corpse piles that impede without trapping

1. Corpses remain on ground as visible static meshes (not active `CharacterBody3D` actors). New falls may make contact with a **single, low-cost corpse proxy** per settled body so they can layer visually; do not simulate friction/limbs forever. Avoid creating dozens of contact shapes per individual corpse.
2. A **shallow broad footprint** / `Area3D` on settled corpses signals nearby live movers. Apply **mild slowdown**, initially suggested `0.80×` normal movement in the footprint; an overlapping pile can become more troublesome but **cap combined slowdown** (e.g. no lower than `0.65×`, never multiplicative exponential slowdown). Apply via one shared movement-modifier contract or a narrow existing movement API rather than hijacking zombie perception, health, gravity or `MoveAndSlide()`. Track enter/exit, and clear penalties on corpse disposal, death, actor unload or mode change.
3. Implement/verify slowdown initially for zombie 001 and runner 002, exposing a narrow **species-independent opt-in movement-modifier contract** usable by player, deer, bears and humans later. Make player slowdown explicitly configurable and test it before enabling globally. Both actors must be able to **walk over or around** piles, not become permanently stuck on a hard full-height collision wall. Do **not** attach the old standing capsule to a corpse.
4. **Collision trade-off:** Godot `CharacterBody3D` does not automatically step over a raised obstacle. Prefer soft Area slowdown as the default initial gameplay blocker. If an optional very low solid pile collider is added, require demonstrated step/slide clearance for player, runner and shambler before enabling it; otherwise keep solid collision only for fall-on-corpse physics and not for living actor movement. Avoid blocking vision, bullets, the camera, or zombie targeting unless explicitly authorized later.
5. Give the foot-area one bounded shape (roughly torso/leg footprint), on a designated collision layer/mask chosen after auditing existing world/weapon/sight layers. No per-frame scene-tree scans or distance calculations per corpse. Visual pile height should arise from actual settled transforms/contacts where feasible rather than randomly teleporting corpses upward.

## ZD2 — Five-minute lifetime and disappearing sink

1. Record `deathTime` once from a **monotonic runtime clock**. Default corpse rest lifetime **300 seconds**, measured from *death*, not when the fall finishes. At expiration, enter Sinking, disable foot slowdown/contact **immediately**, then animate the visual down through the terrain (default **8 seconds**) with an optional subtle fade during its final portion.
2. Remove the entire corpse scene and deregister on completion; release any materials/physics subscriptions and prevent lingering Area3D slowdowns or orphaned transforms. Sinking should not keep a standing/hidden collision body. Avoid a `Timer`/`_PhysicsProcess` per corpse for five minutes.
3. One `CorpseManager` can own an active-only coarse wake/timer (e.g. 0.5–1.0s cadence) to expire corpses and start sink transitions; the **small set currently sinking** may temporarily animate. Sleep manager when no corpses exist. Never age a corpse only while it is onscreen; offscreen corpses expire too. Unloading a world/chunk must free its corpses safely without retaining references or saving persistence (save/load out of scope).
4. Repeated deletion and world exit must be safe: death callback at most once, deregistration idempotent, no attempts to animate/fade an already-freed mesh. If world streaming is integrated later, determine per-chunk ownership and skip duplicate spawning on re-entry.

## ZD3 — Performance/budget and checks

- **Default budget proposal:** max ~96 live corpses in the world and at most ~8 simultaneous physical falls; keep these tuneable. On pressure, prefer the nearest/visible recent falls, skip expensive fall simulation for distant deaths, and retire the **oldest settled** corpse first. No endless backlog; a distant kill can become an immediate settled pose. A full horde should not cause a persistent physics spike.
- Use shared meshes/materials, cached refs/shape resources and a single bounded registry; avoid allocating LINQ/lists or rebuilding geometry every physics tick. Freeze or disable expensive sim immediately after settle, and never keep old zombies' AI/LOS callbacks alive. Integrate with existing fog/visibility policy, but do not use culling as the sole lifetime controller.
- **Build/regression:** `dotnet build war-machine.csproj`; top-down `WORLD/World.tscn` and FPS debug smoke test. Shoot one shambler and one runner; verify different directions, short fall, no more damage/AI, settled pose persists, pile appearance and modest movement slowdown; kill several in one location and ensure walking through them is possible. Toggle collision visualizer to inspect shape footprint; check steep ground/cave and missing terrain. Verify 300s from death + ~8s sink and no invisible collider/slow area afterward. Use temporary configurable short TTL for testing, not a permanent five-minute wait.
- **Performance/regression:** small crowd and ~20-zombie horde; many kills simultaneously; saturated corpse cap; repeated world loads/unloads; idle profile after settling should show **zero corpse physics updates** apart from the one coarse manager timer. Verify memory/physics objects fall back to baseline after removal. Inspect NPC health bars, hit reactions, recoil/shove, enemy movement, sight checks and player blocking separately.

## Explicit non-goals and Codex handoff

Not in this feature: player, deer, bear or human death integrations (only the reusable extension seam); amputations/dismemberment; decaying gore; corpse loot or interaction; reanimation; persistent corpses across saves; new enemy navigation; true bone-ragdoll simulation; damaging the player on corpse contact; unlimited solid obstacles; scene-wide physics queries every frame.

**Codex instruction once authorized:** Re-read the root README, `CodePreferences.md`, this document, `ZombieController.cs`, both mesh builders, `EntityCore.cs`, `HealthComponent`, `DamagePacket`, `WorldController.cs`, active collision layers and FPS test. Implement the smallest shared solution, **including the proposed `Shared/Corpses` structure and `ICorpsePoseAdapter` seam**, honoring current `OnKilled` and deactivation ordering. Keep the death/aging/slowdown implementation species-independent and integrate only zombie 001 and runner 002. Update owning READMEs with **implemented vs planned** status. Review diff and do one brief build/basic check when possible; give explicit manual tests if Godot unavailable. Commit all authorized changes **once** to `implement-td0-td1` only. Do not silently expand to other entity species or change combat damage.

## ZD0 implementation handoff — 2026-10-28

Shared Corpses manager/body/settings and ICorpsePoseAdapter implemented; ZombieCorpsePose supplies existing standard/runner biped joints. World/FPS composition binds one independent root, and lethal callbacks latch/deactivate/detach visuals before actor removal. One CCD box proxy drives a bounded hit-directed fall; stable supported horizontal bodies freeze with disabled shape/collision and no idle callbacks. Unsupported or failed falls dismiss at the cap. Original visuals/materials are transferred, not rebuilt. Actual settled-on-settled contacts belong to ZD1 and are not introduced by this phase; slowdown, timed cleanup/sink and crowd budgets remain unimplemented. No other species death changes. Source/reference/whitespace checks only; no Godot/.NET available. Supplied actual-code smoke test and manual wall/slope/both-view checks: DEBUG/Corpses/README.md. Frozen visuals persist until scene teardown until ZD2. Delivery SHA is in the job response.
