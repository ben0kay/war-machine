> **Vision-range balance adjustment (2026-10-28, implemented in settings):** normal and runner detection **18m → 22.5m**, retention **23m → 28.75m** (both +25%). Regular wandering **0.65m/s → 0.78m/s** and chase **1.15m/s → 1.38m/s** (+20%). Runner speeds unchanged. **No pursuit/vision code changed.** See [PER2A alerted tracking proposal](ALERTED_TRACKING_PROPOSAL.md) for requested rear-angle behaviour, still unimplemented.

# PER — Shared perception, crouch cover and species-specific sensing

> **Status: PER0/PER1 and PER2 implemented, awaiting local validation. PER3+ remain proposed.** Current PER2 handoff below supersedes original anchor-return policy.
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
- Vision starting values: zombie acquire range ~18m, full cone ~160–180°, physics sight decision every **0.4 s**; current 23m `LoseRange` remains *range hysteresis only*, never permission to see through cover. **PER2 uses per-zombie randomized search commitment and a distance/speed-aware safety timeout**, rather than the older fixed ~3-second universal search. See the detailed PER2 design below. Expose tunable ranges/weights in `ZombieMaster.tres`; don't hard-code difficulty into `VisibilityResolver`.
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
| **PER0 — shared contract** | Inventory existing ray masks/opaque colliders; introduce target height sampling and `VisibilityResolver`, a no-AI line-of-sight helper. Preserve `ScanSensor` behaviour and public hooks. | Implemented; local validation pending |
| **PER1 — zombie vision/cover** | Extend zombie `Think()` with cheap gating and ray checks (through reusable resolver), species settings, realistic crouch detection behind solid cover, preserved health/movement. Reuse zombie timer. Keep robot logic/visuals unchanged. | Implemented; local validation pending |
| **PER2 — last seen** | Snapshot the last visually confirmed position; independently sample early/mid/thorough investigation commitment and expiry per zombie; some abandon part-way, thorough zombies reach the spot and briefly look around. Reacquire LOS instantly; no navigation rewrite. | **Implemented; Awaiting Validation** |
| **PER2A — alerted tracking** | After initial visual acquisition, retain/reacquire a target from any bearing inside the retention range **only while head/torso LOS is clear**. Keep PER2 memory and hard occlusion; no hidden tracking. See [proposal](ALERTED_TRACKING_PROPOSAL.md). | **Proposed only** |
| **PER3 — obstruction polish/debug** | Audit and fix relevant glass/solid collision layering, verify vehicle/rocks/tree/terrain cover, add **optional dormant** LOS debug visual (head/torso blocked/clear) in DEBUG; no always-on draw loop. | Not started |
| **PER4 — scanner integration** | When explicitly requested, adapt existing robot `ScanSensor` to the shared target samples/visibility helper **without changing mechanical beam/patrol or enabling combat**. Verify range/cone and no robot regressions. | Not started |
| **PER5 — sound (future)** | Separate `HearingSensor` and event-driven sound stimuli; species-specific responses. Do not implement as part of initial LOS prototype. | Future |
| **Deer / other wildlife** | Optional Vision/Hearing capability with different cone, eye sockets and flee behaviour. No deer assets or AI created by this plan. | Future |

**Recommended next perception gameplay pass: PER2 alone.** PER0/PER1 already provide frontal sight and physical cover. PER2 consumes that existing perception result without requiring hearing, melee, a new global AI manager or the robot's scanner. PER3 glass/occlusion polish and PER4 robot integration remain separate tasks.

## 8. PER2 — individualized last-seen pursuit (original design; implementation amendments below)

**Objective:** A player can break visual line of sight behind a wall. Zombies remember **where the player was last actually seen**, not the player's continuing hidden transform. Individual zombies pursuing the same player vary in their commitment: some stop part-way, others go farther, and a minority try reaching the full last-seen point and pause to look around. Reacquiring real LOS immediately returns to normal chase.

### Behaviour and priorities

```text
WANDER
  + confirmed direct LOS -> CHASE (continually refresh lastSeenPosition from verified sight)
CHASE
  + LOS lost -> INVESTIGATE_LAST_SEEN (freeze lastSeenPosition; sample pursuit commitment once)
INVESTIGATE_LAST_SEEN
  + confirmed LOS -> CHASE (discard old search commitment, renew memory)
  + sampled travel target reached -> LOOK_AROUND (thorough) or WANDER (early/mid)
  + bounded time expired, physically stuck or target invalid -> WANDER
LOOK_AROUND
  + confirmed LOS -> CHASE
  + brief search dwell expires -> WANDER
Future HE1: HEARD_SOUND -> separate approximate noise investigation, never a verified visual fix
```

Use the current `Think()` callback on its existing staggered one-shot `_brain` physics timer. Sight/LOS tests remain authoritative; `IsChasing` continues to mean **visible target chase**, not blind investigation. A single loss-of-LOS episode snapshots `lastSeenPosition`, `lossOrigin`, an **investigation destination/commitment** and a deadline. While investigating, never update the goal from `_target.GlobalPosition`; the target's position is readable for *sight checks* but not accepted as a pursuit clue until verified visible. Do not repeatedly reroll per brain tick.

### Per-zombie random commitment — proposed starting balance

| Profile (sampled per pursuit episode) | Suggested weight | Fraction of journey toward the last-seen position | On reaching its own target |
| --- | ---: | --- | --- |
| **Early abandoner** | ~40% | 0.40–0.60 | Return to ordinary wander |
| **Moderately persistent** | ~35% | 0.65–0.90 | Return to wander after a brief hesitation, optional |
| **Thorough investigator** | ~25% | 1.00 | Look around for ~0.8–2.0 seconds, then wander |

These are **tuning proposals, not probabilities or values presently implemented**. For example, five zombies can independently draw very different outcomes—none of the proportions force exact counts. Seed the existing per-zombie `RandomNumberGenerator` (already initialized by `InitializeTarget(seed)`) and resample **once after each genuinely lost visual pursuit**, not globally once per spawn and not each decision tick. The baseline direct-approach goal can be a single fixed point interpolated between `lossOrigin` and `lastSeenPosition` by the sampled fraction. This makes a 50% investigator physically stop near the halfway mark on open terrain, rather than just deciding to give up after an arbitrary wall-clock delay. Future navigated routes can measure progress along the path instead.

**Give-up timer:** Add a bounded **safety timeout**, separately varied per episode, so no zombie remains trapped pursuing an unreachable point. Budget it using sampled travel distance and that zombie's configured chase/investigation speed (plus a small allowance), rather than one universal 3-second timeout that always penalizes slow shamblers compared with the 8.3m/s runner. Ending conditions are **whichever occurs first**: its assigned destination reached, elapsed safety time, or existing stuck/unreachable guard. Do not choose a deadline that routinely prevents every slow zombie from reaching even its 50% waypoint. Sampled commitment is the primary source of visible variation; the timer is a fail-safe and a secondary variation.

**At the remembered location:** Only the thorough profile is obliged to look around briefly. Use the already-active perception timer and a bounded pose/turn sequence; allow reacquisition during the dwell, then return to the existing wander anchor if nothing appears. Early/mid drop-out goes back to existing wandering, not automatically to sound-investigation anchor relocation. If the **future hearing phase HE1** has intentionally moved the wander anchor after an audible stimulus, preserve that hearing-specific behaviour independently.

**No clairvoyance and no routing promise:** Zombies cannot currently path around a building; their `CharacterBody3D` moves directly toward `_goal` and may stop at a wall. PER2 should safely time out/stick-abandon in that case, not teleport through geometry or claim it reached the other side. A *separate future navigation/pathfinding phase* is needed to reliably route around structures. This limitation does not block the basic remembered-position investigation on an unobstructed route.

### Intended ownership and implementation details

- **`ENTITIES/TYPES/Zombies/ZombieController.cs`:** add a tiny bounded visual-awareness state (or flags if simpler), last visually verified XZ goal, loss origin, per-episode commitment, deadline and look-around dwell. Update on the existing `Think()` callback; keep one `_goal` driving the existing movement `_PhysicsProcess`. Preserve `_home`, wander radius, zombie-specific movement speed, `_stuck` recovery and health/death behaviour. Reset memory/episode on InitializeTarget, deactivation, death and scene unload. Do not use `Time.GetTicksMsec()` beyond an explicit expiry, and do not poll a new timer at idle.
- **`ENTITIES/TYPES/Zombies/ZombieSettings.cs` + zombie/runner masters:** Inspector-editable `LastSeenInvestigationEnabled`, profile weights, partial-distance bands, timeout allowance/ceiling, arrival tolerance and thorough dwell range; author bounded defaults and validate impossible ranges. Prefer a small `[ExportGroup("Last seen investigation")]` to a new configuration class or global singleton. Any runner-specific override should change only policy values, not duplicate the AI implementation.
- **`ENTITIES/Shared/Sensing/VisibilityResolver.cs`:** **no required change**; PER0/PER1 already produce true/false LOS results. Shared geometry answers visibility; zombie owns memory and willingness to search. `WorldController` remains an actor composer, not a central memory service.
- **`DEBUG/` (optional):** If practical, add a disabled-by-default one-shot destination/last-seen marker to the existing zombie debug toggle—not a permanent marker update, per-frame draw loop or second debug window. Helpful for verifying 50%/100% commitments, but strictly optional for PER2.
- **`HEARING_INVESTIGATION_PLAN.md`:** sound stimulus/anchor relocation remains **HE0/HE1**, not implicit in PER2; if both are later active, `confirmed LOS > deliberate recent investigation > ordinary wander` with configurable strength/recency deciding between competing *nonvisual* clues. A gunshot or damage hit does **not** reveal the hidden player's location until a sound gameplay stimulus actually exists.

**Explicitly out of scope:** Implementing hearing, emitting shot noises, damage/shot stagger (COMBAT CM1), zombie attacks, navigation mesh/pathfinding, giant robot sensing, alert-sharing between zombies, AI group coordination, persistent saves and full animation overhaul. No memory of the hidden player should survive death/deactivation.

**Preserve:** acquisition/hysteresis LOS, 0.4s staggered decisions, physics collision, existing wander/chase poses and speeds, runner subclass, damage/death, camera modes, F7 debug cone, weather and current audio playback. No visual/behaviour regressions in normal open-ground pursuit.

**Performance contract:** Each active zombie uses only its existing short `_brain` decision tick; state updates cost O(1), with 1–2 bounded existing LOS queries only when eligible. One seeded random draw/set on LOS-loss, no `_Process` memory manager, global scene scanning, extra idle timers or per-frame allocations. Stop active movement when stable, as before. World/chunk unload or deactivation must stop existing timer and clear memory; no stale player references.

**Pass criteria:** Build if available; F6 `WORLD/World.tscn` and briefly FPS. Hide behind solid wall after confirmed acquisition: multiple zombies pick visibly different partial/full goals, do not follow the moving hidden player, re-see and chase again immediately, and eventually give up. Test five or more with statistical variation over repeated attempts (do **not** require exact counts), slow and runner distances, LOS flicker, close-range near-zero distance, corner/wall trapping and no teleport. Full investigators pause/look at last-seen spot then wander; normal F7/debug, firearms, hit/death and camera-relative movement unaffected. Verify stopped/inactive actors incur no new updates. **Codex / ChatGPT Work** for multicomponent AI integration; **Regular Chat** for tuning and docs; **Copilot** for short local compilation and playtest. Mark Awaiting Validation until the owner visually accepts; this plan alone changes no gameplay.

## 9. Minimum functional tests and acceptance

Brief build/smoke checks at the active `WORLD/World.tscn` and inherited `WORLD/FirstPerson/FirstPersonWorld.tscn` (if runnable):
- Open ground: standing and crouched player both visible in the allowed cone/range.
- Medium cover: standing head visible, crouched head/torso hidden.
- Full building wall and Terrain3D ridge: sight blocked; moving out of cover reacquires.
- When a window is intended transparent, LOS sees through even if the pane remains physically collidable.
- Nearby partial-body reveal: clear head OR clear torso means detectable.
- Zombie faces away: no ordinary visual acquisition outside the configured cone; taking a shot should not silently enable hearing.
- After initially seeing then losing target: investigate the last-seen location (not live hidden position); independent early/mid/thorough give-up fractions and duration bounds, reacquisition, look-around, and blocked-path fallback (PER2).
- Robot beam, scanner cone/visual and patrol unchanged in PER0–PER3.
- Sleep/unload/reactivation: no stale candidate references/timers. Compare FPS behaviour and idle versus active cost, don't claim measured gains without Godot profiling.

**Do not rewrite the procedural buildings, physics gameplay, rifle, robot scanning artwork, save system, FPS camera or overall world hierarchy.** Keep all added C# code under the appropriate `ENTITIES/Shared/Sensing/` and species folders, README docs current, and push **one commit per authorized implementation job** to `implement-td0-td1`.

## PER0/PER1 implementation note

Authorized together with a faint frontal debug cone. Shared IPerceptionTarget/VisibilityResolver, dynamic player head/torso samples, zombie visual-facing cone and bounded LOS now implemented. Existing zombie timer reused; no separate VisionSensor needed. Loss immediately returns to wandering at the next sight tick (no hidden position tracking); investigative memory remains PER2. Mask 17 intentionally uses existing world + trunk layers; glass still blocks pending PER3. DEBUG/Perception owns optional F7 fan display; it shows potential FOV, not clipped ray results. The rest of PER3, including glass audit and sample-ray diagnostics, remains unimplemented. Source review/whitespace checks only: no Godot/.NET available here.

## PER2 — implemented last-seen investigation and relocated wandering

**Implemented; Awaiting local compilation/runtime acceptance.** Owner explicitly authorized PER2 plus relocating the wander anchor to the actual give-up position and collision-safe wander selection. This overrides the original design below that returned to the old anchor. No hearing, navigation, zombie attacks, robot changes or convenience-store placement.

ZombieController retains the only brain timer and movement callback. ZombieInvestigation.cs is a partial implementation of that same controller (not a separately attached script/node): after confirmed sight loss, freeze the last verified player coordinate and draw a single independent episode profile from the actor's existing seeded RNG. Early/moderate/thorough weights default 0.40/0.35/0.25; travel fractions 0.40–0.60 / 0.65–0.90 / 1.0. No resampling per decision tick. Investigation uses species chase speed and pursuit gait (runner keeps its running acceleration); IsChasing still means confirmed direct sight only. While investigating, retain the existing lose-range hysteresis. Confirmed sight on a normal decision tick immediately cancels search/anchor relocation and resumes chase; no hidden transform becomes a new goal.

Give-up deadline = assigned horizontal distance / species chase speed × random 1.10–1.60 + 3s, capped at 60s. All policy weights/ranges/allowance/ceiling/arrival/dwell are Inspector-editable on both masters; swapped ranges normalize, nonfinite/negative values clamp, all-zero weights select thorough. Arrival tolerance 0.45m. Thorough zombies dwell/turn on the existing brain ticks for 0.8–2s; early/moderate stop at their fraction. Arrival, deadline, invalid target, disabling investigation or >2s physically stuck ends search. Existing physical collision remains authoritative; no teleport or pathfinding around walls.

On end, stop the goal, brake through normal movement and let active shove/recovery/gravity finish. Only once grounded and settled, copy actual GlobalPosition into the wander anchor. Do not use the remembered destination or pull the zombie home to spawn. Reacquisition before settling cancels pending relocation. Death/deactivation/spawn reset clear episode state; exit also releases registered target/visibility/ground callback references. Ordinary wander stops do not continually shift the anchor.

Wander selection: maximum four candidate attempts when a new wander goal is needed, on the existing physics brain tick. Each must have layer-1 support within the local feet-height band, slope <= FloorMaxAngle, <=0.75m endpoint height difference, a clear full-body capsule at destination and start, and a clear direct capsule sweep using movement mask plus world/trunks. This rejects points inside solid trees/buildings/actors and paths through walls. Raised start avoids treating floor support as initial overlap; start overlap is explicitly checked before CastMotion because pre-existing overlaps are ignored by that API. If all attempts fail, rest 1–3s and retry. No straight-line obstacle avoidance/pathfinding is claimed: rough ground, narrow passages, crowded starts or steps may conservatively reject usable routes; a mid-route cliff/hole still needs future navigation. No guarantee applies to artwork with no physical collider.

Performance: no new timers, per-frame queries, global scans or processing nodes. Existing active brain uses O(1) memory updates plus existing bounded 1–2 sight rays. Only wander-goal selection adds up to four support rays, eight overlap checks and four sweeps per selection (early-outs reduce this). Query objects and self-exclusions are cached; Godot result dictionaries/arrays may allocate at these bounded calls. Physics/gait sleeps when grounded/settled; look-around uses only existing decision ticks. Deactivate/unload stops timer and clears memory. No measured performance claim.

Validation here: source/state/lifecycle and new C# loop review, git diff --check, numeric policy checks for tier variation, assigned-distance vs timeout for both 1.15m/s shamblers and 8.3m/s runners, frozen clue and actual-position anchor contract. These numeric/source checks do not execute Godot AI. Godot/.NET unavailable: **compilation not verified**.

Local handoff: `dotnet build war-machine.csproj`; F6 `WORLD/World.tscn`, then briefly `WORLD/FirstPerson/FirstPersonWorld.tscn`. Let at least five zombies see you, hide behind solid cover and move while hidden: goals should remain frozen with independent early/mid/full travel, then stop/dwell and wander around their new stopping area. Check slow/runner, reacquisition during travel/look/braking, near-zero clue distance, wall trapping, tree/door/body clearance during wandering and isolated uneven ground. Set weights temporarily to (1,0,0)/(0,1,0)/(0,0,1) to exercise each tier, then restore defaults. Check bullet/shove/death, deactivate/reactivate and unload; F7 still indicates potential sight range. Inspect stationary brain/physics costs locally. PER3+ and HE0+ remain unimplemented.
