# ENTITIES — Shared Actors and Robot Types

> **Status: RM0–RM5 implemented — Awaiting Validation.** Shared actor/patrol/sensor and [Robot_001 encounter](TYPES/Robots/Robot_001/README.md) are integrated into TestWorld. RM5 adds F9 tuning, stronger beam and optional mechanical audio. Owner confirmed the earlier core patrol/scan loop; new rendering/audio/UI and performance acceptance are pending.

This folder owns reusable **mobile world actors**: identity/lifecycle, movement composition, optional sensing, and type-specific behaviour/artwork. Our first proof is an **original large mechanical walker (Robot_001)** that walks a loop in `WORLD/testworld.tscn`, stops, turns its head, sweeps a **translucent cyan scanning cone** with moving scan bands, and resumes walking.

This is a **non-combat environment encounter** to validate scale, silhouette, movement, atmosphere and reuse. Robots do not attack, pursue, damage or spawn dynamically in the first milestone.

## Documents

- **[IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md)** — RM0–RM5, the authorized-by-phase implementation roadmap, ownership, configs, performance and acceptance.
- [War Machine game overview](../README.md)
- [CodePreferences](../CodePreferences.md)
- [Environment plan](../WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md) — independent WE phases; work here must not silently mark them complete.

## Ownership principles

- **`ENTITIES/Shared/`:** Minimal `Entity.tscn` base for *mobile* actors, shared entity identity/lifecycle, waypoint movement and optional nonvisual sensor maths. Composition first: only attach the capabilities an actor needs. Do **not** make every decorative prop, stationary object or projectile inherit this base.
- **`ENTITIES/TYPES/Robots/Shared/`:** Robot-only reusable systems such as articulated leg animation and holographic scanning visuals. The scan **visual** must be separate from the shared logical **sensor**.
- **`ENTITIES/TYPES/Robots/Robot_001/`:** One concrete, configurable robot scene, original provisional procedural C# meshes, its transform rig and RobotMaster. A later imported GLB should replace visuals without replacing patrol/sensor code.
- **`WORLD/`:** Owns TestWorld placement and reference waypoints, not entity behaviour. Use the existing saved terrain non-destructively.
- **`DEBUG/`:** Only optional F9 tuning/debugging. Reuse the existing panel rather than inventing a second always-running control system.

### Proposed convention

New folders/materials/scripts are created **only when their phase is authorized**. Major folder README and master config resources follow CodePreferences. Keep C# runtime logic readable, modular, inspector-exposed and dormant when inactive. Engine/addon source remains untouched.

**Implemented foundation:** [Shared/README.md](Shared/README.md) documents lifecycle, sockets, idle cost and deferred checks.

**Next step:** owner feedback on RM5 appearance, controls, audio and brief local performance checks. Implement **only** a phase the owner explicitly selects.
## Before Codex starts

The implementation plan includes **cross-phase modularity and performance guardrails**: idempotent actor activation/deactivation with event cleanup; event-driven proximity wake/sleep with hysteresis; one shared gait update and foot-target caching; cached procedural mesh resources and stable VisualRoot/SensorSocket transforms; separate logical sensor vs transparent beam (no full-world target scans); and bounded raycasts / shader overdraw. These are constraints on RM0–RM5, **not additional phases**. The root entity scene stays minimal; no generic entity manager, premature navigation system or new debug UI is required.

## Basic zombies — authorized follow-up

[TYPES/Zombies/README.md](TYPES/Zombies/README.md) owns the inherited Zombie scene, runtime-copied master, low-poly shamble and arms-out range pursuit. Six are placed in the top-down preview only. No combat or robot targeting. Implementation awaits local build/visual validation; the previously cancelled ZS combat sandbox stays cancelled.

## Proposed shared perception (PER)

[Shared/Sensing/PERCEPTION_IMPLEMENTATION_PLAN.md](Shared/Sensing/PERCEPTION_IMPLEMENTATION_PLAN.md) describes how to reuse the existing robot `ScanSensor` logic while adding reusable natural eyesight and crouch-aware cover detection for zombies, with optional hearing and future deer/wildlife integration. **Documentation only; no PER gameplay phase has been started.** The robot's cyan `ScanBeam` remains a robot-only visual component, not a generic sight indicator.

## Wildlife foundation — Deer (2026-10-28)

[TYPES/Wildlife/Deer](TYPES/Wildlife/Deer/README.md) now provides a passive, shootable deer prefab, inherited from the shared Entity scene rather than zombie behaviour. It has a one-time procedural animal silhouette and the existing `HealthComponent` / generic health-bar pipeline (120 HP). The active world has one configurable preview spawn near player spawn, grounded using the existing Terrain3D collision and cleared of some vegetation. **No deer AI, flee, herd, animation or spawning system** is implemented. See species README for visual/health tests and deferred Godot compilation.
