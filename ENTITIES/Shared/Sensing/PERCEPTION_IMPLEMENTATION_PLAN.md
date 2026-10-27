# PER — Shared perception, crouch cover and species-specific sensing

> **Status: PROPOSED / DOCUMENTATION ONLY. No PER phases have been implemented by this plan.**
>
> **Target branch:** `implement-td0-td1`. **First gameplay target:** zombies detecting or losing a crouching player behind genuinely solid cover in `WORLD/World.tscn` and its inherited FPS test. **Long-term consumers:** robots, zombies, deer, wolves, humans and other entities.
>
> **Codex handoff:** Read root `README.md`, `CodePreferences.md`, `ENTITIES/Shared/README.md`, `ENTITIES/Shared/Sensing/README.md`, `ENTITIES/TYPES/Zombies/` sources, `ENTITIES/TYPES/Robots/Robot_001/RobotController.cs`, `ENTITIES/TYPES/Robots/Shared/Scanning/`, `PLAYER/PlayerController.cs`, `WORLD/WorldController.cs`, `WORLD/World.tscn`, collision-generating structure/car/rock code and this document at **current HEAD**. Do **only the specifically authorized phase(s)**, in a single focused commit per job; default validation is brief.

## 1. Design principle: share the geometry, not the personality

The **perception mechanics** (distance, view cone, physics occlusion, dynamic target body samples, detection events and optional memory) should be reusable. **Sensing mode and reaction are species-specific:**

| Observer | How it senses | Specific presentation | Reaction (scope note) |
| --- | --- | --- | --- |
| Zombie | Natural eyesight from a head/eye origin; **hearing later** | **NO visible scan beam** | Chase, then briefly investigate last seen location |
| Robot_001 | Existing head-mounted **ScanSensor** with narrow mechanical cone | Keep existing independently animated **cyan ScanBeam**; possible red lock-on VFX is **future**, not part of PER0 | Preserve existing patrol/scan, don't silently add combat |
| Deer (future) | Natural wide-angle eyesight and hearing | No beam | Alert and flee, species-specific AI to be added later |
| Player/future humans | Optional vision and/or audibility | No mandatory new mesh nodes | Input/AI owns responses |

The robot should detect only when its **logical scanner** is pointing at a target and LOS is clear, independent of how the translucent beam is drawn. A zombie should not inherit a robot scan visual or robot stages. Eyesight and scanning can use the same occlusion queries while having different cones, socket orientations, timings, and target rules.

**No generic mandatory perception update in `EntityCore`.** Its existing activation/deactivation contract stays minimal; add optional capabilities to individual species. `PlayerController` is **not** an `EntityCore` and must still be a valid perception target.

## 2. Verified current implementation and gaps

- `ENTITIES/Shared/Sensing/ScanSensor.cs` **already exists** with registered candidates, range/cone filter, capped LOS rays, sensor events, physics Timer, round-robin candidate work, `EntityCore.ActiveChanged` integration and zero idle scan when empty/stopped. `ScanSensorSettings.cs` has Range, FullAngleDegrees, IntervalSeconds, RequireLineOfSight, RaysPerTick and CollisionMask. **Extend/reuse it; don't replace working robot scanning.**
- `ENTITIES/TYPES/Robots/Robot_001/RobotController.cs` creates `ScanSensor` and an independent `ScanBeam` under `SensorSocket`. Its visual beam should remain unchanged during first zombie rollout. Existing scanner presently samples `candidate.GlobalPosition`, not head/torso visibility points.
- `ENTITIES/TYPES/Zombies/ZombieController.cs` currently checks **horizontal squared distance only** in `Think()` on an initially staggered physics Timer (~0.4 s). It has no LOS, no cone, no visibility memory and no hearing. Preserve its wander/chase, health, ground recovery and activation behaviour.
- `PLAYER/PlayerController.cs` exposes `CapsuleHeight`, `IsCrouching`, `StanceChanged`; capsule height interpolates from about **1.8m standing to 1.4m crouched**. Eye/body target sample positions must derive from **current actual height** during the transition, not just the Boolean stance.
- Environment cover already has colliders: Terrain3D, many building walls, vehicles, trunks and a capped set of nearby rock colliders. Some **window panes are transparent visually but share a StaticBody3D/collision layer with opaque walls**. A naive collision-mask ray will regard such glass as cover. Fix classification deliberately; don't make invisible colliders or assume every physics object blocks sight.
- Existing zombie art visibly turns via `VisualRoot.Rotation`; its underlying CharacterBody3D root may not turn with it. Natural eyesight forward vectors must come from a stable **visual/head socket or explicit facing vector**, not blindly from body root basis.

## 3. Proposed folders and code responsibilities (create incrementally)

```text
ENTITIES/
  Shared/
    Sensing/
      README.md                             # current folder, document evolving contract
      PERCEPTION_IMPLEMENTATION_PLAN.md    # this proposal
      ScanSensor.cs                        # EXISTING logical robot scanner; preserve contract
      ScanSensorSettings.cs                # EXISTING robot scanner resource
      VisibilityResolver.cs                # PER0: pure-ish, bounded physics LOS helper
      PerceptionTarget.cs OR
      IPerceptionTarget.cs                 # PER0: optional target sample provider; prefer minimal interface
      VisionSensor.cs / VisionSettings.cs  # PER1 if useful: optional eyesight cone/timing
      PerceptionMemory.cs                  # PER2 if needed; small per-observer record, no manager
      HearingSensor.cs                     # LATER ONLY; not required for initial hiding
      SoundStimulus.cs                     # LATER ONLY; event-driven sound notification
  TYPES/
    Zombies/
      ZombieController.cs                  # observes shared results; decides chase/investigate/wander
      ZombieSettings.cs                    # readable Inspector species awareness settings
      Configuration/ZombieMaster.tres      # explicit defaults
    Robots/
      Robot_001/
        RobotController.cs                 # keep its separate scan stages and beam setup
      Shared/Scanning/ScanBeam*             # purely visual robot scanning, unchanged
    Wildlife/
      Deer/...                             # FUTURE example only; do not create for this work
PLAYER/
  PlayerController.cs                      # implement body sampling via a small optional capability
WORLD/
  WorldController.cs                       # only candidate wiring/proximity, never LOS math
```

These are **proposed files**, not a mandate to scaffold empty classes or add many managers. Avoid a second copied LOS implementation for robots/zombies. A separate `VisionSensor` should share `VisibilityResolver` and a minimal scheduling contract; do not force ScanSensor into a generic inheritance framework unless useful for the requested first pass.

### Core API/contract suggestions (illustrative; adjust for existing types)

- `IPerceptionTarget` or small `PerceptionTarget` component exposes current world-space head and torso samples; player uses current capsule/stance height. Other species can supply different heights (deer grazing head, crawling zombie, large robot).
- `VisibilityResolver.TrySee(observerOrigin, candidate, sightProfile, physicsSpace, cachedRayQuery)` answers *geometric visibility* with cheap squared-distance and cone checks already done by caller. Return `Visible`, visible sample count or best point, and sample position only when useful. **No AI state or sound logic here**.
- Sight query uses the observer's actual eye/socket world position, current target sample positions, cached excludes for self and correct target handling; first ray aims at head, then (only if blocked) torso. In physics-frame context use `PhysicsDirectSpaceState3D.IntersectRay`. One unobstructed sample means **potentially visible**; two blocked samples mean **occluded**. Standing/crouching in open terrain must remain visible.
- Prefer a **blocker-only query mask** (opaque cover/terrain) with owner and target excluded, so *no blocker hit* means visible. Alternatively, when using a mask including the target, explicitly handle target collider and the sight point. Never treat every ray hit as opaque without understanding collider layering.
- `VisionSensor` handles candidate sets, range, viewing cone, bounded interval and visible/not-visible events; `ScanSensor` keeps its existing externally used API and uses the same resolver. Observer-specific logic interprets events. The sensor must not alter robot beam colour/stages or force zombies to glow.

## 4. Correct blocker classification

A visibility query is **not the same as a movement collision test**. Recommended: select a dedicated named **VisionBlocker 3D physics layer** (or an intentionally constrained existing opaque-world mask for the first proof) and make it **additive** to existing physics layers: Terrain3D, opaque walls, large rocks, vehicle bodies/doors, solid tree trunks. Do **not** remove current player/zombie movement or weapon collision bits.

- **Glass:** preferably separate glass collision onto an independent `StaticBody3D` with normal physical collision as needed but **without VisionBlocker**. Merely assigning a different layer to one `CollisionShape3D` on a shared `StaticBody3D` will not work—the body owns collision layers.
- Vegetation leaves/grass and decorative tiny rocks should not automatically provide full cover, even when drawn over a character. Larger solid rock colliders can provide cover; no full-resolution per-rock triangle meshes.
- Account for cover at true 3D heights: a short crate may hide crouched head/torso while a standing head remains above it. Slopes use world coordinates and actual Terrain3D collision.
- Don't change global collision mask naming/allocations without inventorying current `Player.tscn`, `Zombie.tscn`, rifle projectile/raycast, robot and scene-layer use. Document and test the chosen bit. For PER1 a **known opaque building wall/low test crate** is enough to demonstrate correctness; the broader environment classification can happen in PER3.

## 5. Awareness and memory are separate from geometry

- Zombies: cheap distance gate, optional eyesight cone facing their visual heading, then LOS. Loss of sight should not keep querying the player's precise position as a chase goal. Store **last verified seen location** only; chase when visible, **investigate last seen position** briefly after losing sight, and return to wandering on timeout.
- Initial suggested values only: Zombie range ~18m, full visual cone ~160–180°, physics sight decision every **0.4 s**, last-seen search ~3 s. Keep existing 23m `LoseRange` as hysteresis if useful; **range alone must not override occlusion**. Expose defaults in `ZombieMaster.tres` and keep current behaviour predictable.
- ScanSensor: preserve narrow robot head scanner full cone (existing 35° authoring default) and independent beam. Only logical visibility shared; **do not convert the scanner to zombie eyes**. Robot pursuit/red alarm, damaging entities, and zombie-vs-robot fights are not authorized by this plan.
- Hearing **future**: event-driven noise emission with position, magnitude/category and approximate target location. Gunshots/footsteps can trigger `HeardSound` without LOS; listener investigates sound position, **not clairvoyant live player coordinates**. Species hearing distance/threshold differs from visual range. Don't implement hearing in the initial crouch/LOS stage.

## 6. Performance and event lifecycle

- Keep existing zombie staggered `Think()` **physics Timer**; avoid adding a second timer per zombie unnecessarily. Robot ScanSensor already has a bounded physics Timer and stops with no targets/when off.
- Broad phase is **registered nearby candidates → squared distance → facing cone → 1 or 2 raycasts** (second only when head blocked). No full-world scene scans or per-frame raycasts.
- Reuse `PhysicsRayQueryParameters3D`/exclusion lists where safe; avoid allocating new materials, nodes, ray parameters, arrays or LINQ in tick hot paths. Physics hit-result dictionaries may still allocate internally: don't claim the path is fully allocation-free.
- Keep independently bounded `RaysPerTick` and interval per observing species. For the first 12 zombies at ~0.4 s, max 2 LOS rays per check is about **60 rays/s** in an all-candidates-visible-range worst case, before more gating; *illustrative arithmetic, not a measured performance claim*.
- Important: 2 sample rays for one target consume 2 units of budget, not 1. When budget prevents testing a candidate, classify the result as **unknown/deferred**, not invisible: don't spam `TargetExited` or reset alert state due to skipped checks. Round-robin fairness as counts scale.
- `EntityCore` remains inert by default. Wake on explicit activation or nearby candidate registration, stop timers and clear/unregister on deactivation, death, despawn/chunk unload and tree exit. FPS/top-down must share the same 3D physics visibility result without view-specific rewrites.
- For future hundreds of agents, consider optional spatial candidate registration or one shared budget scheduler **after profiling**. Do not introduce either for the small proof without evidence.

## 7. Phased implementation — Codex may implement only approved passes

| Pass | Work | Status |
| --- | --- | --- |
| **PER0 — shared contract** | Inventory existing ray masks/opaque colliders; introduce target height sampling and `VisibilityResolver`, a no-AI line-of-sight helper. Preserve `ScanSensor` behaviour and public hooks. | Not started |
| **PER1 — zombie vision/cover** | Extend zombie `Think()` with cheap gating and ray checks (through reusable resolver), species settings, realistic crouch detection behind solid cover, preserved health/movement. Reuse zombie timer. Keep robot logic/visuals unchanged. | Not started |
| **PER2 — last seen** | Add short last-known-position investigative behaviour, no instant clairvoyance through walls and no navigation rewrite. | Not started |
| **PER3 — obstruction polish/debug** | Audit and fix relevant glass/solid collision layering, verify vehicle/rocks/tree/terrain cover, add **optional dormant** LOS debug visual (head/torso blocked/clear) in DEBUG; no always-on draw loop. | Not started |
| **PER4 — scanner integration** | When explicitly requested, adapt existing robot `ScanSensor` to the shared target samples/visibility helper **without changing mechanical beam/patrol or enabling combat**. Verify range/cone and no robot regressions. | Not started |
| **PER5 — sound (future)** | Separate `HearingSensor` and event-driven sound stimuli; species-specific responses. Do not implement as part of initial LOS prototype. | Future |
| **Deer / other wildlife** | Optional Vision/Hearing capability with different cone, eye sockets and flee behaviour. No deer assets or AI created by this plan. | Future |

Start with **PER0 + PER1** for the useful crouch/cover proof. Consider PER2 and PER3 after user feedback. Keep PER4 separate to avoid accidentally changing the scary robot's current scan behaviour.

## 8. Minimum functional tests and acceptance

Brief build/smoke checks at the active `WORLD/World.tscn` and inherited `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` (if runnable):
- Open ground: standing and crouched player both visible in the allowed cone/range.
- Medium cover: standing head visible, crouched head/torso hidden.
- Full building wall and Terrain3D ridge: sight blocked; moving out of cover reacquires.
- When a window is intended transparent, LOS sees through even if the pane remains physically collidable.
- Nearby partial-body reveal: clear head OR clear torso means detectable.
- Zombie faces away: no ordinary visual acquisition outside the configured cone; taking a shot should not silently enable hearing.
- After initially seeing then losing target: last-seen location, not player's current hidden location (PER2).
- Robot beam, scanner cone/visual and patrol unchanged in PER0–PER3.
- Sleep/unload/reactivation: no stale candidate references/timers. Compare FPS behaviour and idle versus active cost, don't claim measured gains without Godot profiling.

**Do not rewrite the procedural buildings, physics gameplay, rifle, robot scanning artwork, save system, FPS camera or overall world hierarchy.** Keep all added C# code under the appropriate `ENTITIES/Shared/Sensing/` and species folders, README docs current, and push **one commit per authorized implementation job** to `implement-td0-td1`.
