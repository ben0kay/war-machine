# RM — Reusable Entity, Mechanical Walker and Scanning Prototype

> **State:** PLAN ONLY — **RM0–RM5 all Planned**. This file authorizes *no implementation* by itself.
>
> **Purpose:** In the existing Godot C# **War Machine** project, add one memorable non-hostile, original giant mechanical walker that **walks → stops → scans → resumes** in the real `WORLD/testworld.tscn`. Build small shared components so future robots and other entity types can reuse movement, waypoints, sensing and visuals where appropriate.
>
> **Project rules:** [Root README](../README.md) · [CodePreferences](../CodePreferences.md) · [ENTITIES ownership](README.md). This plan is separate from the environment-only [WE roadmap](../WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md).

## Phases at a glance

| Phase | Goal | Recommended assistant | Status |
| --- | --- | --- | --- |
| **RM0** | Minimal shared Entity base, folders, configs and reusable actor/component contract | Codex / ChatGPT Work; Regular Chat can review design | **Planned** |
| **RM1** | Robot_001 procedural hard-surface model and mechanical stepping | Codex / ChatGPT Work | **Planned** |
| **RM2** | Shared waypoint patrol and stop/turn/dwell lifecycle | Codex / ChatGPT Work | **Planned** |
| **RM3** | Shared logical scan sensor and Robot-only cyan 3D beam effect | Codex / ChatGPT Work | **Planned** |
| **RM4** | Integrate an end-to-end walk/stop/scan loop into TestWorld | Codex / ChatGPT Work | **Planned** |
| **RM5** | Performance, reusable authoring, F9 controls and visual/audio polish | Codex / ChatGPT Work, Copilot local checks | **Planned** |

**State vocabulary:** Planned → In Progress → Implemented → Awaiting Validation → Accepted. An implementation commit or a headless compile is not visual acceptance. **Do not automatically start another RM phase.** If the user authorizes the full RM0–RM4 feature as one job, complete only that explicitly scoped job; otherwise implement the selected phase and stop.

## 1. Confirmed current state and boundaries

As inspected before this plan:

- **No `ENTITIES/` directory currently exists.** All paths below are **proposed**, not implemented.
- `WORLD/testworld.tscn` is the **actual project main scene**. It has Terrain3D terrain, Sky3D day/night and thick distance mist, vegetation/grass/pines and small decorative content, a cave, weather integration, F9 tuning and the FPS display.
- `PLAYER/ExplorationController.cs` extends `CharacterBody3D` and supports a free-flight camera plus optional collidable **first-person movement via F6**. Do not rewrite it to give the robot movement. Use it only as an optional future sensing target/debug reference.
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

## 3. RM0 — Shared entity foundation and contracts

**Objective:** Prepare a *minimal* reusable mobile-actor base and typed data contract, without accidentally building gameplay systems.

- **Dependencies:** Existing Godot C# project, `testworld.tscn`, CodePreferences; inspect actual scene/C# conventions before adding files.
- **Scope:** `ENTITIES/Shared/Entity.tscn`, minimal `EntityCore.cs` and `EntitySettings.cs`, `Configuration/EntityMaster.tres`, per-area documentation, planned actor lifecycle/activation contract; optionally stub a tiny event interface required for later components.
- **Out of scope:** Robot model, movement, scanner effect, health/damage, enemy factions, spawn tables, savegame integration, global tick scheduler.
- **Owning paths:** `ENTITIES/README.md`, `ENTITIES/Shared/`, `ENTITIES/Configuration/`; this plan.
- **Implementation:** Root as simple `CharacterBody3D` for moving actors; scene inheritance or composition for concrete types. Provide typed settings, stable owner references, start/stop hooks and no update loops by default. Make optional components attachable through exported NodePaths/typed references rather than deep `GetNode` searches on every frame.
- **Config:** Identity/type label, active/disabled defaults, activation distance or explicit enable toggle (only if actually used). Do not bake robot-specific numbers into EntityCore.
- **Preserve:** Entire TestWorld, authored terrain, environment and other entities unaffected.
- **Performance:** Spawn/enable explicitly wakes actor; disabled/sleeping actor does **zero unnecessary physics/process**. No scans by default. Document idle costs.
- **Pass:** A minimal reusable entity scene/resources load/compile and can be instantiated/released cleanly; no visual/gameplay changes to TestWorld.
- **Assistant:** Codex for scene/C#/resource setup; Regular Chat can refine design/docs; Copilot local build checks.

## 4. RM1 — Robot_001 visual walker and articulated legs

**Objective:** Make one **original, unmistakably large** mechanical creature that moves like an engineered machine rather than a floating rigid body.

- **Dependencies:** RM0 base; working player/terrain reference.
- **Scope:** `Robot_001.tscn`, dark angular sci-fi body, rotating sensor head with named `SensorSocket`, four articulated legs with hip/knee/foot joints, purposeful body weight/bob, industrial materials and restrained emissive details.
- **Out of scope:** Patrol decisions, beam, target detection, attack weapons, imported licensed characters, full IK framework/addon.
- **Owning paths:** `ENTITIES/TYPES/Robots/Robot_001/`, `ENTITIES/TYPES/Robots/Shared/Movement/MechanicalLegs.cs`, related README/config.
- **Approach:** Build initially from C# generated low-poly/medium-poly **original** meshes grouped in a stable transform hierarchy: torso/armour → rotating head/sensor socket; leg sockets → upper/lower leg → foot. Target about **10–12 metres tall** (starting art value only). Use procedural stepping/stance timing, short foot probes if useful for Terrain3D placement, and a simple two-segment bend/IK solver only if needed for credible planted feet. Prefer a convincing 4-leg gait over fancy detail. Make a stationary walk-preview/debug animation possible; the patrol phase later provides actual motion.
- **Config:** Body/leg size, torso pitch/bob, stride length, gait speed, leg lift, foot-plant pause, head yaw limits, material colours, emissive strength, imported mesh override path.
- **Preserve:** Godot Jolt/terrain collision use proper capsule or low-cost approximations; **no mesh collision per articulated leg**. Do not modify `WORLD/Content` procedural pines or decorative-content scatterers.
- **Performance:** Only animate legs while moving/settling and visually relevant; no per-leg Godot nodes running independent `_Process`. If absent/offscreen, stop visual gait without blocking shared actor lifecycle.
- **Pass:** Coherent full-body 3D shape, four distinct mechanically hinged legs and moving joints, stable leg pose when stopped, head independently rotatable, reasonable triangle/instance counts, replaceable art hierarchy.
- **Assistant:** Codex due to procedural geometry, transforms, rig math and scene wiring; Copilot can run local visual/sanity checks.

## 5. RM2 — Shared patrol and stop-state movement

**Objective:** Establish generic **Walk → Arrive → Settle → Wait → Next waypoint** behaviour reused by Robot_001 and future mobile entities.

- **Dependencies:** RM0; RM1 leg hooks (can use placeholder art while testing generic controller).
- **Scope:** Optional `PatrolController`, waypoint array/route resource or child marker transforms, loop route and deterministic arrival/turn logic; signal events for started walking, arrived, stopped and ready to depart.
- **Out of scope:** Global pathfinding framework, pursuit/combat, complex navigation mesh bake over 2 km, terrain deformation.
- **Owning paths:** `ENTITIES/Shared/Movement/` and Robot_001 assembly; TestWorld route marker positions only when RM4 authorizes world integration.
- **Approach:** Begin on an **intentionally flat, unobstructed meadow** with 2–4 reachable waypoints. Kinematic movement with real world delta/velocity, acceleration/deceleration and heading turn rate; floor-snapping/gravity where appropriate. Drive body-relative gait via speed and signals. Expose `StartPatrol()`, `PausePatrol()`, `ResumePatrol()`, `StopPatrol()`. Arrival signals should be precise and only fire once per waypoint.
- **Config:** Speed (~2m/s to start), acceleration, braking, turning speed, arrival radius, wait/settle duration, route looping and optional waypoint dwell overrides.
- **Preserve:** Do not rewrite player input, Terrain3D collision, Sky3D or weather. Path markers are nonintrusive and editable in scene.
- **Performance:** Physics processing **only while moving/turning**; a Godot Timer handles stationary dwell rather than ticking waiting state. No per-frame world search or repeated path recalculation. Stop/disable really sleeps; resume wakes.
- **Pass:** Robot moves between points, rotates with feet aligned approximately to direction, stops without sliding, waits at each marker, loops predictably and restarts on command. No drifting on stop, timer leaks or stuck idle `_PhysicsProcess`.
- **Assistant:** Codex for reusable movement wiring/state signals; Copilot for path validation.

## 6. RM3 — Generic sensor and Robot scan-beam VFX

**Objective:** Build the iconic scanner as **two reusable and independently testable systems**: logical sensor and visual sci-fi projection.

- **Dependencies:** RM0 owner/components; RM1 head/`SensorSocket`; RM2 pause/dwell hooks recommended.
- **Scope — Sensor:** Optional `ENTITIES/Shared/Sensing/ScanSensor.cs` with cone origin/direction, length, half-angle, periodic target checks against explicit candidate list or physics query and optional raycast line of sight. Report enter/exit/seen events to a debug callback without triggering combat.
- **Scope — VFX:** `ENTITIES/TYPES/Robots/Shared/Scanning/ScanBeam.tscn`, `ScanBeam.cs`, `ScanBeam.gdshader`: translucent volumetric-looking cone or truncated cone, cyan/teal glow, soft feathered edges, scrolling scan-lines and one more pronounced scanning band. Cone sweeps with robot's sensor head left/right. Faint ground contact ring/footprint may use a simple mesh/raycast to **approximate**, not pixel-accurate project, contact.
- **Out of scope:** Raymarched real volumetric lighting, real cone collision, expensive many-target per-frame scan, laser damage, targeting/chasing, actual terrain decals that depend on undocumented addon support.
- **Owning paths:** `ENTITIES/Shared/Sensing/`, `ENTITIES/TYPES/Robots/Shared/Scanning/`; RobotMaster owns initial tuning.
- **Approach:** Reusable cylinder/cone mesh built once (local forward axis clearly defined, e.g. `-Z`). Transparent spatial shader uses UV/vertex position for radial/length falloff and scrolling cyan lines. Limit overdraw with short range, few rings/segments and disable shadow casting; test depth sorting/occlusion under Sky3D fog in Forward+. The sensor uses dot-product cone and range checks before occasional physics LOS rays. **Beam shape is cosmetic and need not match irregular terrain surface exactly.**
- **Config:** Cone length (~45m), full spread (~35° starting point; define whether full or half angle), colour, brightness/opacity, line count/speed, gradient, ground footprint size, head sweep amplitude (~120°), sweep speed, scan duration (~6s), sensor interval/LOS budget.
- **Preserve:** Sky3D mist is the sole global fog owner. Scanner should be visible but not wash out nearby texture detail. Keep scanner independent of entity damage/health and environmental state.
- **Performance:** Sensor executes **only during active scan** at a capped rate; no scan with no targets or disabled sensor. Hidden beam stops per-instance CPU updates; its shader animates only while rendered/active. Cache mesh and material resources (per-robot parameter duplication only if needed). Avoid expensive full-screen postprocessing/particle fog.
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
- **Performance:** A single preview actor near the camera is active; disabling/hiding should stop patrol timers, gait and scanning. If far enough away, use simple distance-triggered activation/deactivation and reactivation preserving sensible state. No scene-wide tick scanning.
- **Pass:** Launch F5 → locate walker → observe at least two complete patrol/scan cycles; see sensor sweep through mist and fade appropriately behind terrain/trees where practical; move around in F6 first-person; disable/re-enable without stale beam or missing controls; add a second scene instance as a reuse smoke test **without editing shared scripts**. Codex runs basic compile/checks; user performs visual/FPS acceptance.
- **Assistant:** Codex for multi-scene end-to-end integration, Copilot for local and screenshot verification.

## 8. RM5 — Tuning, profiling, polish and handoff

**Objective:** Turn the proof into a durable reusable encounter system without turning it into a full AI simulation.

- **Dependencies:** RM4 scene works and user reports visual/performance feedback.
- **Scope:** Extend existing `DEBUG/EnvironmentTuning/EnvironmentTuningPanel.cs` with a compact **Entities / Robot_001** section, using existing runtime copies of master resources; optional subtle footstep/thump, metal servo sounds, status label, and measured LOD/update budgets.
- **Out of scope:** Attack weapons, pursuit, health, zombie AI, crafting, dozens of concurrent NPCs, full navigation framework, save persistence.
- **Owning paths:** `DEBUG/EnvironmentTuning/`, RobotMaster, relevant Entity/Robots docs, targeted robot performance fixes.
- **Approach:** Reuse current F9 categories/bindings. Sliders for patrol speed, robot scale, scan duration, full cone angle, beam distance/opacity/brightness, line speed, sweep angle/speed, visual quality toggle and force-scan/debug control. Instant-update material/sensor parameters via change events; staged mesh/scale rebuild (if necessary) via explicit Apply button. Keep settings ephemeral by default, preserving authoring masters.
- **Performance:** Profile idle, walking, scanning, out-of-range, hide/disable/reactivate and two robot instances. Verify GPU transparent overdraw from cone and shadow costs from large models. If using throttles/LOD, record actual distance thresholds and avoid scan update work while inactive. Optional audio must likewise sleep when disabled.
- **Pass:** F9 edits produce predictable results without fighting current controls; look and sound are convincing in daytime, fog and rain; settings reset; base scene works for a second robot with a modified master; CPU/GPU measurements are recorded as **measured or deferred**, not guessed.
- **Assistant:** Codex for targeted tuning/integration; Copilot for repeatable local build and profiler checks; Regular Chat for reviewing art direction and documentation.

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