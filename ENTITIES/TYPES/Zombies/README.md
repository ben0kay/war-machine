# Zombies

Reusable mobile actors with basic incoming hit support inheriting `ENTITIES/Shared/Entity.tscn`. `Zombie.tscn`, `ZombieController.cs`, original procedural low-poly artwork and `Configuration/ZombieMaster.tres` all belong here, not inside the preview world. World placement calls `InitializeTarget(Node3D player, uint seed)` after spawning on safe ground. This keeps zombies independent of TopDownPlayer and Terrain3D.

Default wandering speed **0.65m/s**, chase **1.15m/s**, wander radius **10m**, detection **18m**, lose range **23m**, stop **1.3m** from the player. Each actor copies its settings through EntityCore. Idle wandering arms hang low; pursuit raises both arms forward and keeps them extended near the player. Short asymmetric arm sway, alternating steps and subtle bob create a slow shamble. Mesh and material resources are shared across instances.

Sight decisions use an individually staggered, one-shot **0.4s timer** with an explicit player reference, no global searches; gated, bounded LOS raycasts. Wandering includes random 1–3s rests. Active motion/falling uses CharacterBody3D and one gait callback; grounded stationary actors stop physics and animation, retaining only the sight timer. Deactivate stops that timer and all processing through EntityCore; scene unload owns cleanup. Particle/audio/attack loops are absent. This is a small active prototype population, not an unbounded crowd/streaming manager.

Awareness now uses frontal eyesight and physical cover (PER0/PER1). No memory, attacking, zombie/robot targeting or navigation/pathfinding. Zombies stop near the player and cannot hurt them. Collisions can block a direct route; stuck wandering picks another goal. Larger crowds or obstacle-rich worlds will require deliberate navigation/relevance work later.

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

DetectionRange 18m acquires, LoseRange 23m retains only with cone and clear LOS. At loss of sight, pursuit drops at the next sight decision and the zombie resumes wandering; it does not keep tracking a hidden live position. **PER2 remains planned** and now specifies randomized per-zombie partial/full investigation of the last *verified* seen position, individual speed/distance-aware give-up timers and a brief look-around for thorough seekers. It has **not** been coded; without future navigation, buildings may physically block the goal. **HE0/HE1 hearing is also planned, not active**; even if the player hears a rifle shot via the audio pool, firing behind a zombie does not currently alert it. Robot scanner/beam unchanged.

Tune ZombieMaster.tres: VisionAngleDegrees, EyeHeight (1.6m), SightIntervalSeconds (0.4s, minimum 0.1s), SightBlockerMask (17 = solid world layer 1 + trunk layer 5), plus existing ranges. Player/zombie layers 2/3 and occlusion Areas layer 8 are not sight blockers. Self and target physics RIDs are excluded. Terrain, walls, car and collidable rocks block. **Glass currently shares wall bodies and blocks sight too**; opaque classification/glass separation remain PER3. Grass/canopies do not block.

The world installs DEBUG/Perception/ZombieVisionDebug: faint red eye-height horizontal cone sectors follow actual visual heading. **F7** toggles all; ShowVisionCone controls initial visibility. The fan expands to the current acquire/retain range; arms-out means acquired. This is potential FOV, intentionally drawn through scenery for debugging, not an occlusion-clipped silhouette or in-world scan beam. Cover inside the cone can still block sight. It adds no callbacks/rays per frame; meshes follow parent transforms. Disabled/deactivated cones hide, death/unload frees them. One world-local input handler retains explicit actor references and prunes freed actors on toggles.

Cost: existing timer only while actor active, cheap gates before up to 2 LOS rays/check (12 actors at 0.4s gives at most ~60 sight rays/s before gating; not measured). Waiting active zombies still think, inactive actors stop entirely. No extra timers, target scans or new physics callbacks.

Brief local checks: build, F6 World and FPS; approach from behind vs front, check open crouch remains visible, crouch behind medium solid cover, full wall/ridge blocks and stepping out reacquires, arms lower after loss, F7 hides/shows cones, death/unload clean, robot unchanged. Godot/.NET unavailable here; runtime checks remain outstanding.

Zombie_002 is now explicitly placed by WorldController in both world views: three separate RunnerSpawnOffsets in WorldMaster, retaining the 12 slow placements. Native grounding, tree/rock exclusions and F7 debug registration reuse existing placement. Runner behavior/stats remain those of its prefab; local runtime validation pending.

## Missing collision at horde spawns — awaiting local validation

Owner logs show multiple horde actors falling below terrain height ~4m with no solid surface found by recovery. Actual native collision failure is not yet reproduced. World now explicitly sets runtime FULL_GAME coverage and rebuilds after terrain data/sculpt and camera binding, before actors activate.

Actors with an open-terrain sampler wait at spawn until the existing staggered physics brain timer confirms a walkable collider within 0.6m of expected terrain height. Gravity/movement sleep until confirmation; one warning identifies missing ground and that timer retries. This guards startup falls without inventing collision or teleporting through geometry. Persistent warnings mean actors remain waiting and native collision still needs diagnosis. Later penetration recovery stays unchanged. Placement resets interpolation. No new timer/frame loop or scans; one startup ray per actor, low-rate retries only while waiting. Future cave spawns must omit/replace the open-terrain sampler.

Source/loop review only; compilation not verified (Godot/.NET unavailable). Build and F6 World: inspect all 20 horde spawns, chase over their ground, slopes/cliff edges, inspect Output for spawn-wait/penetration warnings, then check FPS. Runtime success and the underlying collision failure remain awaiting local validation.

## CM1 — nonlethal bullet reactions

Shared ZombieController now accepts IImpactReceiver Bullet contacts after damage, provided alive/active and safely spawned. Rifle/MG briefly damp movement with additive recoil; both species inherit, runner starts with 0.1 impact resistance. ZombieMaster/RunnerMaster expose reaction enable/resistance/threshold/duration/retrigger/recovery/motion/pose settings. Active hits never extend the reaction; recovery gaps prevent permanent stun-lock. Existing health/damage, goal sensing, collision/gravity and gait remain. Death/deactivate clears state/pose. No new callback/timer/query: existing physics wakes during reaction/recovery then can sleep again. See ../../../COMBAT/README.md for API/defaults and brief local checks. Compilation/visual validation pending.
