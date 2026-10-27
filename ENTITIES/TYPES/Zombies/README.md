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

## Building placement and trunk collision

Zombies now collide with pine trunks via physics layer 5. This prevents walking through trees; movement remains a direct slow approach, without navigation around obstacles. Spawn locations receive small pine clearings. The robot does not collide with this trunk layer.

## Ground penetration diagnostic / bounded recovery

A screenshot reported a zombie sinking near the original hill. Source inspection confirmed FULL_GAME Terrain3D collision (mode 3), terrain layer 1 and zombie mask 23 including terrain. No direct position writes occur during normal zombie movement, only MoveAndSlide. The exact runtime failure has not been reproduced or identified; this follow-up is a defensive recovery/diagnostic, not a confirmed underlying terrain fix.

WorldController now supplies an optional terrain-height delegate to InitializeTarget; zombie behavior still has no Terrain3D type dependency. On its existing staggered 0.4s physics brain timer, only an airborne/falling zombie samples that height. If its feet have sunk more than GroundRecoveryDepth (default 0.75m), it casts one cached ray around the expected surface and checks capsule clearance. Only a walkable solid hit plus a clear destination permits recovery; velocity/interpolation reset afterward. No surface/blocked destination means no teleport. One warning per actor records its world position, terrain height, mask and recovery result for local diagnosis.

Ordinary falls from the cliff are above lower terrain, so this check does not catch/hold them in the air. Terrain holes/missing regions return NaN and are left alone. The optional height contract represents open terrain: do not supply it unchanged to future underground/cave actors. GroundRecoveryEnabled/Depth live in ZombieMaster; callers without a sampler keep prior behavior. No new timer/frame callback, no idle ground queries or whole-world scan. Existing collision masks and movement settings remain unchanged.

Validation: source guards, masks, optional-call compatibility, normal cliff-fall versus below-ground conditions and whitespace reviewed. Godot/.NET unavailable: original issue reproduction, engine physics queries and runtime recovery remain unverified. Pull/build and F6 the world; walk zombies across the hill foot, along rocks and down cliff edges. If sinking repeats, capture Output's `Zombie ground penetration` warning with the location/result. No terrain rebake or edits.

## PER0 / PER1 — frontal eyesight and crouch cover

Implemented, awaiting local build/runtime validation. Existing staggered physics brain timer now gates by horizontal range and **160° forward cone**, oriented with VisualRoot (-Z), then queries shared VisibilityResolver at most twice: head first, torso only if blocked. Actual transitioning player capsule height supplies samples (standing head 1.7m, crouched 1.3m). Clear either sample means visible; crouching in the open does not grant invisibility. Height-sensitive cover around 1.45m is useful for testing. Both top-down and inherited FPS use this same physics logic.

DetectionRange 18m acquires, LoseRange 23m retains only with cone and clear LOS. At loss of sight, pursuit drops at the next sight decision and the zombie resumes wandering; it does not keep tracking a hidden live position. Last-seen investigation remains PER2. No hearing; firing behind a zombie does not alert it. Robot scanner/beam unchanged.

Tune ZombieMaster.tres: VisionAngleDegrees, EyeHeight (1.6m), SightIntervalSeconds (0.4s, minimum 0.1s), SightBlockerMask (17 = solid world layer 1 + trunk layer 5), plus existing ranges. Player/zombie layers 2/3 and occlusion Areas layer 8 are not sight blockers. Self and target physics RIDs are excluded. Terrain, walls, car and collidable rocks block. **Glass currently shares wall bodies and blocks sight too**; opaque classification/glass separation remain PER3. Grass/canopies do not block.

The world installs DEBUG/Perception/ZombieVisionDebug: faint red eye-height horizontal cone sectors follow actual visual heading. **F7** toggles all; ShowVisionCone controls initial visibility. The fan expands to the current acquire/retain range; arms-out means acquired. This is potential FOV, intentionally drawn through scenery for debugging, not an occlusion-clipped silhouette or in-world scan beam. Cover inside the cone can still block sight. It adds no callbacks/rays per frame; meshes follow parent transforms. Disabled/deactivated cones hide, death/unload frees them. One world-local input handler retains explicit actor references and prunes freed actors on toggles.

Cost: existing timer only while actor active, cheap gates before up to 2 LOS rays/check (12 actors at 0.4s gives at most ~60 sight rays/s before gating; not measured). Waiting active zombies still think, inactive actors stop entirely. No extra timers, target scans or new physics callbacks.

Brief local checks: build, F6 World and FPS; approach from behind vs front, check open crouch remains visible, crouch behind medium solid cover, full wall/ridge blocks and stepping out reacquires, arms lower after loss, F7 hides/shows cones, death/unload clean, robot unchanged. Godot/.NET unavailable here; runtime checks remain outstanding.
