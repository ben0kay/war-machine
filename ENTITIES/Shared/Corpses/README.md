# Shared corpse falls — ZD0

**Implemented in source; awaiting compilation and local physics/visual validation.** Only normal zombie and runner 002 deaths opt in. No deer/bear/human integration, articulated skeleton ragdoll, pile slowdown, sinking, timed expiry or crowd budget is implemented here. [Phase plan](../../TYPES/Zombies/DEATH_RAGDOLL_IMPLEMENTATION_PLAN.md).

## Ownership and handoff

WorldController creates one dormant CorpseManager under SystemsRoot and an independent Corpses Node3D under PersistentEntitiesRoot, shared by World.tscn and the inherited FPS scene. Spawned zombies receive the manager explicitly. Unbound standalone zombie scenes retain removal on death; no autoload or global service lookup is introduced. The shared code knows nothing about zombie classes, joint names, terrain plugins or health/damage rules.

ZombieController latches the first lethal callback, captures the original VisualRoot world transform and the species adapter's joint rotations, then deactivates its brain/reactions/vision/gait and removes collision. A deferred main-thread handoff reparents that exact visual hierarchy into CorpseBody before freeing the old actor. Captured transforms/poses are reapplied after deactivation, preserving runner lean and spawn-seeded materials. No geometry rebuild, copied mesh, new visual palette or second damage calculation. Source HealthComponent retains its usual notification order and exits with its actor. Late hits/impacts cannot restart the actor.

ICorpsePoseAdapter exposes only normalized Apply(progress). ZombieCorpsePose caches six existing biped pivots and an optional head, captures their death-time rotations and supplies deterministic asymmetric limb relaxation for both variants. Future quadruped/skeletal adapters can be supplied with per-spawn CorpseSettings without changing manager/body code. Those species are not implemented.

## Active fall and shutdown

One temporary RigidBody3D with a simplified box proxy per fall contacts static collision layer 1 only. Its own layer is zero: it is not a target/blocker for actors, bullets, sight or camera queries. CCD, low initial push/angular velocity, damping, velocity clamps and a bounded travel envelope limit runaway motion. A sanitized horizontal death-hit direction supplies the fall hint; nonfinite/degenerate directions use an independent spawn-seeded fallback. Actor movement RNG is not consumed.

Physics drives root translation/rotation. The adapter relaxes cached joints only during _IntegrateForces; no scene searches, mesh/material creation or per-limb rigidbodies occur during the fall. After minimum fall time and stable velocity, or at the hard 2.5s default timeout, one downward static-world ray checks nearby support, slope, oriented-proxy support depth and a sufficiently horizontal body. No upward roof snapping or Terrain3D edit/height injection. Unsupported/cliff/failed-upright/escaped falls are dismissed instead of freezing in mid-air or simulating indefinitely. A wall-blocked fall may therefore disappear; this bounded fallback still needs visual tuning.

Successful settle freezes the exact world pose, applies the final joint pose, disables the temporary shape and all collision, and stops all node processing. Settled bodies have no gait, AI, timers, support polling, ground queries or limb update callbacks. The manager is dormant even during falling; native bodies own their short active callbacks. Frozen visuals remain until world teardown in ZD0. The entire independent root is freed with the scene, including active falls.

**Phase boundaries:** settled corpses do not support new falls yet (pile proxies/contacts and soft slowdown belong to ZD1). They cannot trap actors. No five-minute lifetime or sink exists until ZD2; a long session can accumulate frozen visual resources until those later phases are implemented. ZD3 adds crowd/streaming budgets and profiling. The world streamer still owns only its four selected buildings, not these corpses. Frozen corpse persistence across world saves/unload is not claimed.

## Settings and validation

Configuration/CorpseMaster.tres owns the common enabled flag, proxy dimensions/pivot/mass, fall duration/stability, damping, modest push/spin and maximum travel. The manager and each fall use runtime copies; validation rejects unsafe/nonfinite authoring. Enabled=false restores disappearance on zombie death. No unused TTL/slowdown fields are created before their phases.

See [DEBUG/Corpses](../../../DEBUG/Corpses/README.md) for the provided actual-code test and brief local checks. Source/resource/whitespace checks only here; no Godot/.NET installed, so no successful compile, physics contact, settled appearance or performance measurement is claimed.
