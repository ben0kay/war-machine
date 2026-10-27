# Zombies

Reusable mobile actors with basic incoming hit support inheriting `ENTITIES/Shared/Entity.tscn`. `Zombie.tscn`, `C/ZombieController.cs`, original procedural low-poly artwork and `Configuration/ZombieMaster.tres` all belong here, not inside the preview world. World placement calls `InitializeTarget(Node3D player, uint seed)` after spawning on safe ground. This keeps zombies independent of TopDownPlayer and Terrain3D.

Default wandering speed **0.65m/s**, chase **1.15m/s**, wander radius **10m**, detection **18m**, lose range **23m**, stop **1.3m** from the player. Each actor copies its settings through EntityCore. Idle wandering arms hang low; pursuit raises both arms forward and keeps them extended near the player. Short asymmetric arm sway, alternating steps and subtle bob create a slow shamble. Mesh and material resources are shared across instances.

Range decisions use an individually staggered, one-shot **0.4s timer** with an explicit player reference, no global searches or raycasts. Wandering includes random 1–3s rests. Active motion/falling uses CharacterBody3D and one gait callback; grounded stationary actors stop physics and animation, retaining only the range timer. Deactivate stops that timer and all processing through EntityCore; scene unload owns cleanup. Particle/audio/attack loops are absent. This is a small active prototype population, not an unbounded crowd/streaming manager.

Awareness is deliberately distance-only: no LOS, crouch benefit, cover, memory, attacking, zombie/robot targeting or navigation/pathfinding. Zombies stop near the player and cannot hurt them. Collisions can block a direct route; stuck wandering picks another goal. Larger crowds or obstacle-rich worlds will require deliberate navigation/relevance work later.

## ZW0 — Basic wander and converge

**Implemented, Awaiting Local Validation.** Authorized scope: reusable zombie entity and six top-down preview placements, slow random wandering, range-based pursuit, arms-out chase pose. Code owner: Codex/ChatGPT Work; brief local build/appearance checks: owner/Copilot. Existing robot and FPS scene preserved. No later zombie/combat phases authorized.

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
