> **CM5-A attack visual refinement (2026-10-29):** The standing strike now includes opposing windup torso/head sway, forward twisting lunge and asymmetric alternating lead arms. Both species use one shared cached upper-body pivot under the art root, preserving ordinary gait/capsule and timed melee hit. Tune five visual fields under `ZombieSettings`'s *Attack lunge pose* group; see [attack animation plan](ATTACK_ANIMATION_PLAN.md). Implemented in source; local compilation, visual, hitbox and corpse checks pending.

> **HE1 update:** One clear/blocked obstacle ray per consumed sound; blocked lines favour early give-up and shorter deadlines. Retarget cooldown 6s. Source complete; local validation pending. [Tuning and handoff](../../Shared/Sensing/HEARING_INVESTIGATION_PLAN.md).

> **HE1 source implemented / awaiting local validation:** Active zombies now receive gunshot events and investigate one distance-uncertain estimate with separate sound commitment/retarget policy. Shared sight/hit settings and physically settled wander anchors are preserved. Rifle/MG hearing radii: 130m/120m. Current ownership, tuning and checks: [HE1 handoff](../../Shared/Sensing/HEARING_INVESTIGATION_PLAN.md). Older no-listener/hearing-proposal notes below are historical. HE2/HE3 remain planned.

> **CM5 source implemented / awaiting local validation:** Zombie standing strikes now use the shared melee executor and existing player-health route. Damage/timing and checks: [CM5 handoff](../../../COMBAT/CM5_README.md). ELD3 attack capability rules remain deferred.

> **ZM-PERF2A — source complete / awaiting local Godot and profiling (2026-10-28):** `C/ZombieVisualBatcher.cs` now combines compatible same-joint, same-material geometry into **one ArrayMesh surface per group**, caches a clean template per species and reuses the mesh resources across actors. Per-part palette names are replaced by explicit `SkinBatch`/`FaceBatch`/`ShirtBatch`/`PantsBatch` categories. The original zombie/runner rig pivots, seed-based appearance, anatomical hit bounds, detachable limbs, visual hiding, corpse handoff, brain/physics movement and shadows remain unchanged. See C/ and Zombie_002/C READMEs. **ZM-PERF2B distance LOD remains unimplemented.**

> **ZM-PERF1 (2026-10-28), source implemented / awaiting local performance acceptance:** Zombie 001 **42 → 29** and Runner 002 **40 → 29** separately rendered visible mesh parts. Simplification is contained in both `C/` mesh builders: no changes to combat, AI, limb injury/stumps, shared textures, corpse handling, or FPS/top-down player input. Detailed scope/tests in [C/README.md](C/README.md) and [Zombie_002/C/README.md](Zombie_002/C/README.md). ZM-PERF2 mesh batching/LOD is still future work, not included here.

> **ELD-PERF1 source implementation (2026-10-28), awaiting local build/correctness/performance validation:** [Deferred anatomy and A/B acceptance](../../Shared/Anatomy/DEFERRED_HIT_DETECTION.md). Default is one broad native shape per actor; current joint-local limb boxes are resolved only during firearm/bat attacks. No per-frame limb-shape updates in deferred mode. Physical comparison mode requires a profile edit and scene restart. This supersedes older eight-native-shapes/always-sync descriptions below; no measured FPS gain is claimed.

> **Shared investigation + bullet-hit clue (2026-10-28):** [Current owner/acceptance](../../Shared/Investigation/README.md). The existing last-seen episode policy now lives in a species-independent passive helper. Surviving player bullet contacts can start the same fixed-point search with the same give-up rules, independently of flinch. Both zombie variants/views share it. No nearby-gunshot hearing/HE1, new movement AI or attacks are included. Source implemented; local validation pending. This supersedes older hit-trigger-deferred/no-shot-investigation statements below; existing hearing phases retain their own status.

> **Damaging melee backsteps (2026-10-28):** A hit with the equipped bat now produces real, collision-aware backward zombie travel using the existing `MoveAndSlide` path, plus a short alternating-leg and torso-recoil pose. Normal 001 and runner 002 both inherit it; `ZombieSettings.MeleeMaxStaggerSeconds=0.55` controls its maximum duration. Bat strength/timing live with the bat profile; bullets and Q shove remain separate, unchanged. Source update awaits local Godot/compilation acceptance.

> **ELD2 source implementation (2026-10-28), awaiting local validation:** [Injury locomotion and World/FPS acceptance](LOCOMOTION_README.md). Both variants share limp, one-leg shuffle, low living crawl and immobilization. Debug panel buttons demonstrate injuries without HP changes. Existing attacks/ELD3 and ZD1–ZD3 remain deferred. This supersedes older ELD2-Planned/upright-legless descriptions below.

> **Zombie PER tuning (2026-10-28):** Current regular-zombie `WanderSpeed=0.78m/s` and `ChaseSpeed=1.38m/s` (+20% each). Both regular and runner `DetectionRange=22.5m`, `LoseRange=28.75m` (+25% each). **PER2A is now implemented in source**; see [alerted tracking](../../Shared/Sensing/ALERTED_TRACKING_PROPOSAL.md). After an initial frontal detection, zombies can retain/reacquire an unobstructed target from any bearing while chasing/investigating. Hit-triggered investigation is deferred.

> **ELD1 source implementation (2026-10-28), awaiting local validation:** [Limb condition, deterministic severing and injury-preserving ZD0 handoff](../../Shared/Anatomy/README.md). Original vitality/weapon HP damage is unchanged. ELD2 locomotion, ELD3/CM5 attacks, ELD4 debris and ZD1–ZD3 remain deferred. Existing World/FPS scenes are the acceptance venue; no new test scenes. This supersedes older ELD1-Planned/no-severing statements below.

> **ELD0 source implementation (2026-10-28):** [Shared region detection](../../Shared/Anatomy/README.md) now routes firearm shape hits for both zombie variants in both views. BodyRegion moved to ENTITIES/Shared/Anatomy with values unchanged. Existing damage balance/player rules and melee stay unchanged; no injuries, severing or new attacks. Local build/runtime validation pending.

> **Anatomy/combat/corpse direction (2026-10-28):** [Overall master plan](ANATOMY_COMBAT_CORPSE_PLAN.md). ELD0–ELD5 are Planned. Combat, player-condition and ZD plans remain their implementation owners; the master coordinates region hits, limb state, living injury movement and injury-preserving death. No new gameplay is implemented by this documentation update.

> **ZD0 implemented in source, awaiting local validation:** normal and runner zombie deaths transfer their existing visuals into the [shared corpse fall owner](../../Shared/Corpses/README.md), collapse using one temporary physics proxy, then freeze without collision or idle updates. [ZD plan](DEATH_RAGDOLL_IMPLEMENTATION_PLAN.md): ZD1 pile slowdown, ZD2 five-minute sinking cleanup and ZD3 budgets remain planned. No articulated ragdoll or timed removal yet.

# Zombies

Reusable mobile actors with basic incoming hit support inheriting `ENTITIES/Shared/Entity.tscn`. `Zombie.tscn`, `ZombieController.cs`, original procedural low-poly artwork and `Configuration/ZombieMaster.tres` all belong here, not inside the preview world. World placement calls `InitializeTarget(Node3D player, uint seed)` after spawning on safe ground. This keeps zombies independent of TopDownPlayer and Terrain3D.

Current default regular-zombie wander **0.78m/s** (+20%), chase **1.38m/s** (+20%), wander radius **10m**, acquisition **22.5m** (+25%), retention **28.75m** (+25%), stop **1.3m** from the player. Runner speed remains unchanged; its vision ranges share these new values. Each actor copies its settings through EntityCore. Idle wandering arms hang low; pursuit raises both arms forward and keeps them extended near the player. Short asymmetric arm sway, alternating steps and subtle bob create a slow shamble. Mesh and material resources are shared across instances.

Sight decisions use an individually staggered, one-shot **0.4s timer** with an explicit player reference, no global searches; gated, bounded LOS raycasts. Wandering includes random 1–3s rests. Active motion/falling uses CharacterBody3D and one gait callback; grounded stationary actors stop physics and animation, retaining only the sight timer. Deactivate stops that timer and all processing through EntityCore; scene unload owns cleanup. Particle/audio/attack loops are absent. This is a small active prototype population, not an unbounded crowd/streaming manager.

Awareness now uses frontal eyesight and physical cover (PER0/PER1). PER2 adds last-seen memory (current handoff below); attacking, zombie/robot targeting and navigation/pathfinding remain unimplemented. Zombies stop near the player and cannot hurt them. Collisions can block a direct route; stuck wandering picks another goal. Larger crowds or obstacle-rich worlds will require deliberate navigation/relevance work later.

## ZW0 — Basic wander and converge

**Implemented, Awaiting Local Validation.** Authorized scope: reusable zombie entity and six top-down preview placements, slow random wandering, sight-based pursuit, arms-out chase pose. Code owner: Codex/ChatGPT Work; brief local build/appearance checks: owner/Copilot. Existing robot and FPS scene preserved. No later zombie/combat phases authorized.

Source/resource paths, inherited scene wiring, collision masks and whitespace reviewed. Godot/.NET unavailable here; compilation, physics grounding, animation and crowd behaviour are unverified. Locally build, F6 the top-down scene, observe six zombies, approach/leave detection range (18m/23m), watch arms rise/lower and ground contact, check pause/deactivate and unload. No terrain rebake needed.

## Incoming weapon hits

Zombies forward DamagePacket hits through HealthComponent and DamageResolver via the shared IDamageReceiver contract. HealthProfile.MaxHealth (100 by default)
lives in ZombieMaster.tres and resets when InitializeTarget configures the actor.
A lethal hit immediately deactivates its brain/movement, clears collision and frees
the actor; no death animation/loot/respawn or outgoing damage. The default 95-damage
rifle takes two hits. Existing wandering/pursuit remains unchanged.
Awaiting local compilation and physics/gameplay validation.

Health/defence rules now live in [Shared/Combat](../../Shared/Combat/README.md),
not in ZombieController. The controller owns hit eligibility and its death
reaction. Rifle damage is 95; zombie HealthProfile.MaxHealth is 100 with zero
defences, so the first hit leaves 5 HP. HealthProfile is embedded in the existing
ZombieMaster.tres; no separate per-zombie health file is required.

## Ownership cleanup

Controllers/settings now live directly in this folder. Procedural geometry is in C/ZombieMeshBuilder.cs and future imported models in Artwork/. World placement is now WORLD/WorldController.cs.

## Procedural zombie appearance pass (awaiting local validation)

The original cube-like placeholder has been replaced with a stylized organic silhouette, mottled decayed skin, several worn clothing palettes, head/face details and uneven sleeves. The original shamble rig has also gained small cached knee pivots driven only during existing movement. No independent animation processing, physics changes, combat, AI, or new settings were added. See [C/README.md](C/README.md) for the appearance details and short F6 validation checklist. Not yet visually verified in Godot.

## Zombie 002 — Runner prefab (not placed)

A separate [Zombie_002](Zombie_002/README.md) prefab now inherits `Zombie.tscn` and `ZombieController` without duplicating sight, health, terrain recovery, idle decisions or damage. It has a leaner generated body and a distinct running animation when it sees the player. Its independent `ZombieRunnerMaster.tres` sets `ChaseSpeed = 8.3 m/s`, slightly above the player's configured 8.0 m/s sprint. Wandering remains slow (0.85 m/s). The normal Zombie 001 remains unchanged in the world; **Zombie 002 is not spawned anywhere yet**, and still cannot attack. The visual/gait tuning hooks added to the shared controller preserve its default shambler behavior. Source review only; local build and visual verification pending.

## Building placement and trunk collision

Zombies now collide with pine trunks via physics layer 5. This prevents walking through trees; movement remains a direct slow approach, without navigation around obstacles. Spawn locations receive small pine clearings. The robot does not collide with this trunk layer.

## Ground penetration diagnostic / bounded recovery

A screenshot reported a zombie sinking near the original hill. Source inspection confirmed FULL_GAME Terrain3D collision (mode 3), terrain layer 1 and zombie mask 23 including terrain. No direct position writes occur during normal zombie movement, only MoveAndSlide. The exact runtime failure has not been reproduced or identified; this follow-up is a defensive recovery/diagnostic, not a confirmed underlying terrain fix.

WorldController now supplies an optional terrain-height delegate to InitializeTarget; zombie behavior still has no Terrain3D type dependency. On its existing staggered 0.4s physics brain timer, only an airborne/falling zombie samples that height. If its feet have sunk more than GroundRecoveryDepth (default 0.75m), it casts one cached ray around the expected surface and checks capsule clearance. Only a walkable solid hit plus a clear destination permits recovery; velocity/interpolation reset afterward. No surface/blocked destination means no teleport. One warning per actor records its world position, terrain height, mask and recovery result for local diagnosis.

Ordinary falls from the cliff are above lower terrain, so this check does not catch/hold them in the air. Terrain holes/missing regions return NaN and are left alone. The optional height contract represents open terrain: do not supply it unchanged to future underground/cave actors. GroundRecoveryEnabled/Depth live in ZombieMaster; callers without a sampler keep prior behavior. No new timer/frame callback, no idle ground queries or whole-world scan. Existing collision masks and movement settings remain unchanged.

Validation: source guards, masks, optional-call compatibility, normal cliff-fall versus below-ground conditions and whitespace reviewed. Godot/.NET unavailable: original issue reproduction, engine physics queries and runtime recovery remain unverified. Pull/build and F6 the world; walk zombies across the hill foot, along rocks and down cliff edges. If sinking repeats, capture Output's `Zombie ground penetration` warning with the location/result. No terrain rebake or edits.

## PER0 / PER1 — frontal eyesight and crouch cover

Implemented, awaiting local build/runtime validation. Existing staggered physics brain timer now gates by horizontal range and **160° forward cone**, oriented with VisualRoot (-Z), then queries shared VisibilityResolver at most twice: head first, torso only if blocked. Actual transitioning player capsule height supplies samples (standing head 1.7m, crouched 1.3m). Clear either sample means visible; crouching in the open does not grant invisibility. Height-sensitive cover around 1.45m is useful for testing. Both top-down and inherited FPS use this same physics logic.

**Current values:** DetectionRange 22.5m acquires, LoseRange 28.75m retains only with **the same forward cone** and clear LOS. This means a player running behind an alerted zombie currently breaks vision even with an unobstructed ray. **PER2 is implemented:** on sight loss, the zombie investigates a sampled fraction (or all) of the last *verified* seen position, then settles and moves its wandering anchor. **PER2A is implemented**: the forward cone only applies to initial detection; chasing/investigating uses the retention range and mandatory clear head/torso LOS from any bearing; without future navigation, buildings may physically block the goal. **HE0/HE1 hearing is also planned, not active**; even if the player hears a rifle shot via the audio pool, firing behind a zombie does not currently alert it. Robot scanner/beam unchanged.

Tune ZombieMaster.tres: VisionAngleDegrees, EyeHeight (1.6m), SightIntervalSeconds (0.4s, minimum 0.1s), SightBlockerMask (17 = solid world layer 1 + trunk layer 5), plus existing ranges. Player/zombie layers 2/3 and occlusion Areas layer 8 are not sight blockers. Self and target physics RIDs are excluded. Terrain, walls, car and collidable rocks block. **Glass currently shares wall bodies and blocks sight too**; opaque classification/glass separation remain PER3. Grass/canopies do not block.

The world installs DEBUG/Perception/ZombieVisionDebug: faint red eye-height horizontal cone sectors follow actual visual heading. **F7** toggles all; ShowVisionCone controls initial visibility. The fan expands to the current acquire/retain range; arms-out means acquired. This is potential FOV, intentionally drawn through scenery for debugging, not an occlusion-clipped silhouette or in-world scan beam. Cover inside the cone can still block sight. It adds no callbacks/rays per frame; meshes follow parent transforms. Disabled/deactivated cones hide, death/unload frees them. One world-local input handler retains explicit actor references and prunes freed actors on toggles.

Cost: existing timer only while actor active, cheap gates before up to 2 LOS rays/check (12 actors at 0.4s gives at most ~60 sight rays/s before gating; not measured). Waiting active zombies still think, inactive actors stop entirely. No extra timers, target scans or new physics callbacks.

Brief local checks: build, F6 World and FPS; approach from behind vs front, check open crouch remains visible, crouch behind medium solid cover, full wall/ridge blocks and stepping out reacquires, arms lower after loss, F7 hides/shows cones, death/unload clean, robot unchanged. Godot/.NET unavailable here; runtime checks remain outstanding.

Zombie_002 is now explicitly placed by WorldController in both world views: three separate RunnerSpawnOffsets in WorldMaster, retaining the 12 slow placements. Native grounding, tree/rock exclusions and F7 debug registration reuse existing placement. Runner behavior/stats remain those of its prefab; local runtime validation pending.

## Missing collision at horde spawns — awaiting local validation

Owner logs show multiple horde actors falling below terrain height ~4m with no solid surface found by recovery. Actual native collision failure is not yet reproduced. World now explicitly sets runtime FULL_GAME coverage and rebuilds after terrain data/sculpt and camera binding, before actors activate.

Actors with an open-terrain sampler wait at spawn until the existing staggered physics brain timer confirms a walkable collider within 0.6m of expected terrain height. Gravity/movement sleep until confirmation; one warning identifies missing ground and that timer retries. This guards startup falls without inventing collision or teleporting through geometry. Persistent warnings mean actors remain waiting and native collision still needs diagnosis. Later penetration recovery stays unchanged. Placement resets interpolation. No new timer/frame loop or scans; one startup ray per actor, low-rate retries only while waiting. Future cave spawns must omit/replace the open-terrain sampler.

Source/loop review only; compilation not verified (Godot/.NET unavailable). Build and F6 World: inspect all 20 horde spawns, chase over their ground, slopes/cliff edges, inspect Output for spawn-wait/penetration warnings, then check FPS. Runtime success and the underlying collision failure remain awaiting local validation.

## ZMAT0 — cached procedural zombie appearance variation (2026-10-27)

**Implemented in source; awaiting local Godot/.NET compilation and visual checks.** Shared `C/ZombieAppearance.cs` now generates **28 reusable 64×64 RGBA8 textures with mipmaps once**, rather than one texture per zombie: four general skin tones; four clean/four subtly bloody faces; four clean/four blood-stained shirts; four clean/four stained trouser finishes. It also applies natural mottling and patchy dirt. The previous 9-texture static palette in `ZombieMeshBuilder.cs` was replaced (not duplicated). The independently-modelled `Zombie_002` runner uses the **same shared pool**, not its own duplicate textures.

When `InitializeTarget(player, seed, ...)` is called, the appearance is **deterministically chosen from the existing spawn seed** by a small integer hash, without consuming the zombie's movement RNG. The skin, shirt and trouser palettes vary independently; by default **45% of zombies are eligible for blood**. Where blood occurs (face, clothing, or both with an occasional trouser patch) is determined independently from the seed. Blood is muted/dried and blends into the underlying colour—no glowing red. Existing bruise and wound props remain, as do rig pivots, logic and collisions. Standalone prefab editor previews use a provisional instance-ID selection until initialized with a spawn seed.

Both `ZombieMaster.tres` and `Zombie_002/Configuration/ZombieRunnerMaster.tres` expose two **Appearance** settings through their inherited `ZombieSettings`:

- `AppearanceVariationEnabled = true` — set false to use a consistent, blood-free fallback palette.
- `BloodChance = 0.45` — probability (0–1) that an actor receives one of the stained palettes.

These settings are read **only at spawn/initialize time**; edit, reload and reinitialize to view changes. Blood pattern strength/dirt details are intentionally fixed inside the cached palette for this small pass, rather than generating new texture copies for every slider setting.

**Memory budget:** 28 × 64 × 64 × 4 RGBA bytes × approximately 1.33 for mipmaps = **~0.58 MiB raw pixel storage**, shared by all zombies and runner variants (excluding texture resource and driver overhead). No new shaders, per-frame CPU loops, per-actor texture/image allocations, PNG files, mesh nodes or material instances are created by variation selection. It does not solve draw-call scaling for future hundred-actor hordes.

**Quick owner check:** rebuild .NET scripts; F6 `WORLD/World.tscn` and inherited first-person test; approach several standard zombies and runners to compare skin/shirt colours, facial smears, clothing stains and clean variants. Check blood is unobtrusive at a distance, the original animation pivots remain correct, and reloads preserve a spawn-seeded appearance. Try `BloodChance=0` and `BloodChance=1` (reload) to compare. Full Godot runtime/render testing not performed here.

## Refined human-like procedural geometry

Zombie 001 and Runner 002 now use a small shared cached profile mesh library `C/ZombieHumanGeometry.cs` with human-shaped shoulder/waist and skull contours, thin eyes, slimmer limb and foot silhouettes, and short hanging fingers. The original animation pivots, speed and combat AI remain in place, and spawn-seeded material variation is retained. The shambler has slight left-knee/arm asymmetry; runner keeps its chase animations. No imported models or new material pass. Godot visual and build validation pending.

## PER2 — last-seen search and safe new wander anchor

Implemented, awaiting local build/gameplay acceptance. ZombieInvestigation.cs extends the same partial ZombieController with early/mid/thorough remembered-position travel, speed-scaled timeout and thorough look-around. Both masters expose the policy. A zombie that loses interest settles physically, then wanders around that actual stopping point rather than its spawn. New wander goals must pass support, slope, full-body overlap and direct capsule-sweep checks (four attempts maximum); failed selections rest. This is conservative local clearance, not navigation around buildings or a promise of traversability across cliffs. Existing colliders are required for obstacle avoidance.

No extra timer/node/idle physics; existing decision ticks manage search and existing movement/gait handles motion. IsChasing means visible pursuit; IsInvestigating is separate, and IsPursuing lets slow/runner movement preserve chase speed/gait while traveling to the clue. Sight reacquisition cancels search; death/deactivate/reset/unload clear memory. See ../../Shared/Sensing/PERCEPTION_IMPLEMENTATION_PLAN.md PER2 handoff for defaults, costs and exact local checks. No convenience-store scene changes.

## Convenience-store stress population (2026-10-28)

The world now configures **20 extra initial zombie candidates: 16 normal and four Zombie_002 runners**, scattered across the existing site parking aisles, store flanks and back-of-store area. This is an **authored WORLD spawn placement test**, not a new entity class or AI system. Both top-down and first-person scenes inherit it. Existing default world placement (nine slow, one runner) remains, for a nominal 30 zombies in the village when optional horde is zero and all ground/body checks pass. Use `WorldMaster.tres/StoreStressZombiesEnabled=false` and restart the scene to compare performance. Candidates blocked by terrain, actor/player clearance or patrol corridor continue to be skipped and logged as before. AI cadence, wander/vision, combat, ragdoll and corpse systems are unchanged; increased actor load may expose known navigation limitations. Local acceptance pending.

## PER2A — Alerted tracking (2026-10-28)

Implemented in source, awaiting Godot/.NET build and in-world checks. During unaware wandering, the initial 160° forward cone and 22.5m detection range still apply. Once chasing or investigating, the 160° cone no longer rejects the player running behind the zombie: clear `VisibilityResolver.CanSee()` head-or-torso LOS within the existing 28.75m retention range remains mandatory. Full cover (rocks, walls, cars) still ends visual pursuit, freezing the last confirmed position for PER2; reappearing behind during active investigation allows reacquisition. Finished investigations revert to normal front-cone acquisition. F7 cone is the **initial acquisition cone**, not a representation of alerted 360° attention.

No other AI state, per-frame loop, timer, ray type, attack, weapon or sound handling was changed. **Hit-triggered investigation was specifically deferred at user request and is not part of this commit.** Test normal and runner from both viewpoints, including circling behind after initial detection and hiding completely behind solid cover. Godot C# compile and gameplay checks are pending locally.
