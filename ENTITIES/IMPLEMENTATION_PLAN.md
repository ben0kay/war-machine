# RM — Reusable Entity, Mechanical Walker and Scanning Prototype

> **State:** **RM0–RM5 implemented, Awaiting Validation.** Owner confirmed patrol/scanning after grounding correction. Latest job authorized RM5 plus organic cave noise; new visuals/controls/audio still need local acceptance.
>
> **Purpose:** In the existing Godot C# **War Machine** project, add one memorable non-hostile, original giant mechanical walker that **walks → stops → scans → resumes** in the real `WORLD/testworld.tscn`. Build small shared components so future robots and other entity types can reuse movement, waypoints, sensing and visuals where appropriate.
>
> **Project rules:** [Root README](../README.md) · [CodePreferences](../CodePreferences.md) · [ENTITIES ownership](README.md). This plan is separate from the environment-only [WE roadmap](../WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md).

## Current art direction — 2026-10-26

Owner superseded the provisional quadruped with an original heavy **two-legged** cyan scanner: reinforced articulated legs/feet, broad armour, and two cyan-lit components flanking the head. Implemented in RobotMeshBuilder/RobotMaster with alternating biped gait and forward knee directions. Generic scene inheritance, patrol/sensing and head-emitted beam are preserved. MechanicalLegs still supports future four-legged art. Historical RM1 four-leg requirements below describe the earlier prototype; this paragraph is the current artwork contract. Build/render/gait/pod-clearance acceptance is pending locally. Cave exterior also receives a hillside-specific material in the same authorized job; interior and cave generation are unchanged.

## Phases at a glance

| Phase | Goal | Recommended assistant | Status |
| --- | --- | --- | --- |
| **RM0** | Minimal shared Entity base, folders, configs and reusable actor/component contract | Codex / ChatGPT Work; Regular Chat can review design | **Awaiting Validation** |
| **RM1** | Robot_001 procedural hard-surface model and mechanical stepping | Codex / ChatGPT Work | **Awaiting Validation** |
| **RM2** | Shared waypoint patrol and stop/turn/dwell lifecycle | Codex / ChatGPT Work | **Awaiting Validation** |
| **RM3** | Shared logical scan sensor and Robot-only cyan 3D beam effect | Codex / ChatGPT Work | **Awaiting Validation** |
| **RM4** | Integrate an end-to-end walk/stop/scan loop into TestWorld | Codex / ChatGPT Work | **Awaiting Validation** |
| **RM5** | Performance, reusable authoring, F9 controls and visual/audio polish | Codex / ChatGPT Work, Copilot local checks | **Awaiting Validation** |

**State vocabulary:** Planned → In Progress → Implemented → Awaiting Validation → Accepted. An implementation commit or a headless compile is not visual acceptance. **Do not automatically start another RM phase.** If the user authorizes the full RM0–RM4 feature as one job, complete only that explicitly scoped job; otherwise implement the selected phase and stop.

## 1. Confirmed current state and boundaries

As inspected before this plan:

- **RM0 now exists:** `Shared/Entity.tscn`, `Shared/C/EntityCore.cs`, `Shared/C/EntitySettings.cs`, `Configuration/EntityMaster.tres` and `Shared/README.md`. RM1 adds Robot_001 scene/controller/settings/mesh builder/master and shared MechanicalLegs. RM2–RM4 now add shared Movement/Sensing, robot Scanning and WORLD/RobotEncounter.cs/.tscn, integrated in testworld. RM5 remains proposed.
- `WORLD/testworld.tscn` is the **actual project main scene**. It has Terrain3D terrain, Sky3D day/night and thick distance mist, vegetation/grass/pines and small decorative content, a cave, weather integration, F9 tuning and the FPS display.
- `PLAYER/LegacyFirstPerson/ExplorationController.cs` extends `CharacterBody3D` and supports a free-flight camera plus optional collidable **first-person movement via F6**. Do not rewrite it to give the robot movement. Use it only as an optional future sensing target/debug reference.
- The main landscape has authored/saved Terrain3D data. **Do not rebake, replace, overwrite or regenerate terrain** to make a robot patrol.
- Existing fog and wind are controlled by `ENVIRONMENT/`; use them visually and never introduce a second WorldEnvironment/Sky3D owner.
- The owner requested a reusable foundation, **not** an attack AI, health/damage system, inventory or world population system.

**Experience target:** In a fairly flat meadow area, the player sees a large, dark, industrial mechanical walker partly silhouetted in the fog. It steps heavily between a few landmarks, settles, slowly sweeps a glowing **cyan, cone-shaped 3D scanning volume** from its head with animated bands/scanlines and a faint ground footprint, then continues the route. Make the visual *legible in mist* without blasting the whole screen white/cyan.

**Original artwork only:** The movie is mood/scale inspiration. Do not copy a recognizable exact robot design, branding or film asset.

## 2. Proposed layout and ownership

Use these conventions to match current `WORLD/Content/.../C` + `Configuration` and master-resource patterns. `Robots` and `Robot_001` use that capitalization — **not** `ROBOTS`.

```text
ENTITIES/
├── README.md
├── IMPLEMENTATION_PLAN.md
├── Configuration/
│   └── EntityMaster.tres               # Shared activation/behaviour defaults
├── Shared/
│   ├── Entity.tscn                     # Minimal common mobile actor base
│   ├── C/
│   │   ├── EntityCore.cs              # Actor identity, lifecycle and hooks
│   │   └── EntitySettings.cs          # Typed shared Resource config
│   ├── Movement/
│   │   ├── PatrolController.cs         # Optional waypoint patrol component
│   │   └── PatrolSettings.cs           # Path/speed/wait behaviour
│   └── Sensing/
│       ├── ScanSensor.cs               # Optional reusable cone maths / LOS
│       └── ScanSensorSettings.cs       # Range, angle, tick budget
└── TYPES/
    └── Robots/
        ├── README.md
        ├── Shared/
        │   ├── Movement/
        │   │   └── MechanicalLegs.cs     # Visual joint stepping only
        │   └── Scanning/
        │       ├── ScanBeam.tscn        # Robot scanner VFX scene
        │       ├── ScanBeam.cs          # VFX lifecycle/parameter binding
        │       └── ScanBeam.gdshader    # Cyan translucent banded volume
        └── Robot_001/
            ├── README.md
            ├── Robot_001.tscn
            ├── C/
            │   ├── RobotController.cs  # Orchestrates patrol & scan
            │   └── RobotMeshBuilder.cs # Optional procedural art generator
            ├── Configuration/
            │   └── RobotMaster.tres
            └── Artwork/
                └── Generated/          # Saved generated art if appropriate
```

This is a **target layout, not an instruction to create every empty folder immediately**. Use readable folder READMEs wherever a real feature is added. Avoid `EntityManager`, `RobotManager` and additional registries until there is a demonstrated need.

### Architecture decision: base scene vs components

- For **mobile world actors**, prefer `Entity.tscn` as a small reusable `CharacterBody3D` scene with an `EntityCore` C# root, identity/config, minimal collision/activation hooks. `Robot_001.tscn` may be **inherited from** that scene, with its concrete robot controller deriving from the shared C# core if needed.
- Do **not** require every future entity to use this physics body: stationary effects/props and special entities may simply **compose** reusable shared components instead. “Shared scene” should not become a giant mandatory god object.
- `PatrolController` is **generic**: waypoint selection, travel/turn/arrival/dwell status, no knowledge of pine trees, beams, attack AI or procedural limbs.
- `MechanicalLegs` drives only visual hips/knees/feet/body response, from actor velocity and optional terrain/foot probe data. Walking logic should not animate the armature itself.
- `ScanSensor` is nonvisual, generic optional sensing: cone distance/angle and **budgeted** line-of-sight tests; no damage or chase. It should function with no beam at all.
- `ScanBeam` is **robot visual hardware** only: show/hide, head attachment, scan band speed, colour/range/angle; does not decide detection.
- `RobotController` is a *thin coordinator* of patrol state and scanner activation. Prefer signals, timers and explicit state transitions over a global behaviour tree for this one prototype.

### Data vs nodes

Use C# `Resource` settings stored as `.tres` master files with exported/inspector groups, e.g. generic `EntityMaster` plus per-species `RobotMaster`. Do not duplicate configuration values across scripts, scenes and hardcoded shader uniforms. Runtime adjustable settings should be a **copy** of the master; do not inadvertently modify source `.tres`.

A future imported `.glb` should only replace the robot art scene/mesh hierarchy, using stable named sockets (`SensorSocket`, leg attachment transforms etc.), rather than changing patrol/detection code.

### Must-have implementation guardrails before RM0

These additions deliberately keep the first robot **small and expandable**. They are requirements on existing phases, **not new managers, services, phase IDs or permission to start work**.

1. **One optional capability per responsibility.** EntityCore provides identity, on/off and references only; patrol owns route choice and locomotion requests; MechanicalLegs owns visual gait; ScanSensor owns logical tests; ScanBeam owns its own drawing and shader. Use a handful of explicit methods/events, not a hierarchy of speculative interfaces or a single all-purpose AI controller. A second entity may omit legs, scanner or patrol entirely.
2. **Clear lifecycle contract, from the start.** Define `Activate()`, `Deactivate()` and component-specific `Start/Stop` (or equivalent names) with idempotent semantics. Deactivation must cancel timers, hide/disable the scan VFX, stop gait and sensory queries, disable processing and release relevant runtime subscriptions. `_ExitTree` must unsubscribe from player/terrain signals. Reactivation restores a valid stationary or patrol state without duplicated timers or stale beam visibility.
3. **Keep relevance checking event-driven.** The existing explorer emits `ViewPositionChanged`; use it (plus an initial placement check) as the **wake trigger** for a distant dormant robot, rather than polling all entities every frame. Use separate activate/deactivate thresholds with hysteresis so a viewer near the boundary does not repeatedly wake/sleep it. For the single-actor proof, it is acceptable to pause a far-away actor and resume sensibly; **do not** invent a full remote simulation, global scheduler, or background actor manager. If the robot itself moves toward a threshold while active, it can assess relevance during its already-active movement callback.
4. **Stable reusable art sockets.** Robot_001 defines `VisualRoot`, `BodyRoot`, `HeadPivot`, `SensorSocket` and explicitly named leg pivots. Movement changes the **physics root**; leg/head effects change the **visual rig**; scanning attaches to the **socket**. The procedural builder generates *mesh resources once per type/variant*, where feasible, and a later imported GLB plugs into VisualRoot without edits to patrol or sensor code.
5. **Physical robot vs visual gait.** Use one inexpensive locomotion collider/body, not full collision on individual legs. Animate four legs from **one active gait update**. Sample Terrain3D heights or short physics probes only when a foot chooses a new target/needs a correction, **not four fresh raycasts per frame while standing**. Cache foot targets and planted state; settle the pose, then stop visual updates.
6. **Budget the sensor; don't use the beam mesh as a hitbox.** A sensor should check only **explicit registered candidates** (the player is the optional initial target), first reject on squared distance and cone dot product, then perform bounded LOS raycasts only on surviving candidates. Suggested initial cadence is ~**5 Hz** when scanning, with a small maximum rays-per-tick and **zero** raycasts while inactive or when there are no candidates. No scene-wide `GetNodesInGroup` or physics-shape scan every frame. Scanning visuals must work even if logical detection is disabled.
7. **Limit transparent overdraw, not just triangles.** The translucent cyan beam is potentially more expensive than the robot mesh. Start with a short, low-segment cone and one shader-driven moving-band effect, **no actual per-pixel volumetric raymarch**, no beam light/shadow, and a cheap/optional ground footprint. Test with Sky3D's dense fog, tree foliage and different view angles; tune opacity before adding layers. Reuse mesh/shader/materials, with per-instance overrides only where distinct values require them.
8. **Boundaries and ownership.** Entity components receive references to their owner, route, optional terrain and scan candidate source when initialized; they must not hardcode `testworld.tscn` node paths. Reuse existing `DEBUG/EnvironmentTuning` rather than a separate UI or polling debug loop. Global EntityMaster is limited to settings genuinely shared among all mobile entities; RobotMaster owns species-specific scale, leg, patrol and beam defaults. Modify runtime copies, not saved masters.
9. **Proportionate verification.** At each phase, smoke-check the precise failure modes introduced: disabled vs active processing, duplicate timers/events, head socket alignment, foot planting, source resource preservation, shader transparency and 1 vs 2 instances. Do **brief source/build/scene checks**; detailed rendered/FPS profiling stays with the owner per CodePreferences unless explicitly requested.

**Suggested execution order:** RM0 establish the small lifecycle/art-socket contract; RM1 prove a stable silhouette and one gait updater; RM2 link movement via events; RM3 build and visually test the cone **before** implementing optional detection; RM4 integrate one near-ground loop; RM5 add F9 tuning and measure costs. No extra intermediate framework phases are needed.

## 3. RM0 — Shared entity foundation and contracts

**Objective:** Prepare a *minimal* reusable mobile-actor base and typed data contract, without accidentally building gameplay systems.

- **Dependencies:** Existing Godot C# project, `testworld.tscn`, CodePreferences; inspect actual scene/C# conventions before adding files.
- **Scope:** `ENTITIES/Shared/Entity.tscn`, minimal `EntityCore.cs` and `EntitySettings.cs`, `Configuration/EntityMaster.tres`, per-area documentation, planned actor lifecycle/activation contract; optionally stub a tiny event interface required for later components.
- **Out of scope:** Robot model, movement, scanner effect, health/damage, enemy factions, spawn tables, savegame integration, global tick scheduler.
- **Owning paths:** `ENTITIES/README.md`, `ENTITIES/Shared/`, `ENTITIES/Configuration/`; this plan.
- **Implementation:** Root as simple `CharacterBody3D` for moving actors; scene inheritance or composition for concrete types. Provide typed settings, stable owner references, **idempotent Activate/Deactivate and `_ExitTree` cleanup**, with no update loops by default. Make optional components attachable through exported NodePaths/typed references resolved once at initialization, not through repeated scene searches. Define the **VisualRoot/HeadPivot/SensorSocket** contract for later Robot_001 art without constructing a large mandatory tree.
- **Config:** Identity/type label, active/disabled defaults, activation distance or explicit enable toggle (only if actually used). Do not bake robot-specific numbers into EntityCore.
- **Preserve:** Entire TestWorld, authored terrain, environment and other entities unaffected.
- **Performance:** Spawn/enable explicitly wakes actor; disabled/sleeping actor does **zero unnecessary physics/process**, sensor or gait work. Canceled timers and disconnected signals do not leak callbacks. Relevance waking uses player movement events and hysteresis only when implemented; **no global actor manager**. Document idle costs.
- **Pass:** A minimal reusable entity scene/resources load/compile and can be instantiated/released cleanly; no visual/gameplay changes to TestWorld.
- **Assistant:** Codex for scene/C#/resource setup; Regular Chat can refine design/docs; Copilot local build checks.

### RM0 implementation handoff — 2026-10-26

Implemented the minimal capsule/VisualRoot actor, per-instance settings copy, idempotent lifecycle signal/hooks, exit cleanup and dormant process mode. No world files changed. No proximity subscriptions, unused radius settings, robot artwork or later-phase components were added. See [Shared/README.md](Shared/README.md) for exact contracts and local checks. Source/path review and `git diff --check` completed; dotnet/Godot are unavailable here, so build/instantiate/release checks are deferred. Pushed as `97aa35b7e5411d0facb486f1029b6dbaee1ed164` before the RM2–RM4 job. RM1 was subsequently authorized and implemented.

## 4. RM1 — Robot_001 visual walker and articulated legs

**Objective:** Make one **original, unmistakably large** mechanical creature that moves like an engineered machine rather than a floating rigid body.

- **Dependencies:** RM0 base; working player/terrain reference.
- **Scope:** `Robot_001.tscn`, dark angular sci-fi body, rotating sensor head with named `SensorSocket`, four articulated legs with hip/knee/foot joints, purposeful body weight/bob, industrial materials and restrained emissive details.
- **Out of scope:** Patrol decisions, beam, target detection, attack weapons, imported licensed characters, full IK framework/addon.
- **Owning paths:** `ENTITIES/TYPES/Robots/Robot_001/`, `ENTITIES/TYPES/Robots/Shared/Movement/MechanicalLegs.cs`, related README/config.
- **Approach:** Build initially from C# generated low-poly/medium-poly **original** meshes grouped in a stable transform hierarchy under **VisualRoot**: BodyRoot/torso/armour → HeadPivot → SensorSocket; named leg pivots → upper/lower leg → foot. Target about **10–12 metres tall** (starting art value only). Generate mesh resources once per reusable variant, not every frame or per footstep. Use procedural stepping/stance timing, short foot probes or cached Terrain3D height samples **when selecting new foot targets**, and a simple two-segment bend/IK solver only if needed for credible planted feet. Prefer a convincing 4-leg gait over fancy detail. Make a stationary walk-preview/debug animation possible; the patrol phase later provides actual motion.
- **Config:** Body/leg size, torso pitch/bob, stride length, gait speed, leg lift, foot-plant pause, head yaw limits, material colours, emissive strength, imported mesh override path.
- **Preserve:** Godot Jolt/terrain collision use proper capsule or low-cost approximations; **no mesh collision per articulated leg**. Do not modify `WORLD/Content` procedural pines or decorative-content scatterers.
- **Performance:** Update all articulated legs from **one** active robot gait driver, never per-leg `_Process`; cache planted targets, avoid per-frame stationary terrain raycasts and settle to a static pose when halted. Reuse generated meshes/materials and turn the gait driver off at rest/out of relevance. Robot root collision is coarse and separate from visual feet.
- **Pass:** Coherent full-body 3D shape, four distinct mechanically hinged legs and moving joints, stable leg pose when stopped, head independently rotatable, reasonable triangle/instance counts, replaceable art hierarchy.
- **Assistant:** Codex due to procedural geometry, transforms, rig math and scene wiring; Copilot can run local visual/sanity checks.

### RM1 implementation handoff — 2026-10-26

Implemented Robot_001 inherited scene, RobotSettings/master, cached primitive art builder, named head/sensor/leg rig, and one reusable two-bone gait driver. Includes explicit stationary preview, body bob/pitch, settling, coarse capsule, head yaw and visibility/lifecycle shutdown. ArtOverride supports an explicitly matching rig; arbitrary GLBs require a wrapper. Flat ground only by default; optional foot-target correction is injected, with no per-frame terrain queries. No patrol/scanner or world edits. Source/path checks and whitespace review passed; dotnet/Godot unavailable, so compilation and visual/runtime acceptance are deferred. See [Robot_001 handoff](TYPES/Robots/Robot_001/README.md). Pushed as `23224965710ee103671f7a6eeeeaf4fdbe1253d2` before the authorized RM2–RM4 job.

## 5. RM2 — Shared patrol and stop-state movement

**Objective:** Establish generic **Walk → Arrive → Settle → Wait → Next waypoint** behaviour reused by Robot_001 and future mobile entities.

- **Dependencies:** RM0; RM1 leg hooks (can use placeholder art while testing generic controller).
- **Scope:** Optional `PatrolController`, waypoint array/route resource or child marker transforms, loop route and deterministic arrival/turn logic; signal events for started walking, arrived, stopped and ready to depart.
- **Out of scope:** Global pathfinding framework, pursuit/combat, complex navigation mesh bake over 2 km, terrain deformation.
- **Owning paths:** `ENTITIES/Shared/Movement/` and Robot_001 assembly; TestWorld route marker positions only when RM4 authorizes world integration.
- **Approach:** Begin on an **intentionally flat, unobstructed meadow** with 2–4 reachable waypoints. Cache route/marker world positions when route initializes (and refresh explicitly if authored paths change). Kinematic movement with real world delta/velocity, acceleration/deceleration and heading turn rate; floor-snapping/gravity where appropriate. Drive body-relative gait via speed and signals, never direct limb writes. Expose `StartPatrol()`, `PausePatrol()`, `ResumePatrol()`, `StopPatrol()` with idempotent transitions. Store current waypoint and movement state when deactivated, restoring sensibly without teleport or replayed callbacks. Arrival signals should fire once per waypoint.
- **Config:** Speed (~2m/s to start), acceleration, braking, turning speed, arrival radius, wait/settle duration, route looping and optional waypoint dwell overrides.
- **Preserve:** Do not rewrite player input, Terrain3D collision, Sky3D or weather. Path markers are nonintrusive and editable in scene.
- **Performance:** Physics processing **only while moving/turning**; a Godot Timer handles stationary dwell rather than ticking waiting state. Timer must pause/stop on Deactivate, with no duplicate timeout subscriptions on reactivation. No per-frame world search or repeated path recalculation. Route markers and terrain references are injected/cached. Stop/disable really sleeps; resume wakes.
- **Pass:** Robot moves between points, rotates with feet aligned approximately to direction, stops without sliding, waits at each marker, loops predictably and restarts on command. No drifting on stop, timer leaks or stuck idle `_PhysicsProcess`.
- **Assistant:** Codex for reusable movement wiring/state signals; Copilot for path validation.

## 6. RM3 — Generic sensor and Robot scan-beam VFX

**Objective:** Build the iconic scanner as **two reusable and independently testable systems**: logical sensor and visual sci-fi projection.

- **Dependencies:** RM0 owner/components; RM1 head/`SensorSocket`; RM2 pause/dwell hooks recommended.
- **Scope — Sensor:** Optional `ENTITIES/Shared/Sensing/ScanSensor.cs` with cone origin/direction, length, half-angle, periodic target checks against explicit candidate list or physics query and optional raycast line of sight. Report enter/exit/seen events to a debug callback without triggering combat.
- **Scope — VFX:** `ENTITIES/TYPES/Robots/Shared/Scanning/ScanBeam.tscn`, `ScanBeam.cs`, `ScanBeam.gdshader`: translucent volumetric-looking cone or truncated cone, cyan/teal glow, soft feathered edges, scrolling scan-lines and one more pronounced scanning band. Cone sweeps with robot's sensor head left/right. Faint ground contact ring/footprint may use a simple mesh/raycast to **approximate**, not pixel-accurate project, contact.
- **Out of scope:** Raymarched real volumetric lighting, real cone collision, expensive many-target per-frame scan, laser damage, targeting/chasing, actual terrain decals that depend on undocumented addon support.
- **Owning paths:** `ENTITIES/Shared/Sensing/`, `ENTITIES/TYPES/Robots/Shared/Scanning/`; RobotMaster owns initial tuning.
- **Approach:** Reusable cylinder/cone mesh built once (local forward axis clearly defined, e.g. `-Z`). Transparent spatial shader uses UV/vertex position for radial/length falloff and scrolling cyan lines. Limit **screen coverage/overdraw** with short range, few rings/segments, no light or shadow casting; test transparent depth sorting/occlusion under Sky3D fog and foliage in Forward+. The sensor (optional during the first visual pass) checks cached/registered candidates with squared-distance and cone-dot rejection before occasional capped physics LOS rays, at roughly **5Hz active scan** initially. Never perform per-frame scene-tree target enumeration. **Beam shape is cosmetic and need not match irregular terrain surface exactly.**
- **Config:** Cone length (~45m), full spread (~35° starting point; define whether full or half angle), colour, brightness/opacity, line count/speed, gradient, ground footprint size, head sweep amplitude (~120°), sweep speed, scan duration (~6s), sensor interval/LOS budget.
- **Preserve:** Sky3D mist is the sole global fog owner. Scanner should be visible but not wash out nearby texture detail. Keep scanner independent of entity damage/health and environmental state.
- **Performance:** Sensor executes **only during active scan** at capped ~5Hz, with a small configurable maximum LOS rays per tick; **zero LOS rays** with no targets or a disabled sensor. Hidden beam stops per-instance CPU updates, material/mesh remains cached; its shader animates only while rendered/active. Cache mesh/material resources (per-instance overrides only as needed). Avoid expensive full-screen postprocessing, light casting and layered particle fog. Sensor must remain optional so visual-only scanning costs no detection queries.
- **Pass:** ScanBeam can be instantiated at an arbitrary test socket, explicitly turned on/off and independently swept; cyan cone visible from sides and in mist with moving bands; off = invisible and no scans; sensor emits correct debug result in/out of cone and respects walls/LOS when enabled. No duplicate environment/fog node.
- **Assistant:** Codex for VFX/shader and sensor logic, Copilot for rendered inspection and basic profiler.

## 7. RM4 — End-to-end TestWorld encounter

**Objective:** Give the user the first complete **walk → stop → scan → walk** loop in the real main world scene.

- **Dependencies:** RM1 mesh/animation, RM2 route and RM3 scanner verified independently.
- **Scope:** One Robot_001 instance placed in an **actually traversable flat section** of `WORLD/testworld.tscn` away from cave openings/steep rock; 2–4 waypoint markers; route stages `Walking → Settling → Scanning → Recovering → Walking`; head/sensor reorientation, motion cues and basic debug indicators.
- **Out of scope:** Dynamic spawn system, health/combat, attack/chase/alert, global entity scheduler, patrols across mountainous terrain, savegame integration.
- **Owning paths:** Robot_001 assembly/controller, minimal additive edits to `WORLD/testworld.tscn`, existing F9/DEBUG only if needed.
- **Approach:** Place robot and visible/hidden editable patrol anchors without editing terrain saves. State transitions use shared patrol and scan events: stop body, plant feet, rotate head, enable beam/sensor, sweep left-right for configured duration, extinguish beam, recentre head, resume. Spawn/starting transform must be safe relative to Terrain3D terrain height. First-person player should be able to watch from a distance; free-flight camera remains available for inspection.
- **Config:** Route and stage durations, head yaw limits, robot position, animation scaling and initial enable/disable in RobotMaster; preserve scanner sharing so a second robot can be instantiated with different values.
- **Preserve:** Current F5/F6 modes (note **F6 is already camera mode**), F8 time, F9 tuning, FPS, Terrain3D/Sky3D, caves, weather, vegetation, authored terrain data and existing input map.
- **Performance:** A single preview actor near the camera is active; disabling/hiding should stop patrol timers, gait and scanning. For far dormancy, subscribe to existing `ExplorationController.ViewPositionChanged` (plus an initial proximity check), with **distinct wake and sleep radii** to avoid rapid switching. If the moving robot crosses a boundary, check relevance during its **existing active** movement callback; no per-entity distance polling while asleep, no scene-wide tick scanning, and no background actor manager. For this preview, far robots may pause cleanly and resume at their current waypoint.
- **Pass:** Launch F5 → locate walker → observe at least two complete patrol/scan cycles; see sensor sweep through mist and fade appropriately behind terrain/trees where practical; move around in F6 first-person; disable/re-enable without stale beam or missing controls; add a second scene instance as a reuse smoke test **without editing shared scripts**. Codex runs basic compile/checks; user performs visual/FPS acceptance.
- **Assistant:** Codex for multi-scene end-to-end integration, Copilot for local and screenshot verification.

### Reported patrol/grounding fix — 2026-10-26

User observed stationary scanning then a fall through ground. Corrected completed-wait resume using explicit completion state rather than trusting timer TimeLeft during timeout. Injected the existing terrain-height function into generic locomotion for active body support (not only visual feet); keeps horizontal collision and stops on missing height. No polling while stationary/disabled. Source checks only; runtime confirmation still pending.

### RM2–RM4 implementation handoff — 2026-10-26

Authorized as one job after pushing RM0/RM1. Implemented:

- `Shared/Movement/PatrolController.cs`, `PatrolSettings.cs`, README: explicit actor/route injection; acceleration/braking/turning/gravity, events, one settle/wait timer, idempotent start/pause/resume/stop.
- `Shared/Sensing/ScanSensor.cs`, `ScanSensorSettings.cs`, README: explicit candidates, cheap cone tests and capped round-robin LOS at 5Hz, independent of art.
- `TYPES/Robots/Shared/Scanning/ScanBeam.tscn/.cs/.gdshader`, README: cached low-segment cyan cone, moving bands, no light/shadow/raymarch, no CPU animation callback.
- RobotController/RobotSettings/RobotMaster: coordinate walking/settling/scanning/recovery, nested configurable patrol/sensor defaults, head sweep, beam shutdown and interrupted-scan departure. Reset a valid foot stance on renewed visual relevance; do not solve rig globals after visual subtree exit.
- `WORLD/RobotEncounter.cs/.tscn`, additive TestWorld integration: four-marker 20×14m route near spawn, saved terrain edge/corridor checks and foot placement sampling, explicit player-camera candidate, movement-event relevance with 180m/240m hysteresis, event-only console diagnostics. No terrain, vegetation, weather or input changes.

**Checks:** source/lifecycle/resource-reference review, numerical cone/IK sanity and `git diff --check`. No dotnet or Godot is installed here. Build, actual route clearance, first-person collision, two complete loops, fog/foliage transparency, disable/re-enable, distance waking and two-instance runtime checks are **deferred to the owner**, not passed. No performance measurement or visual acceptance claimed. One local commit (see Git history) covers the RM2–RM4 job; only RM0/RM1 push was requested.

**Known limits:** direct meadow route without obstacle navigation; no exact terrain-conforming beam/ground footprint, no audio or F9 entity controls; arbitrary imported art needs the documented rig wrapper. Sensor range/angle share the beam initialization values, but no live tuning panel is added. See [Robot_001 handoff](TYPES/Robots/Robot_001/README.md) for usage and short local checks. RM5 requires user feedback and separate authorization.

## 8. RM5 — Tuning, profiling, polish and handoff

**Objective:** Turn the proof into a durable reusable encounter system without turning it into a full AI simulation.

- **Dependencies:** RM4 scene works and user reports visual/performance feedback.
- **Scope:** Extend existing `DEBUG/EnvironmentTuning/EnvironmentTuningPanel.cs` with a compact **Entities / Robot_001** section, using existing runtime copies of master resources; optional subtle footstep/thump, metal servo sounds, status label, and measured LOD/update budgets.
- **User feedback — beam visibility:** Patrol and scanning now work in the owner's runtime, but the beam is too faint. RM5 must increase the default cone opacity/brightness and strengthen the moving scan bands so the full cyan volume reads clearly from normal viewing distances in the existing mist. Expose independent cone opacity, brightness and band contrast in F9. Preserve soft boundaries and terrain occlusion; avoid washing out the scene or adding stacked transparent cones/expensive lights. Check side/front views in daytime, night and fog; final strength is subject to owner visual feedback.
- **Out of scope:** Attack weapons, pursuit, health, zombie AI, crafting, dozens of concurrent NPCs, full navigation framework, save persistence.
- **Owning paths:** `DEBUG/EnvironmentTuning/`, RobotMaster, relevant Entity/Robots docs, targeted robot performance fixes.
- **Approach:** Reuse current F9 categories/bindings. Sliders for patrol speed, robot scale, scan duration, full cone angle, beam distance/opacity/brightness, line speed, sweep angle/speed, visual quality toggle and force-scan/debug control. Instant-update material/sensor parameters via change events; staged mesh/scale rebuild (if necessary) via explicit Apply button. Panel only reads actor status **when opened or when a meaningful event occurs**, not every frame. Keep settings ephemeral by default, preserving authoring masters.
- **Performance:** Profile idle, walking, scanning, out-of-range, hide/disable/reactivate and two robot instances. Verify GPU transparent overdraw from cone and shadow costs from large models. If using throttles/LOD, record actual distance thresholds and avoid scan update work while inactive. Optional audio must likewise sleep when disabled.
- **Pass:** F9 edits produce predictable results without fighting current controls; look and sound are convincing in daytime, fog and rain; settings reset; base scene works for a second robot with a modified master; CPU/GPU measurements are recorded as **measured or deferred**, not guessed.
- **Assistant:** Codex for targeted tuning/integration; Copilot for repeatable local build and profiler checks; Regular Chat for reviewing art direction and documentation.

### RM5 implementation handoff — 2026-10-26

Implemented the existing F9 panel's robot category, explicit scale Apply, patrol/beam/sensor edits, force scan, reset and event-only status. Beam defaults now opacity 0.30 / brightness 2.0 / contrast 1.5, with stronger baseline/grazing-angle visibility and no stacked cone/light. More upright default rig (3.7m stance, 7.6m hips). Original quiet synthesized/cached spatial footstep and active-scan servo cues; explicit shutdown and independent audio toggle. Shared gait publishes one pair-landing event; no extra limb update loops. Runtime config update APIs preserve route/candidates without changing saved masters. See Robot_001/README.md and DEBUG/EnvironmentTuning/README.md.

The same authorized job adds seeded broad/detail cave geometry and an organic shared-rim terrain apron under WORLD/Content/Caves; that is separate from entity ownership. No folder relocation.

Source/resource/binding checks and geometry arithmetic performed. No Godot/.NET executable available: **build, visual/audio acceptance, UI reset/scale, one/two-instance runtime and actual CPU/GPU profiling are deferred**. No performance measurements claimed. Owner should briefly check F5/F6/F9, walking/scanning/offscreen/disable/reactivation, cone through fog/foliage and cave entrance/floor/seams. One commit covers this authorized job. No later feature work is implied.

## 9. Recommended first milestone and simplifications

**Minimal viable experience** is one mechanical walker, a short waypoint loop and one good cone effect with a reliable pause/resume cycle. We explicitly prefer a **believable silhouette and cadence** over elaborate sci-fi surface detail.

Prioritize:
1. **Readability in fog:** dark machine outline, cyan scan moving across muted forest/grass. Test day/night and fog on/off before further polish.
2. **Mechanical movement:** distinct foot lift/plant and body weight transfer; start simple with visually linked limb segments. Full multi-leg terrain IK can be improved after seeing it move.
3. **Reusable boundaries:** patrol knows no robot specifics; scan sensor knows no mesh; scan beam knows no targets; only Robot_001 assembles them.
4. **No giant infrastructure:** no custom navigation system, spawner, behaviour-tree addon or custom terrain/voxel tools in this prototype. Existing Terrain3D, Jolt and shared C# suffice.
5. **Proportionate validation:** compile and a brief headless scene check where possible; user validates appearance/Windows D3D12/FPS per CodePreferences. Avoid unnecessary extended test runs unless explicitly requested.

Suggested initial **art-direction numbers** (tunable, not fixed): height 10–12 m, travel speed 2 m/s, scanner length 45 m, scanner full cone angle 35°, head sweep ~120° and scan duration 6s.

## 10. Workflow for Codex

For any selected RM phase:

1. Read [root README](../README.md), [CodePreferences](../CodePreferences.md), this folder README and plan; inspect the current `WORLD/testworld.tscn`, player movement API, masters, debug panel and any newly added entity files. **Do not trust this historical audit over new code.**
2. Implement **only the selected phase(s)**. Do not inadvertently complete or change WE environment phases. Keep the visual atmosphere and saved terrain intact.
3. Use named, editable configurations and comments above methods (with `=========================================================` separators). Keep C# behaviour modular and simple; scene grouping should be clear rather than full of repetitive managers.
4. Document activation trigger, remaining active work, dormant condition and disable/re-enable path. Keep scans capped and render effects disabled when off.
5. Perform proportionate source review and basic build/load checks when available; note any tests deferred to the owner/Copilot (visual appearance, gait, shader transparency/fog, target GPU performance).
6. Update ENTITIES-owned README/plan statuses, list files changed and known limitations, and make **one authorized GitHub commit per job**. Do not declare user visual acceptance without user confirmation or continue into the next RM phase automatically.

**First Codex prompt after review:** “Read README.md, CodePreferences.md, ENTITIES/README.md and ENTITIES/IMPLEMENTATION_PLAN.md. Implement RM0 only, preserving WORLD/testworld.tscn and all current systems. Commit and push with brief validation notes; stop before RM1.”