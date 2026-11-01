# WS — Finite world, streaming and town authoring

**Status (2026-10-28): WS0/WS1 implemented in source, awaiting local validation. WS2 isolated proof demonstrated locally; four-building active integration implemented, awaiting local validation; WS3–WS7 remain Planned.** Owner explicitly authorized WS0 with a top-centre current-section heading/grid, then WS1 fog-aware rendering. Branch: `implement-td0-td1`; one commit per subsequently authorized job. Read root README, CodePreferences, this plan, WORLD README and current owning sources before each phase. Do not resume the parked wilderness roadmap or generate a giant map automatically.

## Direction

Build a large **finite, expandable authored world**, centred on a fairly built-up town, with surrounding forest, hills, terrain, caves and eventual drivable vehicles. Establish coordinates and section ownership early; populate and extend the detailed area gradually. Exact world dimensions are undecided. Existing playable terrain remains the first test area; reserving a large logical map must not allocate or generate that whole map on startup.

Use deliberate town layout plus constrained procedural assistance. The owner places roads, intersections, important buildings and sites. Generators may create validated building variants, forest detail and roadside dressing. Alleyways, rear access and pedestrian connections are reserved before final building placement. Furniture uses legal sockets/clearances, not arbitrary wall positions that can put a picture over a doorway. World-layout choice and chunk streaming are independent: hand-placed scenes can stream just as procedural ones can.

## WS distance-authority refinement — proposed Copilot implementation contract (2026-10-31)

**Status: PLAN ONLY.** This clarifies existing owners and provides executable numeric acceptance criteria; it **does not** mean simulation tiers, unified configuration or actor unloading have been implemented. Owner request: make rendering and simulation distance-related, keep scene streaming farther out, and avoid unexplained competing distance sliders. Use the first-person gameplay world as the design reference. The older WS0–WS7 goals, previously implemented WS2 proof, 32m foliage chunks and 128m logical section identities remain intact.

### Folder ownership — binding decision for Copilot

**Do not create `WORLD/Relevance/`, a new `WORLD/ChunkStreaming/` folder, or a second global world-streamer.** The authoritative home for **world-distance/relevance policy, streaming coordination, 128m section identity and static-scene residency code** is the **existing `WORLD/Streaming/`** folder. The one authored master remains `WORLD/Configuration/WorldMaster.tres`. This decision supersedes the earlier `WORLD/Relevance/` examples in past conversations.

**WS-D0 expected layout (proposed code; not created by this documentation update):**

```text
WORLD/
  Configuration/
    WorldMaster.tres                   existing; add typed Relevance subresource, do not split master
  Streaming/                           EXISTING authoritative world streaming/relevance home
    README.md                          existing owner/limits; update when behaviour changes
    WorldSectionSettings.cs            existing 128m logical ownership grid
    SectionCoordinate.cs               existing logical coordinates
    WorldStreamingSettings.cs          existing WS2 load/unload settings and work budgets
    WorldStaticStreaming.cs            existing four-building streaming integration
    StaticSectionStreamer.cs           existing bounded static scene scheduler
    WorldRelevanceSettings.cs          WS-D0 new; ONLY new offsets/rates, no duplicate fog/vegetation settings
    WorldRelevancePolicy.cs            WS-D0 new; pure calculator + read-only effective snapshot
    [optional small snapshot type]     only if cleaner/necessary; avoid extra files by default
  Rendering/                           EXISTING WS1 native geometry/light culling owner
    WorldRenderCulling.cs
VEGETATION/                             EXISTING grass/pine/DF2 per-chunk generation, MultiMesh, native LOD
ENVIRONMENT/                             EXISTING fog visual profile, shader and applied-range signals
ENTITIES/                                EXISTING zombie/deer AI; future WS4 tier consumers
DEBUG/WorldStreaming/                    EXISTING focused contract tests / diagnostics
```

**One source of truth for _effective_ boundaries, not a new owner for every subsystem:** `FirstPersonMaster.tres` still authors visible FPS fog, `PlayerDistanceFog` reports actual applied fog, `VegetationVisibilityPolicy` remains the source of the existing vegetation cutoff and LOD, and the WS2 bridge remains responsible for its camera-safe load/unload envelope. The new `WorldRelevancePolicy` should **compose those already-computed values into one immutable/read-only snapshot**, adding only the proposed new actor-tier offsets/rates from `WorldRelevanceSettings`. **Do not add another independent `FogSafetyBuffer=24` or `LoadDistance=272` setting just to reproduce an existing number.** Centralize ownership further only after auditing consumers and authorizing a separate integration slice; WS-D0 must not alter runtime behaviour.

Keep the `WorldMaster.tres → Relevance` typed subresource adjacent to existing `Streaming` and `RenderPolicy`, rather than inventing additional authoring masters. Existing `WorldMaster → Streaming` retains scene residency caps/distance minima and `RenderPolicy` retains native GPU range buffers; they are different responsibilities under one master, not three competing masters.

### Chunk coordinates vs distance — factual current design (verified 2026-10-31)

**There is no single terrain-chunk loader controlling the entire War Machine world today.** Do not assume a 128m square, terrain region, foliage batch and building are the same object.

| Existing owner | Spatial unit | Actual activation/deactivation | What remains |
| --- | --- | --- | --- |
| `VEGETATION/Grass/Stylized/StylizedGrassPlacement.cs` | **32×32m** by `StylizedGrassMaster.tres` (ForestGrass001 + ForestGrass002 + optional billboard/fern/shrub species in a shared owner) | Computes candidate chunk keys around the moving player with `Visibility.RenderDistance` plus view-offset and chunk-footprint safety. Creates `GrassChunk` MultiMesh batches; `ReleaseChunk` removes scene nodes when no longer wanted | Imported mesh assets/materials are cached/shared; chunk placement is deterministic. This is real batched vegetation residency, not only visibility toggling |
| `VEGETATION/Trees/Pine/PinePlacement.cs` | **32×32m** by `PineMaster.tres`; configurable | Player movement + fog-derived `Visibility.RenderDistance`; uses chunk-bound safety and a separate larger removal margin for hysteresis | Procedural/imported art and authored LODs retained as shared source; individual tree render LOD is native |
| `VEGETATION/Forest/ForestPlacement.cs` (DF2) | **32×32m** by `ForestMaster.tres`; configurable | Precomputed forest layout; camera/player reach determines which per-chunk MultiMesh/tree-trunk batches are created/removed | The layout/seed data is not necessarily removed when one chunk becomes invisible |
| `WORLD/Streaming/StaticSectionStreamer.cs` + `WorldStaticStreaming.cs` | **Placement record and actual world footprint**, not full-section terrain tile | Approx. **272m load / 336m unload** around the player in current FPS profile; threaded resource requests and one bounded scene operation/frame; actor proximity can pin buildings | Currently only four village buildings; no game-state save and no zombies/terrain streaming |
| `WORLD/Streaming/WorldSectionSettings.cs` | **128×128m** logical section | Floor-based address/ownership plus F3 section diagnostics; footprint overlap is a dependency, not another instance | Does not itself spawn, hide or free the square's terrain/vegetation |
| `WORLD/Terrain/Gameplay/` and Terrain3D | Saved/native terrain regions (different abstraction) | The present game loads its saved terrain and full gameplay collision; native Terrain3D internals do not amount to an implemented WS terrain unload adapter | No authored terrain-region streaming lifecycle in WS0–WS2 |
| `ENTITIES/` zombies/deer/robot | Individual active actors | Existing species AI timers/physics-on-demand; **no shared proximity tier coordinator** | Actors not yet WS4 distance-slept or safely unloaded |

**Meaning:** chunks answer *which batch*, distance answers *whether that batch/placement is wanted*, rendering answers *whether Godot draws it*, and simulation answers *how much AI/physics runs*. The **128m section is not a 128m GPU render/cull tile**; 32m vegetation chunks may sit inside it, but don't need the same origin or common lifetime. Never make a section crossing automatically unload a building that overlaps it or freeze a pursuer.

### Chunk lifecycle cooperation — separate proposed later slice, not WS-D0

Reference inspected: [ProWille/Godot-Adventure-Game](https://github.com/ProWille/Godot-Adventure-Game), especially `scripts/TerrainController.cs`, `scripts/Decorations/DecorationGenerator.cs` and `scripts/Grass/BladeGrassGenerator.cs`. Its useful pattern is **`terrain chunk created → decoration systems populate; terrain chunk released → decorations release`**. War Machine uses saved Terrain3D and its own foliage chunks, so **borrow the event/ownership principle, not its 64m procedural terrain grid, per-chunk OS threads, square radius or code**.

**WS-CL0 (planned, after WS-D0/D1 and only if the lifecycle audit finds a real coordination need):**
- Audit each existing owner for *desired / loading / resident / visible / dormant / releasing*; define exact event semantics **per owner**, not falsely claim one global `ChunkLoaded` event means every subsystem in that area is ready.
- If useful, add small idempotent lifecycle notifications within existing `WORLD/Streaming/` contracts and publish **distance snapshot changes**, while existing vegetation managers retain their own chunk IDs, meshes, LODs, collision and per-chunk create/release methods. New consumers may subscribe rather than poll.
- Do **not** introduce a `WorldStreamingCoordinator` script by default. Add one **only if** later WS3/WS4 integration proves the existing event bus / owners cannot coordinate without repeated logic. No forced parent-child chunk hierarchy, no repartitioning terrain, no duplicating scene trees, no dispatcher with per-frame global scans.
- Preserve different granularities: vegetation 32m; world logical ownership 128m; buildings footprint/distance; terrain3D separately; actors distance tier plus stable encounter/section IDs. Observers may consume *section ownership changes* or *relevance tier changes* without sharing the same grid.
- Events should have stable identifiers and a clear sequence: request → ready/resident → optional visibility/AI tier changes → release requested → save or pin veto (mutable objects) → released. Never advertise saved/released state before the owning system confirms it. Debounce live fog slider changes, cap work and reject stale requests; idle cost ~0.
- Evaluate **when** cross-owner coordination is actually beneficial: e.g. WS4 actors require the ground/collision ready before wake, and mutable WS3 props must save before their static owner releases. Simple foliage growth doesn't need a global scene streamer to function.
- Test: repeated same-coordinate entry/exit, rapid fog changes, boundary oscillation, no duplicate objects or extra running callbacks, no early collision disappearance, return regenerates the same seeded scenery, no unintended save reset. If the existing lifecycle is sufficient, document that and implement **zero new code**.

**Proposed sequence:** **WS-D0** pure snapshot in `WORLD/Streaming/` → **WS-D1** live F1 readout → **WS-CL0** optional lifecycle audit/contract (no new manager by default) → **WS4A** actor simulation tiers without unloading → **WS3** safe persistence → **WS4B** actor unload. These are distinct, owner-authorized scopes; this docs pass authorizes no runtime changes.

### A. Current authoritative files — verified against source

| Concern | Current real owner | FPS current value / behavior | Important limitation |
| --- | --- | --- | --- |
| **Visual fog: start/full opacity** | `WORLD/FirstPerson/Configuration/FirstPersonMaster.tres → FogStartDistance / FogEndDistance`; `ENVIRONMENT/FirstPerson/FirstPersonFog.cs` applies this to `ENVIRONMENT/Fog/PlayerDistanceFog.cs` | 1m → **115m** opaque; opacity forced to 1 | Fog overlay visually obscures opaque world pixels; it **does not itself stop GPU drawing, physics or AI** |
| **FPS camera clipping** | Same FPS master `FogCullBuffer=120`; `FirstPersonFog.Apply()` | Camera `Far = 115 + 120 = 235m` when enabled | This is a **far-clip buffer**, not proof every renderer culls at 115m |
| **Vegetation LOD and visibility** | `VEGETATION/Configuration/VegetationVisibilityMaster.tres` → `VegetationVisibilityPolicy.cs` | **139m** nominal vegetation range = opaque fog 115 + 24m safety; LOD split **~49m / ~97m** (35% / 70%) | Applies to loaded grass/billboards, pines and DF2 trees; 32m vegetation chunk owners determine resident batches. Off/nonopaque fog uses 160m fallback in current config |
| **WS1 static props/buildings rendering** | `WORLD/Configuration/WorldMaster.tres → RenderPolicy`; `WORLD/Rendering/WorldRenderCulling.cs` and `WorldRenderEnvelope.cs` | Enabled; hidden buffer 8m, shadow buffer 48m, AABB/object-specific margin | **Not one fixed 139m cutoff.** Native AABB/camera/light/shadow rules and authored ranges apply. FPS `GameplayEnvironmentMaster.tres → FpsSkyFogOpacity=0.08` means the sky is **not fully opaque**, so WS1 currently does **not** automatically impose a fog-derived geometry cutoff on arbitrary static props. This avoids silhouette-to-clear-sky popping |
| **WS2 static scene residency** | `WORLD/Configuration/WorldMaster.tres → Streaming` (`WorldStreamingSettings.cs`); `WorldStaticStreaming.cs`; `StaticSectionStreamer.cs`; FPS-only `FirstPersonMaster.tres → StreamingHysteresisDistance` | **272m load / 336m unload** for the current 235m FPS clip and 64m hysteresis. Requests on movement, two in-flight reads, one scene operation/frame by default | Currently **only Building001–004** are adopted. These distances are derived from the far clip and footprint, not a 128m-square unload switch |
| **Actor simulation distance** | **NO shared policy exists yet** | Zombies use `ZombieController` physics-on-demand and a repeating `SightIntervalSeconds` brain timer; deer use their own decision timer | No distance-based `Full/Reduced/Dormant` tiers, actor unload or persistent encounter system yet. `EntityCore.Activate/Deactivate` exists but is **not** a ready-made distance sleeper: Deactivate cancels chase/investigation, clears velocity and may alter behaviour |
| **World identity grid** | `WORLD/Streaming/WorldSectionSettings.cs` and `SectionCoordinate.cs` | 128m logical cells, 0–1024m test bounds; F3 grid | Section is a **placement address**, not a fog distance or an instruction to unload all objects in that square |
| **Vegetation streaming grid** | Existing grass/pine/DF2 chunk managers | Usually **32m** batch residency chunks | Controls vegetation creation/removal; separate from WS0 128m ownership. Do **not** merge sizes or create a second foliage streamer |

**Interpretation:** Today you do **not** have one file that authoritatively owns all three distances. A single fog value exists for FPS presentation, but WS1 static render safety, foliage range and WS2 residency have independent policies. Simulation relevance is absent. Do **not** claim changing fog already reduces zombie AI.

### B. Proposed common distance contract — one calculation, several consumers

**Goal:** retain the user's existing fog-driven visual behaviour while making reduced actor simulation begin no later than the point where opaque fog plus the vegetation safety envelope has fully concealed the actor. Stream scenes farther than they are visibly useful; do not make everything unload exactly at the fog wall.

**Proposed sole effective-distance calculator (WS-D0):** add `WORLD/Streaming/WorldRelevancePolicy.cs` and `WORLD/Streaming/WorldRelevanceSettings.cs`, with one typed settings subresource under **`WORLD/Configuration/WorldMaster.tres → Relevance`**. These are **planned files, not current files**. Keep FPS fog visual authoring under `FirstPersonMaster.tres` and the applied fog signal in `PlayerDistanceFog`; reuse the active `VegetationVisibilityPolicy.RenderDistance` as the established foliage boundary and the existing WS2 bridge's camera-safe effective load/unload values. `WorldRelevanceSettings` only introduces genuinely new offsets/tier rates: do **not** clone existing fog, LOD and streaming buffers into a second master. One read-only snapshot provides the effective distances, live subscriptions for consumers belong to later WS-D1, and no new per-frame tree or actor scan is permitted.

**Proposed start values (metres), with current FPS fog ON and opaque at 115m:**

| Derived band | Example boundary | Purpose / proposed response |
| --- | ---: | --- |
| Fog fully opaque | **115m** | Owner-editable presentation distance in FPS master, **not** a scene unload distance |
| **Rendering reference / full AI through** | **139m** = 115 + 24m shared visual safety | Maintain normal actor behaviour near the visible edge. Existing vegetation already uses this envelope. Native static render range and camera clip can remain longer for safety; this is a **simulation relevance reference**, not a false promise that every triangle is culled at 139 |
| **Reduced actor simulation** | **139–203m** | Proposed zombie/deer **decision checks at most 4 Hz**, preferably only on existing decisions, with expensive perception/raycast work reduced or skipped as safe. Preserve necessary movement/physics while in motion; do not globally throttle Godot physics or make moving actors teleport. No distance-based decision system is implemented yet |
| **Dormant actor simulation** | **beyond 203m** = 139 + 64m | Proposed stop of idle brains, optional sound/debug/pose activity, and movement/physics only when safe; keep actor state/scene resident. An active chase, attack, gunshot investigation, player interaction or physics dependency **pins/wakes** the actor even outside the normal band. No loss of health, limbs, AI target or pending interaction |
| **Stream-in / preload** | **272m** effective current FPS static scene preload | Build/collision/actor resources must become available **before** the player reaches the simulation boundary; actual load distance is at least current camera far reach + conservative offset and 32m cushion, not simply fog end + a hardcoded constant |
| **Stream-out / unload** | **336m** effective current FPS static scene unload | Only after hysteresis and **after WS3 state storage** for mutable objects and actor encounter IDs; no disappearing pursuers or automatic respawns. Buildings already use this band; zombie actors do **not** yet |

These **example outputs must be calculated**, not copied as universal constants: for existing FPS settings, `visualEnvelope = 115 + 24 = 139`, `reducedStart = visualEnvelope`, `dormantStart = visualEnvelope + 64 = 203`, `staticPreload = 272`, and `staticUnload = 272 + 64 = 336`. The **139/203 tier thresholds are a proposal**; the 272/336 building thresholds are already in source for the current FPS profile. Static scene preload is derived using the existing `WorldStaticStreaming.Distances(settings, camera.Far, camera.MaximumRenderOffset)`; preserve the footprint-based distance check, camera/far-plane safety and per-actor pins.

**Expected relationship:** `opaqueFogEnd (115) < vegetationRenderEnvelope (139) <= reducedSimStart (139) < dormantSimStart (203) < streamPreload (272) < unload (336)`. This is deliberately **not** three unrelated absolute settings; once a new fog value is applied, the snapshot must update render reference and simulation thresholds together. Adjusting fog longer means more rendering and more actor simulation; shorter means less, with measured safety margins.

**Important caveat — actual visibility varies by type.** Camera frustum, imported mesh LOD, native geometry AABB bounds, shadows, Sky3D opacity, camera far plane and authored caps mean there is no one exact GPU-culling metre for **every** object. WS1 currently preserves FPS objects at longer ranges because `FpsSkyFogOpacity=0.08`; don't impose a 139m hard cutoff on static buildings and risk a visible horizon hole. Actors can be rendered under fog but obscured; confirm the visual result **before reducing animation/AI at 139m**. If a class remains truly visible farther than the selected simulation boundary (fog disabled, transparent art, long-range scope, etc.), extend its full/low-frequency threshold or exclude it from dormancy. Do not use `VisibleOnScreenNotifier3D` alone to sleep off-camera threats.

**Fog OFF / translucent / extreme distances:** never keep the old 115m simulation threshold. Choose a conservative camera- and per-class-visibility-aware fallback, audit the existing vegetation's 160m fallback, and ensure the player cannot see frozen actors. Clamp/range-check inputs; enforce `full <= reduced <= dormant < preload < unload` for actors intended for streaming, while existing building load requirements may be greater. If full visibility exceeds safe streaming reach, increase residency rather than unloading visible content. Teleport/vehicle speed can require anticipatory preloading beyond the walking profile.

**Direction of authority / avoid spaghetti:**

```text
FirstPersonMaster (fog visual end) -> FirstPersonFog/PlayerDistanceFog (applied profile)
                                      |
WorldMaster.Relevance (safety + AI offsets + update rates)
                                      |
                                      v
                         WorldRelevancePolicy snapshot
                       /          |             \
        vegetation visibility   actor relevance  WS2/WS4 preload minimums
        (keep native LOD owner) (future WS4A)    (keep WS2 streamer)
                       \
                    WS1 static render safety remains native bounds/shadow/sky aware

No duplicated hardcoded 115/139/203/272/336m in every species.
```

**Distance metric:** for FPS use player horizontal X/Z distance for **actor relevance/ground footprints** with conservative camera-eye/lean/vertical and object-bound margins; keep the existing camera-spherical fog/clip metric for FPS fog and native render safety. For flying actors or large vertical separations use a documented 3D/bounds exception. For top-down, retain its separate actual fog/camera envelope during the approved later FPS-only conversion; do not silently delete support. Decide on one reference player/observer per world, not a new every-actor search.

### C. How Copilot should implement it (one permission-gated PR per slice)

| Slice | Work | Dependency/status | Acceptance gate |
| --- | --- | --- | --- |
| **WS-D0: audit + contract** | Use **only the existing `WORLD/Streaming/` folder** for the proposed `WorldRelevanceSettings.cs` and pure read-only `WorldRelevancePolicy.cs`; add `WorldMaster.tres → Relevance`. Compose *already effective* fog, vegetation and WS2 streaming values; only the new actor offsets/rates are authored here. Audit consumers; prove 115/139/203/272/336m, fog edits, fog OFF, camera changes, invalid inputs; **no runtime behaviour change** | **Planned; recommended FIRST Copilot task** | No new `WORLD/Relevance/`, no competing fog/LOD/streamer or duplicate tunables, no `WorldStreamingCoordinator`, no per-frame loop; focused unit checks and one build |
| **WS-D1: diagnostics and subscriptions** | Expose F1 effective readout: `Fog opaque, Vegetation end, AI full/reduced/asleep, Static load/unload, Fog on/off, entity tier counts`. Wire to applied-settings and camera events; avoid duplicating WS1 culling and GV1 foliage work. Rendered vs resident metrics must be distinguished | Planned after WS-D0 | Fog slider changes reported distances without restart; no hidden UI cost or extra tree scans when F1 closed |
| **WS-CL0: optional chunk lifecycle coordination audit** | Review existing 32m foliage create/release, WS2 per-placement unload and 128m section identity; document any missing lifecycle notifications and consumer readiness/pinning. Add minimal existing-owner events **only when proven necessary**, without a second chunk manager or terrain rewrite. Reference ProWille as a pattern, **not code to port** | **Planned; after WS-D0/D1, separate authorization** | No duplicate chunk creation, no per-frame world scans, no new top-level folder, no mutable-state loss; potentially documentation-only |\n| **WS4A: actor reduction (without unloading)** | Integrate one zombie/runner + deer or another one species using existing `EntityCore`/brain timers, motion and detection rules; event-driven cohort evaluation on player movement and threshold changes; stagger wakeups. Introduce **Normal / Reduced / Dormant** distinct from `EntityCore.Deactivate` if needed so AI memory is not destroyed. Start from full simulation inside 139m and bounded 4Hz decisions in 139–203m; dormant beyond 203m only if safe | Planned; **can precede WS3 if actors stay resident** | A distant zombie's brain/physics idle work stops or reduces while health/limbs/targets remain; damage, gunshot, movement toward player, nearby threats and ongoing chase wake/pin immediately; no frame-by-frame full-tree scans |
| **WS3: persistent round-trip** | Existing WS3 scope: stable IDs, mutable state delta, missing/remove tombstones, atomic save/load; one movable prop proof before general adoption | Planned, separately authorized | Unload/reload/restart preserves object state and avoids duplicate IDs; no save every frame |
| **WS4B: actor unload/reload** | After WS3, build encounter member IDs, ownership transfer and saved deaths; integrate WS4 actors with the existing static streaming scheduler or compatible bounded owner. Actors can be safely removed beyond 336m only when dormant/unpinned and state committed | Planned, depends on WS3 and WS4A | Killed actors don't respawn on boundary crossing; pursuits remain; actor roots disappear/return; no silent AI/corpse/item state loss |
| **WS-D2: other world families** | Extend measured relevance to persistent props, shop and other remaining static content, collision activation, lights, terrain adapter and future vehicles **by owner**, without double managers or equating fog with physics | Planned after proof | Rendering, simulation, collision and residency counters separately checkable; no overlapping chunk ownership |

**Do not jump from WS2 to unloading all zombies.** `ZombieController` already does a per-actor `Timer` decision at `SightIntervalSeconds` and enables physics when moving; `DeerController` has its own decision timer. A correct distance integration must suspend/reduce those owners without destroying pursuit/health/animation state and must honor sound/hearing wake events. In particular, `EntityCore.Deactivate()` clears velocity, and the zombie's `OnDeactivated()` cancels investigations/chases: it is **not** appropriate as a generic distance-sleep call without changing the lifecycle contract.

### D. Fixed-cost safeguards and developer handoff

- **No one-distance-to-rule-everything GPU claim:** distinguish visual fog end, native rendered bounds, logical simulation tier, 32m vegetation chunk membership, 128m world section identity and live scene residency. Keep physics blockers/collision where movement, LOS or projectiles can reach them; never remove the floor/cover just because hidden.
- **Event-driven state changes:** update calculated thresholds on fog/visibility/clip settings events and batched player movement (existing WS2 uses 8m movement), not once per actor per frame. Tier boundaries need hysteresis (suggest **8m** exit/return buffer) and bounded/staggered activation work. When player teleports, prioritize immediate near actors and collision readiness.
- **Pin exceptions:** chased/attacking/hearing-investigating actors, active melee/projectiles/impact reactions, occupied structures, interactive/moved props, vehicles and in-flight save transactions cannot be unloaded just by crossing a fog threshold. Wake due to sound/damage as appropriate even if previously dormant; retain deduplicated sound subscriptions or an event-facing wake proxy.
- **Test with current 115m fog**, 80m, 200m, disabled and after several fast direction changes; check transparent sky, hillside, high roofs, tall trees and building shadow silhouettes. With current walking/running 5/8m/s the 272m load radius has significant runway, but do not assume future fast vehicles are safe without predictive prefetch.
- **Measurements:** F1 show actual `render end / AI full / reduced / dormant / resident actor count / scene load-unload counts`. Check CPU frame time and GPU separately. Never claim merely hiding a mesh unloads it or makes physics/AI sleep.
- **Run one build and focused F6 check per authorized slice**; Godot/.NET runtime checks not performed by drafting this contract. Report exact changed files and commit SHA. Leave `FPS_ONLY_CONVERSION_IMPLEMENTATION_PLAN.md`, source Terrain3D data, accepted art, current F1 settings and the 4-building WS2 bridge untouched until explicitly requested.

### E. Copy-paste first Copilot assignment (when the owner approves implementation)

> Read `AGENTS.md`, `CodePreferences.md`, root `README.md`, `LessonsLearned.md`, `WORLD/Streaming/README.md`, and `WORLD/WORLD_STREAMING_IMPLEMENTATION_PLAN.md → WS distance-authority refinement`, including **Folder ownership** and **Chunk coordinates vs distance**. On branch `implement-td0-td1`, implement **ONLY WS-D0**: audit existing FPS fog, GV1 vegetation visibility, WS1 static native culling and WS2 static streaming; add typed `WORLD/Streaming/WorldRelevanceSettings.cs` and pure `WORLD/Streaming/WorldRelevancePolicy.cs` (plus an optional small snapshot type only if needed). Author new relevance settings under `WORLD/Configuration/WorldMaster.tres → Relevance`. **Do not create `WORLD/Relevance/`, `WorldStreamingCoordinator`, new foliage/terrain chunk managers, or duplicate existing fog, vegetation and WS2 distance tunables.** Compose a **read-only effective distance snapshot** from applied owner values and new actor offsets. Verify current values **fog 115, vegetation 139, proposed reduced start 139, proposed dormant 203, existing FPS static load 272 and unload 336**, plus sky-opacity guards, camera bounds, live fog changes, fog OFF and numerical invalids. No FPS visual/LOD/streaming behavior changes, no zombie AI sleeping, no actor unload, no WS-CL0 code, and no roadmap phase escalation. One focused build/test where possible, update this plan's WS-D0 status with verified facts, report changed files/commit SHA, then stop for local review.

## Phase overview

| Phase | Deliverable | Dependencies | Suggested executor | State |
| --- | --- | --- | --- | --- |
| **WS0** | Section/identity/placement contract and read-only boundary/ownership proof | Current active world/asset audit | Codex; Regular Chat design review | Implemented; Awaiting Validation |
| **WS1** | Shared fog-aware render/relevance policy for procedural and imported assets | WS0; current fog/vegetation audit | Codex; owner visual check | Implemented; Awaiting Validation |
| **WS2** | Bounded loading/unloading of a few static test sections | WS0; WS1 compatible visibility | Codex | Implemented; Awaiting Validation |
| **WS3** | Persistent state round-trip and safe unload/save lifecycle | WS2; coordinate existing IX8/BI4 plans | Codex; Copilot local checks | Planned |
| **WS4** | Nearby encounter activation and persistent actor ownership | WS3; actor lifecycle audit | Codex | Planned |
| **WS5** | Roaming placement/edit/save workflow using section records | WS0 schema; WS3 storage | Codex; Regular Chat catalogue/UI work | Planned |
| **WS6** | One representative town block and restrained population tools | WS1–WS5 locally validated | Owner layout; Codex integration | Planned |
| **WS7** | Expand geography and tune traversal/preload budgets | Proven block; later vehicle requirements | Codex; owner/Copilot profiling | Planned |

Each phase is independently authorized. Complete implementation is not acceptance: Planned → In Progress → Implemented → Awaiting Validation → Accepted. Do not implement later phases because this roadmap exists.

## Current source reality and existing owners

- `World.tscn` is the primary game; inherited `WORLD/FirstPerson/FirstPersonWorld.tscn` remains a compatibility/debug view. F5 still points at parked `testworld.tscn`; do not change it here.
- `WorldController` initializes terrain, village, actors, environment, equipment and HUD. `VillageLayout` measures prefab footprints and reserves approaches/patrols; `Generation/VillageTerrain` grades the existing terrain during initialization. These are not a persistent whole-world streamer.
- Native terrain is currently one saved gameplay area. Startup requests full-game terrain collision. Vegetation has existing player-centred batch/chunk lifecycles; pine placement already configures visibility ranges. Reuse those mechanisms, not a second independent forest manager.
- `ENVIRONMENT/Fog/PlayerDistanceFog.cs` supplies distance-fog presentation. Its gameplay/player-centred distance and optional camera-distance policy differ from native geometry visibility ranges. Thick fog is an artistic constraint; it is not automatically CPU suspension or memory unloading.
- `WORLD/Content/Assets/` is the canonical reusable library. Procedural C# and imported GLBs coexist; GLB visual wrappers do not universally inherit every procedural tree's batching, wind, collision, LOD or fade behavior today.
- Persistent actors/props have stable scene-tree roots from the existing restructure. Existing scene names, structure visibility, world-local AUDIO/hearing, player/HUD and prefab metadata must survive section activation/removal.
- There is no confirmed general saved placement catalogue, mutable world-state database or persistent town-section streamer in this source. Review actual code again before introducing one. Repository history in another project is not evidence this project already has those systems.

Related plans: [world hierarchy](WORLD_SCENE_RESTRUCTURE_PLAN.md), [village](VILLAGE_IMPLEMENTATION_PLAN.md), [surface](SURFACE_IMPLEMENTATION_PLAN.md), [caves](Content/Caves/IMPLEMENTATION_PLAN.md), [hybrid houses reference](../NOTES/Ideas/PROCEDURAL_HOUSES_AND_HYBRID_WORLD.md), [interaction IX8](../INTERACTION/IMPLEMENTATION_PLAN.md), [inventory BI4](../UI/GameplayHUD/Inventory/IMPLEMENTATION_PLAN.md). This roadmap coordinates world lifetime; those owners retain gameplay, inventory and generation responsibilities.

## Shared design contracts

1. **Separate stored description, live scene and saved changes.** Asset definitions identify reusable prefabs. Placement records identify authored instances. Mutable state records describe what happened to each instance. A missing scene instance is not a missing building in the world description.
2. **One owner per instance.** Choose an anchor-based section owner; index footprint overlap separately for loading dependencies. A house/site crossing a boundary is instantiated once, not once per touched section. Roof, lamps, children and shared references cannot create duplicate copies. Stable instance IDs survive scene reload, save/reload, asset-path changes and moving an object.
3. **Separate render, simulation and residency.** Geometry can stop drawing behind fog while collision/AI required for gameplay remain. Fully distant sections release live scenes only after state is preserved. Global sky, environment, player, camera and HUD are persistent services, not ordinary culled section props.
4. **Treat distance consistently.** Define the gameplay reference position and camera/frustum envelope explicitly. Do not assume a camera-based 3D visibility distance equals the player's horizontal fog distance. Use object/batch bounds for large buildings, tall trees and multi-cell sites. Light/shadow behavior needs its own audit, not just hiding mesh nodes.
5. **Load earlier than unload.** Keep a hysteresis band. Preload beyond the visible/interactive area and enlarge the requested region for speed/camera look-ahead. Collision must be ready before entry. A pursuing actor or ongoing interaction can temporarily pin a section beyond its normal distance.
6. **No arbitrary despawn at the fog wall.** Combat, moving actors, vehicles, ownership transfers and a player's interior occupancy require leases/explicit ownership. Forgetting a zombie is not killing it, and reloading a spawner is not permission to respawn its killed population.
7. **Authoring and play saves differ.** Building/hedge placements belong to the authored map; dead actors, moved items and collected loot belong to runtime saves. Do not overwrite source scenes/terrain from a gameplay save or rebuild mutable objects from seeds after they have changed.
8. **Bound work and memory.** No synchronous whole-town generation or whole-scene scan per frame. Background resource work must use supported engine-safe APIs; scene-tree/physics attachment happens on the main thread with a budget. Cancel stale loads, deduplicate requests, cap in-flight jobs and release resource references when appropriate. Removing nodes alone does not guarantee shared mesh/texture memory is freed.
9. **Terrain is a separate adapter.** Logical placement sections need not equal Terrain3D native region sizes. Do not slice/rebake the current terrain as a side effect of object streaming. Audit height/material residency and collision independently before promising a large terrain-streaming solution.

## WS0 — Section ownership and saved placement foundation

**Objective:** Establish exactly what a section means and how instances belong to it before anything disappears from the current world.

**Deliverables / owning files (proposed; create only what is needed):**

- A small `WORLD/Streaming/` owner with README and plain section-coordinate/placement-record helpers. Keep C# model/controllers out of art `C/` folders. Reuse an existing equivalent if the live audit finds one.
- A typed world manifest with world ID, schema version, seed, logical finite bounds and configurable section size. A **128m X/Z square** is a starting proposal to test, not a locked requirement or a terrain rebake request. Store consumed settings through `WORLD/Configuration/WorldMaster.tres` or its typed nested profile, not scattered constants.
- Placement records containing persistent instance ID, stable asset ID, owning section, world transform, footprint/dependency bounds, authoring seed and supported configuration overrides. Resolve asset IDs to canonical `.tscn` paths via one small catalogue/adapter. Do not save only a temporary Godot object instance ID or regenerate authored GUIDs on every load.
- A read-only diagnostic under `DEBUG/WorldStreaming/` showing the player's section coordinate, nearby section bounds and ownership for a few explicitly selected existing placements. No extra debug callbacks while disabled; avoid conflicting with existing F1/F7/F9 controls.
- A concise migration map for the convenience-store site and representative building/vegetation/actor owners. The composed store site can start as one owned placement; a later split must not duplicate its prefab children.

**Approach:** audit current sources; pick a fixed world origin; compute section coordinates using floor division (including negative coordinates); assign owners by a documented anchor rule; use bounds overlap only to request dependencies. Represent existing scene objects in a diagnostic manifest without reparenting, replacing or duplicating them. Validate serialization of the manifest/placement description separately from a full gameplay save.

**What the first result looks like:** the normal World still runs. An explicitly enabled debug view shows an X/Z grid and the current section ID while walking across boundaries. The store is listed once under its owning section even if its bounds touch another. No houses vanish, no zombie spawn policy changes, and no fog/terrain parameters change. This is a contract/ownership proof, not completed streaming.

**Out of scope:** unloading existing World content, background loading, player-state saves, runtime mutation persistence, replacing pine art, a giant terrain generator, town construction, a drivable car or a complete editor plugin.

**Configuration/preserve:** use runtime copies; stable authored IDs/seed/transform survive manifest round-trip. Preserve World startup, terrain data, existing village transforms, NPC AI, collision, player/HUD, weather/fog and FPS inheritance.

**Performance:** section lookup is arithmetic; recompute nearby diagnostic cells on cell changes/config changes. The optional debug view wakes only while enabled; no section renderer or background tick exists when off. No eager loading of scenes referenced by the logical catalogue merely to draw the grid.

**Pass:** correct positive/negative coordinates and boundary crossings; finite bounds handled explicitly; stable IDs after serialization/reload; one owner for a boundary-spanning prefab; no new duplicates or runtime behavior changes; diagnostics OFF sleeps. Brief build/F6 where available. Codex owns the cross-system audit/schema; Regular Chat can review data/layout choices; Copilot/owner validates locally.

## WS1 — Fog-aware rendering and relevance

**Objective:** give both procedural geometry and GLB assets a shared policy without losing batching or gameplay collision.

**Scope/files:** existing environment distance settings, pine/grass owners, reusable asset wrappers and a small streaming render-policy adapter where needed. Audit mesh, MultiMesh, light, shadow and imported-model paths. Replace a pine's visual model through its existing placement owner as a separate explicit asset-integration choice, not thousands of standalone GLB scene roots by default.

**Approach:** record each consumer's current cutoff metric; establish tunable visual cutoff/fade and preload envelopes that hide transitions in the thick fog across overhead zoom and FPS. Apply on registration/settings changes. For repeated vegetation, keep spatial batches and source meshes shared. Large props require bounds-aware handling. Suspend optional cosmetic work outside relevance; do not silently suspend essential AI or remove colliders.

**Out of scope/preserve:** no actual section unload/save, new fog appearance or global hide-every-Node3D loop. Preserve cover, weapon range/collision, structure fading, wind and existing vegetation ownership. Procedural versus imported is a visual-source distinction, not a different lifetime rule.

**Cost/pass:** no per-object polling; shared bounded distance updates only where necessary, native visibility where appropriate. Check distant GLB/procedural equivalence, edge popping, camera zoom/turn, tall objects and shadows. Measure one dense comparison locally; no claimed FPS gain without measurement. Codex integration, Regular Chat visual tuning, owner/Copilot acceptance.

## WS2 — Small static-section streaming proof

**Objective:** load and unload a few sections without a town-sized workload.

**Scope/files:** `WORLD/Streaming/` lifetime/request scheduler and an isolated `DEBUG/WorldStreaming/` test scene with a few static placement records. Opt into the active world only after the isolated proof works. Reuse stable World roots and resource/prefab lifetime hooks.

**Approach:** request nearby bounds on movement/cell changes, preload farther than visibility, unload only beyond a larger hysteresis boundary. Background-load resources where supported; budget main-thread instantiation, collision readiness and teardown. Pin sections needed by player/camera and dependencies. Start with static objects whose state does not change; use one site ownership record across boundaries. Terrain remains the existing area unless a separately verified terrain adapter is ready.

**Data/preserve:** manifest and source resources are read-only; no saving mutable world state yet. Do not unload player, global systems, ongoing encounters or current interactive items in this proof. No runtime terrain rebake.

**Cost/pass:** queue/in-flight/resident instance caps, canceled obsolete requests, no duplicate load or boundary thrashing, settled scheduler sleeps. Walk away/return repeatedly and reload scene; verify one scene per ID, footprint dependencies and no missing collision/pop-in. Record actual timings/memory locally, including first-use shader/physics spikes. Codex implementation; Copilot/owner brief scene checks.

## WS3 — Persistence before mutable unloading

**Objective:** preserve changed objects across unload/reload and application restart.

**Scope/files:** a minimal versioned world-state repository with stable instance records and a world persistence adapter; coordinate IX8/BI4 rather than inventing separate item-save authorities. Keep authored placement data separate from runtime state deltas and support unknown/missing assets without silently deleting their saved state.

**Approach:** choose a small authorized mutation contract (e.g. one moved placed prop) for the first test. Capture committed state before unload; restore before exposure to gameplay. Record removal/tombstones, changed transform and deterministic generator version/seed. Dirty writes are bounded and safe/atomic; write failure must retain unsaved state/instance rather than silently discarding it. Save active mutable sections on explicit save/shutdown as supported; no disk write per frame. Document schema upgrade policy and world IDs.

**Out of scope/preserve:** no full inventory/player/medical save or complete destructible-town simulation. Do not claim persistence for state types without a tested adapter. No respawn/reset simply because an asset leaves memory.

**Cost/pass:** event-driven dirty tracking and bounded storage work; no idle serialization. Move/remove the test object, unload/return, restart and verify the same identity/state once; test failed write, missing asset, stale/canceled load and scene exit. Codex for ownership/storage, Copilot local filesystem tests.

## WS4 — Encounters and mobile actors

**Objective:** placed encounter descriptions activate relevant zombies without keeping every actor alive across the map.

**Scope/files:** world-owned encounter/spawner records/adapters, ENTITIES lifecycle integration and persistent population state. Compose through existing health, sensing, hearing and movement; actors still own their AI. No zombie attack feature is implied.

**Approach:** a stable encounter ID plus seeded member IDs; near-player activation after ground/clearance checks; bounded per-frame spawn work and total active population cap. Save killed members and surviving state according to an explicit minimum contract. Pin pursuits/combat/interaction beyond fog; sleep or serialize genuinely irrelevant actors. Define actor ownership transfer across sections before unloading their former section. Spawn-point authoring does not itself instantiate its entire population.

**Out of scope/preserve:** no new navigation, infinite hordes, offline full-AI simulation or automatic population respawn. Existing visual/hearing pursuit and saved deaths must survive nearby lifecycle decisions.

**Cost/pass:** activate on relevance crossings, not a timer per dormant spawner. Verify killed zombies do not return just by crossing a boundary, survivors are not duplicated, pursuits remain continuous, invalid spawn candidates are skipped safely, listeners/audio subscriptions release and active counts stay bounded. Codex; owner/Copilot local encounters.

## WS5 — Roam, place, edit and save

**Objective:** the owner can build the town using canonical scenes and persistent authored placement records.

**Scope/files:** `DEBUG/WorldAuthoring/` tool or a focused extension of the existing F1 PlaceableSpawner, backed by WS0 records/WS3 storage. Existing F1 previews are temporary; do not pretend their current spawn is persistent map authoring.

**Approach:** select catalogue asset; ground-aware placement ghost; rotate/move/delete with a clear selection; retain/duplicate configuration intentionally; save/reload authored placements and stable IDs. Add basic undo or an isolated draft file so experimenting does not damage the map. Respect terrain footprint/slope and protected entrances, then add authored region/spawner markers. No silent terrain flattening when dropping a house. Keep play-save state separate from editing the baseline map.

**Out of scope/preserve:** no giant editor framework, furniture autopopulation, terrain sculpting rewrite or procedural road network in the first tool. Preserve canonical prefab identity, standalone assets and existing debug previews.

**Cost/pass:** explicit actions activate placement checks/previews; tool OFF has no input/polling/ghost. Place several structures/hedges and an encounter marker across sections, save/restart and see identical placements, IDs and variants without duplicate gameplay instances. Regular Chat can own isolated catalogue/UI polish; Codex owns placement/storage integration.

## WS6 — Representative built-up town block

**Objective:** prove density, layout and practical rendering budgets before scaling the town.

**Scope/files:** authored block/site placements, reusable Assets and constrained generation settings. Owner lays out an intersection, houses/shops, rear alley/service access, parked cars, lamps, benches and hedges. Reserve street/footpath/entrance/vehicle clearances before decoration. House variants use validated layouts and legal furnishing sockets.

**Approach/data:** hand-place the main composition; use deterministic optional tools for compatible lamp spacing, hedge runs and minor dressing. Do not generate random houses into arbitrary gaps. Vary a few well-designed assets before creating hundreds. Keep lamp visibility/relevance separate from shadow budgets; audit interior detail, transparent materials and draw calls.

**Out of scope/preserve:** no giant town at once, new traffic/driving AI, general procedural architecture solver or automatic medical/loot gameplay. Streets and alleys remain walkable; structure visibility and effects respect their existing owners.

**Cost/pass:** compare the same dense block at representative day/night views on the target PC, with GLB/procedural assets, active encounter limits, collisions and save/reload. Count resident instances/materials/lights and capture bottlenecks before changing budgets. Owner directs art/layout; Codex integrates cross-feature tools; Regular Chat handles isolated asset work.

## WS7 — Expand the world and prepare faster traversal

**Objective:** grow from a validated block into connected town districts and surrounding geography without unbounded startup/memory costs.

**Scope/files:** manifest bounds/section catalogue, terrain adapter and relevance budgets; future vehicle ownership/preload requirements. Expand a populated perimeter gradually. Design forest/hill/cave connections against road and town footprint first. A large low-detail layout map can remain lightweight and separate from loaded gameplay sections.

**Approach:** choose final-ish world extent only after travel/scale trials. Validate Terrain3D region/material/height/collision residency, cross-section caves/sites, async completion and GPU resource retention. Reserve preload distance from maximum intended speed, measured load latency and camera envelope; use conservative readiness fallback if required collision is not ready. A drivable car is a separate feature with stable mobile identity, ownership transfer and save adapters, not added by this phase automatically.

**Out of scope/preserve:** no implicit infinite world, floating origin, multiplayer or offline simulation. Add large-coordinate support only if measured/world-coordinate needs justify it. Keep the original gameplay area and saves compatible or migrate deliberately.

**Cost/pass:** long traversal and return, simulated faster travel, bounded resident memory/AI/lights, no holes/pop-in/deadlocks, no lost state at section edges, capped request queues and idle work. Codex integration; owner/Copilot targeted profiling/acceptance.

## Decisions to make during WS0, not blockers for writing this plan

- Initial logical world bounds and origin; prototype section size (128m proposal) and maximum site footprint.
- Asset ID resolution and authoring storage format using current Godot/C# facilities.
- Which few current objects become diagnostic manifest records, and which always-resident roots stay excluded.
- How existing vegetation batches map to section relevance without overlapping ownership; how current fog metrics constrain render envelopes.
- Record terrain streaming/collision limitations honestly. A working static-object streamer is not proof of a streamed gigantic terrain.

**Next step after WS0/WS1 local acceptance:** separately authorize WS2. Do not automatically implement them.

## WS0 implementation handoff — 2026-10-28

Added typed section defaults under WorldMaster, a plain finite manifest/placement/asset-ID model, explicit five-instance bootstrap identities and startup read-only capture from the actual store/building nodes. Geometry and scene transforms are not changed. The store owns its composed children once; overlap cells are dependencies, not duplicate instances. The initial logical bounds match the existing 1024m test area; default cell size is 128m with origin (0,0). Negative-coordinate support is tested separately with configurable negative bounds. Descriptions round-trip in memory, preserving IDs/transform/seed/override strings; no disk gameplay save exists.

The requested top-centre heading shows current section (default spawn 4,4), with an explicit outside-bounds status. F3 toggles a nine-cell map of section IDs, owner counts and footprint outlines. Heading updates at cell/bounds changes from existing movement signals. No new Process/PhysicsProcess/timer, actor lifecycle changes, terrain modification or culling. Both displays OFF removes movement subscriptions; input-only F3 remains. Existing help is shifted below the heading. The optional map scales to smaller viewports on resize events only; the heading stays anchored.

Read [Streaming/README.md](Streaming/README.md) for the migration/exclusion map, limitations and identity rules; [DEBUG/WorldStreaming/README.md](../DEBUG/WorldStreaming/README.md) for test commands. Source/resource/whitespace checks performed. `dotnet build war-machine.csproj` could not run because dotnet is absent (exit 127); Godot is absent too. Contract smoke test, world/FPS heading/grid/owner checks, layout and visual acceptance are pending. No measured performance claim. Delivery SHA is recorded in the job response; this handoff is part of that commit.

## WS1 implementation handoff — 2026-10-28

Added WORLD/Rendering native range policy for static props and mesh vegetation, with conservative conversion from the actual overhead/FPS fog metrics. GLBs register through normal scene additions, including F1 previews; procedural batches keep their mesh/material/LOD/collision owners. Geometry bounds, fog edge variation, camera offset/height, small wind padding, margins and shadow buffers protect the cutoff. Local lights get native influence-aware fades only when no authored fade already owns them. Fully opaque fog is required; OFF/translucent restores original ranges/fades. The FPS clear-sky profile retains its existing far clip/authored geometry ranges to avoid fog-silhouette-to-sky popping; new geometry caps require an opaque backdrop (normal overhead already has one). FPS local lights still participate. Pine metadata preserves its currently authored LOD ends through zoom/rebuilds. Existing shorter draw ranges are retained.

WorldMaster RenderPolicy fields and the debug panel's WS1 toggle/buffers are wired in both views. No new visibility/transparency writer, mesh generation, source asset replacement, terrain edit, collision/AI suspension, extra shadowed lights or section unload. Native GLB mesh LOD is retained rather than faked. Actual forest replacement remains a separate artwork integration; existing imported-Mesh batches are supported. GPU grass, native terrain, actors and global effects retain their current relevance owners.

Read [Rendering/README.md](Rendering/README.md) for precise coverage, dynamic edit/exemption hooks, authored-range composition, idle/lifecycle behavior and long-shadow/transparent-art caveats. Source/resource checks and policy arithmetic review performed. `dotnet build war-machine.csproj` failed to start (tool absent, exit 127). Godot is absent: RenderPolicyTest, GLB import/LOD inspection, world/FPS day/night/zoom/edge checks and performance measurement remain pending. No measured gain or visual acceptance claimed. WS2–WS7 remain unimplemented. Delivery SHA is recorded in the job response.

### WS1 zoom regression follow-up — 2026-10-28

Owner reports scrolling hitches disappear with WS1 disabled. Removed full scenery refreshes from ordinary zoom/settings events, using a conservative maximum-orbit envelope instead. Added a refresh-count regression assertion. Source checks only; compilation, repeated-scroll A/B and steady FPS remain awaiting local validation. WS2 remains deferred.

### WS2 isolated proof handoff — 2026-10-28

Implemented StaticSectionStreamer with bounded manifest/requests/resident roots, threaded resource reads, per-frame scene-operation budget, footprint-based hysteresis, stable-ID deduplication, obsolete-request draining and idle sleep. StaticStreamingProof.tscn provides five section-boundary pairs (procedural C# car + GLB hedge), collision walker and live counts. Config lives in DEBUG/WorldStreaming/Configuration/StreamingProof.tres. No active-world opt-in until the isolated proof is locally validated; World.tscn, village ownership, terrain, AI and WS3 persistence remain untouched. Scope and limitations are in Streaming/README.md; executable checks/handoff are in DEBUG/WorldStreaming/README.md. Source/reference/whitespace review only; compilation, runtime collision, first-use stalls and memory remain unverified here. Delivery SHA is in the job response.

### WS2 active-world integration handoff — 2026-10-28

Owner demonstrated isolated proof unload/return counts, then explicitly authorized real-world integration. WorldStaticStreaming now adopts existing Building001–004 after startup in World.tscn and inherited FPS, preserving terrain/layout/names/transforms. Actor footprint pins protect occupied scenes; existing StructureVisibility/CameraOcclusion and WS1 lifecycle hooks handle reload. Clip/orbit-derived conservative preload, hysteresis and the same capped scheduler apply. WorldMaster exposes startup opt-out and streaming profile; section heading shows real residency/load/unload status. Convenience-store composite, roads, terrain, vegetation, actors and F1 previews retain existing owners. No WS3 persistence or wider-town migration. Source/reference checks only here; compilation, the supplied integration contract smoke test and local World/FPS collision/fading/performance acceptance remain pending. See Streaming/README.md and DEBUG/WorldStreaming/README.md. Delivery SHA is in the job response.

### FPS distance tuning — 2026-10-28

Owner authorized roughly 45s sprint-equivalent unload using the observed 50s baseline, plus 15% farther opaque FPS fog. FPS fog edge 100→115m; eye-camera allowance 4m and FPS-only configurable 64m hysteresis produce preload/unload 272/336m (formerly 288/368). Empirical estimate 50×336/368≈45.7s, no timed despawn. Top-down unchanged. Source/math checks only; compilation and same-route FPS pop-in/collision/timing validation remain local. See FPS README.
