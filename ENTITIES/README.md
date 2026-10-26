# ENTITIES — Shared Actors and Robot Types

> **Status: planning only.** No ENTITIES gameplay scripts, scenes, meshes or configuration resources have been implemented yet.

This folder will own reusable **mobile world actors**: identity/lifecycle, movement composition, optional sensing, and type-specific behaviour/artwork. Our first proof is an **original large mechanical walker (Robot_001)** that walks a loop in `WORLD/testworld.tscn`, stops, turns its head, sweeps a **translucent cyan scanning cone** with moving scan bands, and resumes walking.

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

**Next proposed phase:** RM0 architecture/data model, followed by RM1 robotic silhouette. Implement **only** a phase the owner explicitly selects.
## Before Codex starts

The implementation plan includes **cross-phase modularity and performance guardrails**: idempotent actor activation/deactivation with event cleanup; event-driven proximity wake/sleep with hysteresis; one shared gait update and foot-target caching; cached procedural mesh resources and stable VisualRoot/SensorSocket transforms; separate logical sensor vs transparent beam (no full-world target scans); and bounded raycasts / shader overdraw. These are constraints on RM0–RM5, **not additional phases**. The root entity scene stays minimal; no generic entity manager, premature navigation system or new debug UI is required.
