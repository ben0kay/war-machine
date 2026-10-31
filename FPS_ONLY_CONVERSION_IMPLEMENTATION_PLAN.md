# War Machine — FPS-only Conversion Implementation Plan

> **Decision (2026-10-31):** War Machine will be a **first-person-only** survival game. Remove the playable top-down/isometric camera, overhead controls, overhead aiming, scenery/roof fading for that view, and supporting compatibility code. Keep the existing 3D world and survival systems.
>
> **Repository:** `ben0kay/war-machine` · **active branch:** `implement-td0-td1` (NOT `main`).
>
> **Status:** **PLANNED — no migration code has been implemented by creation of this document.** The owner has approved the **direction**, not automatic execution of every phase. Implement only the phase explicitly assigned, in order, and stop for local FPS acceptance.
>
> **Primary instructions:** [AGENTS.md](AGENTS.md) · [CodePreferences.md](CodePreferences.md) · [README.md](README.md). The root README and some feature docs currently describe top-down support; those are *current-code/historical descriptions*, not a reason to retain it indefinitely. Reconcile them as phases are implemented.

## Objective and final architectural contract

Make `WORLD/World.tscn` the single canonical **FPS gameplay world** with the existing terrain, village, buildings, entities, simulation, interaction, UI, environmental systems, vegetation and streaming. F5 should launch that canonical scene. FPS controls, weapon presentation, eye camera, leaning, fog and rendering become the standard path, not a separate compatibility mode.

**Do not rebuild the game.** Migrate the proven FPS adapters into the world cleanly; remove the unused overhead code only after FPS dependencies have been redirected. Continue to support all existing gameplay capabilities in one scene, with a predictable scene-root structure and no new per-frame managers.

### Current scene graph (source-checked)

```text
project.godot F5 -> WORLD/testworld.tscn              [parked wilderness experiment]
WORLD/World.tscn -> WorldController + PLAYER/Player.tscn + CAMERA/Camera.tscn
                   |  World simulation, terrain, actors, props, vegetation, HUD, systems
                   |  Currently top-down defaults
                   +-> WORLD/FirstPerson/FirstPersonWorld.tscn [inherited scene]
                          FirstPersonWorld : WorldController
                          FirstPersonPlayer : PlayerController
                          FirstPersonCamera : Camera
                          FirstPersonWeapon, FirstPersonInteraction, FirstPersonFog
```

**Desired scene graph (conceptual; no blanket renames required):**

```text
project.godot F5 -> WORLD/World.tscn               [canonical FPS game]
World
  TerrainRoot/Terrain3D
  WorldActorsRoot
    PersistentEntitiesRoot/{Player [FPS], Zombies, Wildlife, RobotPatrol}
    PersistentPropsRoot/{Structures, Objects, Cars, ...}
  Camera [FPS eye camera]
  GameplayHUD
  DebugUIRoot [F1 controls, spawner, FPS diagnostic, colour grading]
  SystemsRoot/{Audio, VegetationRoot, EnvironmentRoot, world streaming...}
```

Keep **semantic existing roots and stable resource/prefab paths**. The exact runtime `FirstPersonWorld` / `FirstPersonCamera` / `FirstPersonPlayer` class names may remain until safe to simplify; the objective is **one active view**, not a risky cosmetic mass-rename.

### Non-goals and invariants

- **No** new terrain technology, regeneration/rebake, village layout, asset replacement, reworked buildings or unrelated player/combat/AI changes.
- **Preserve:** Terrain3D saved gameplay terrain, graded roads and village; zombie/robot/deer behaviour, hitboxes/anatomy, corpses, health/stamina, inventory/hotbar, guns/melee/reload, interaction, physical doors and object catalogue; scene-owned master configuration, F1 debug/spawning/colour grade, weather, Sky3D, ground haze, FPS distance fog, foliage, LOD, chunk/static streaming and future persistence interfaces.
- **Preserve real geometry and physical contracts:** roof meshes, walls, floors, stairs, railings, openings, shutters, rigid-body door leaves, collision shapes, reach/LOS, bullet obstruction, footprints and the authored police-station editor-preview controls. Deleting a transparency effect is NOT deleting a roof.
- **Preserve haze footprint tags:** `StructureLayout.Occupied` and `structure_occupied_bounds` are used by `GroundHaze.SetBuildingExclusions(...)`. These must still work after any visibility/occlusion cleanup. Likewise preserve other building layout, streaming or placement metadata that has non-fade consumers.
- Distinguish camera-obstruction fading from **legitimate rendering**: normal mesh visibility, fog, far clipping, LOD transitions, static streaming, occlusion culling, colour grading, shadows and transparent glass remain relevant in FPS.
- `WORLD/testworld.tscn` and `PLAYER/LegacyFirstPerson/` are **legacy experiments**, not the playable top-down mode. Change F5 entry in FPS0; do not delete those assets without a separate explicit decision.
- No broad API/folder migrations, forced Git reset/checkout, overwriting local edits, merges to `main` or unrelated work. Use the branch specified above, one logical commit per approved phase, and proportionate tests.

## Phase status and execution order

| Phase | Purpose | Primary owners | Dependency | Recommended assistant | Status |
|---|---|---|---|---|---|
| **FPS0** | Launch the existing FPS scene by F5; establish working baseline | `project.godot`, FPS entry documentation | None | Regular Chat / Codex | **Planned** |
| **FPS1** | Promote FPS scene composition into canonical `WORLD/World.tscn` | `WORLD/`, `PLAYER/FirstPerson/`, `CAMERA/FirstPerson/`, gameplay composition | FPS0 accepted | **Codex / Work** | **Planned** |
| **FPS2** | Retire overhead camera/input/aim/recoil/interaction/debug adapters | `CAMERA/`, `PLAYER/`, `EQUIPMENT/`, `INTERACTION/`, `DEBUG/`, world settings | FPS1 accepted | **Codex / Work** | **Planned** |
| **FPS3** | Retire top-down building/foliage fade and overhead fog while retaining FPS visuals | `CAMERA/Occlusion/`, `WORLD/Content/Assets/Structures/`, `VEGETATION/`, `ENVIRONMENT/` | FPS2 accepted | **Codex / Work** | **Planned** |
| **FPS4** | Remove obsolete references/resources/tests; make all instructions FPS-only | root docs, feature READMEs, scene/resource references | FPS3 accepted | Regular Chat / Codex | **Planned** |

**Status vocabulary:** Planned → In Progress → Implemented → Awaiting Validation → Accepted. Source/commit success is not owner visual acceptance. Do not automatically start the next phase; stop after the explicitly requested phase.

## Dependency inventory — inspect the live code again before deleting

The following paths and relationships were examined on `implement-td0-td1` on 2026-10-31. **Candidate removal** is not a command to delete a file without inspecting current consumers.

| Area | Confirmed coupling | Migration instruction |
|---|---|---|
| `WORLD/World.tscn` and `WORLD/WorldController.cs` | World composes shared actors, terrain, equipment, interaction, weather/fog, vegetation, debug and streaming. FPS inherits this world. | **Keep and promote**. Preserve initialization order and stable root paths. |
| `WORLD/FirstPerson/FirstPersonWorld.tscn/.cs` | FPS overrides root/player/camera scripts plus equipment, interaction, occlusion, fog, debug crosshair, foliage and static-streaming settings. | Transplant active FPS composition without duplicating World nodes. Retire redundant inherited entry when no longer referenced. |
| `CAMERA/Camera.cs` → `CAMERA/FirstPerson/FirstPersonCamera.cs` | FPS subclasses overhead camera, but still uses its inherited notification/settings contract (`ViewChanged`, `SettingsChanged`, render offset/range notifications, initialization). | Strip overhead orbit/follow/projection ONLY after retaining the minimal API needed by FPS/rendering/streaming/debug. Do not delete base class first. |
| `PLAYER/PlayerController.cs` → `PLAYER/FirstPerson/FirstPersonPlayer.cs` | Shared physics, stamina, capsule, stance, visual socket/probe, death/input and events; FPS overrides movement direction/braking and camera pose capture. | **Keep shared physical actor**; make FPS movement canonical. Remove screen-relative overhead axis caching only after all consumers are redirected. Keep invisible rig/probe mounts where gameplay uses them. |
| `PLAYER/Movement/PlayerMovement.cs` | Caches overhead camera-projected movement axes and `Camera.SettingsChanged` subscription; also owns gravity, movement and physical door pushing. | Remove only the overhead-axis path; keep gravity, contact impulses, collision, stamina and sleep behavior. |
| `PLAYER/Equipment/PlayerWeaponController.cs` and `PLAYER/Equipment/TopDownWeaponRecoil.cs` | Top-down cursor aim and recoil, including `FadeAwareAim`; not the shared gun damage engine. | Remove after redirecting slot/HUD/interaction consumers to the FPS weapon adapter. |
| `EQUIPMENT/Weapons/FirstPerson/FirstPersonWeapon.cs` | FPS center-screen targeting, gun visual placement/recoil, reuses weapon slots and existing firearm pipeline. | **Keep**; preserve reload, accepted-shot events, recoil, melee/shove, ammo, interaction arbitration and HUD. |
| `INTERACTION/ViewAdapters/InteractionView.cs` | Plain `TopDownInteraction` vs `FirstPersonInteraction` implementations, shared `IInteractionView`. | Keep center-screen and mouse-capture rules. Remove cursor adapter; simplify interface only if no useful contract remains. |
| `INTERACTION/Shared/InteractionTargeting.cs` | Optional `FadeAwareAim` selection bypass for transparent top-down buildings, then mandatory physical reach/cover check. | Use normal FPS ray; **retain solid reach/cover physics**. Remove faded-selection branch once safe. |
| `DEBUG/Overlay/DebugOverlay.cs` | Shared weather, time, fog, foliage, visual grading and actor diagnostics plus overhead camera/occlusion controls. | Remove overhead Camera/zoom/projection and fade UI; keep F1, FPS tuning, spawner, hue/saturation/contrast and debug exports. |
| `ENVIRONMENT/Fog/PlayerDistanceFog.cs` | Supports overhead player-centered metric **and** FPS camera-distance metric used by `FirstPersonFog` and `WorldRenderCulling`. | Remove overhead mode, **not FPS fog/render policy**. Preserve fog-linked camera far clipping and native render distance behavior. |
| `WORLD/Rendering/WorldRenderCulling.cs` | Subscribes to camera signals and uses `MaximumRenderOffset`, `PlayerDistanceFog` and scene roots. | **Keep**, adjust any camera contract only after verifying subscriptions and residency/range math. |
| `CAMERA/Occlusion/` | `CameraOcclusion`, `CameraOccluder`, fade targets/providers, `FadeAwareAim` serve overhead transparency/cursor exceptions. | Remove after all source/scene refs are unwired; do not remove generic math needed for building occupancy. |
| `WORLD/Content/Assets/Structures/Shared/` | `StructureVisibility`, `BuildingFloorVisibility`, `BuildingObstructionVisibility` implement floor/roof/outside fades. `StructureLayout` also defines physical/occupied metadata. | Remove fade runtime components, keep geometric metadata/collision/preview contracts. Extract shared bounds transforms if needed before deleting occlusion math. |
| Existing building prefabs | `Building001`–`004`, `PoliceStation001`, `ConvenienceStore001` each instance a `StructureVisibility` child. | Remove only visibility child and resource linkage, not generator, scene, colliders, layout, furniture or roof. Check nested store site. |
| `VEGETATION/Trees/Pine/PinePlacement.cs`; `VEGETATION/Forest/ForestPlacement.cs` | Implement `ICameraOcclusionProvider` and per-instance canopy fade tracking. FPS does not register fade provider. | Remove only fade interface/targets/caches; retain seeded chunks, MultiMesh batches, wind, collision, LOD and shared tint. |
| Foliage shaders | `VEGETATION/Trees/Pine/Shaders/LeafWind.gdshader`; `VEGETATION/Forest/Shaders/CanopyFoliage.gdshader` encode overhead opacity. | Remove unused fade shader branches carefully; preserve authored vertex colours, palette recovery, material variants, wind and any MultiMesh custom-data channels still required. |
| `WORLD/WorldSettings.cs` | Has `Horde preview (top-down only)` settings. Another store-stress population is shared. | Retire view-specific enable/offset defaults only if unused; **keep** useful FPS-compatible zombie stress testing. |
| `DEBUG/Interaction/InteractionTargetingTest.cs` | Tests FPS center ray, old overhead projection/faded selection and physical reach. | Keep FPS target/reach/solid-cover regression cases; update obsolete overhead-only assertions, not all interaction tests. |

### High-risk traps

1. **A C# base class may look top-down but be required by FPS.** `FirstPersonCamera : Camera`, `FirstPersonPlayer : PlayerController`, `FirstPersonWorld : WorldController`: first move their required contracts and all scene/resource references, then remove old behaviour. Avoid a giant rename pass.
2. **Structure occupancy is not camera visibility.** `StructureLayout.Read` currently uses `CameraOcclusionMath.TransformBounds` for local occupied volumes; move that reusable transform math to a neutral geometry helper (or equivalent) before removing `CAMERA/Occlusion`. Recheck `GroundHaze.SetBuildingExclusions(...)`.
3. **Building art sometimes calls fade-related helpers.** Remove `StructureVisibility` scene children, `CameraOccluder.DynamicFadeVisualMeta`, `StructureLayout.FadeGroup`, `FadeAwareAim.Link`, mesh status flags and inherited `StructureSettings` fade exports only after a repo-wide search confirms their callers and settings resources are addressed. Keep working hinged door bodies and correct per-piece collision.
4. **Shared FPS fog uses an overhead-named class.** `FirstPersonFog.Fog` is a `PlayerDistanceFog` in camera-distance mode. Deleting `PlayerDistanceFog` would break both atmosphere and render culling.
5. **Player rig is not merely top-down decoration.** Hidden FPS art can still own `WeaponMount`, `WeaponProbeOrigin`, upper-body attachment transforms or shove/bat pose. Verify consumers before pruning meshes and animation helpers.
6. **Vegetation per-instance custom data may have other uses.** Remove camera opacity channel only after checking instance colours, material setup, tint/wind/LOD and shader variants across near/far/third LOD.
7. **FPS debug remains important.** Retain F1 dual-panel visibility, weapon/interaction input blocking, ability to recapture mouse, foliage sliders, DF2 palette safe mode, and diagnostic counters. No GUI-click-through firing.
8. **Not all 'fade', 'visibility', 'occlusion' or 'top-down' strings indicate active code.** Separate source calls, scene script resources, shader symbols, archived plans and manual editor-preview helpers. Remove active obsolete work; keep useful historic records clearly marked.

## FPS0 — Switch F5 to the working FPS scene (low risk)

**Goal:** Give the owner a single FPS entry *now*, without destabilizing the inherited world.

- **Current:** `project.godot` launches `res://WORLD/testworld.tscn` with F5; active FPS requires F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`.
- **Change:** Point `run/main_scene` to the existing `res://WORLD/FirstPerson/FirstPersonWorld.tscn`. Do not alter World, player, camera, collision or terrain at this phase.
- **Keep:** testworld, active shared top-down scene and all FPS inheritance temporarily. This is a reversible startup setting.
- **Verification:** `dotnet build war-machine.csproj` (when available); local F5 equals the existing FPS F6 experience. Confirm mouse capture/Escape/recapture, walk/run/crouch/prone/jump/lean, rifle and MG selection/fire/reload, bat/Q shove, interaction, F1, environment, village, zombies and HUD. Check no scene load/resource errors.
- **Exit gate:** Owner confirms F5 launches the correct FPS world. Do not start FPS1 automatically.

## FPS1 — Promote FPS to canonical World composition (medium–high risk)

**Goal:** `WORLD/World.tscn` becomes the FPS gameplay entry. There must not be two distinct live simulation roots or duplicate actor/environment/scenery instances.

**Proposed steps:**

1. Inspect both `.tscn` files, root node script, inherited index/paths, all FPS overrides in `FirstPersonWorld.cs`, current master resources and consumers of the scene paths. Record a baseline FPS scene tree and gameplay check.
2. Carefully transfer the *active* FPS player (`FirstPersonPlayer`), camera (`FirstPersonCamera` and `FirstPersonMaster.tres`), close-view grass config, equipment/slot binding, center-screen interaction, FPS fog and world-streaming profile into the canonical `WORLD/World.tscn` composition. Keep world actor/prop/system owners untouched.
3. **Least-risk intermediate is acceptable:** temporarily use `FirstPersonWorld` as the root script directly on canonical `World.tscn`, or fold its small override hooks into `WorldController`. Prefer the option with fewer moving pieces. Do **not** duplicate the inherited world, add a second player, or make a cyclic scene inheritance.
4. Retain compatibility only long enough to redirect explicit scene references, callbacks, test/handoff instructions and `project.godot` F5 to `res://WORLD/World.tscn`. Do not delete the old FPS scene until no references depend on it; historical source can remain archived in Git.
5. Verify `_Ready` → `InitializeWorld` → actor/camera/equipment/fog/streaming initialization order; ensure active FPS scene has no top-down occlusion manager and no overhead controls.
6. Keep `WorldController` as the existing world compositor instead of rewriting its terrain/village/zombie/vegetation logic. Do not rename major folders merely to make 'FirstPerson' disappear.

**Ownership:** `WORLD/World.tscn`, `WORLD/WorldController.cs`, `WORLD/FirstPerson/FirstPersonWorld.tscn/.cs`, `PLAYER/FirstPerson/FirstPersonPlayer.cs`, `CAMERA/FirstPerson/FirstPersonCamera.cs`, `WORLD/FirstPerson/Configuration/FirstPersonMaster.tres`, `project.godot`, and any exact files needed by actual reference scans.

**Keep:** interactive combat, slot/ammo HUD, world spawning, visuals, village structural hierarchy, terrain collision, original authored layouts, spawn selection and world streams.

**Performance:** no new world tick; reuse existing events, actor physics driver and render policy. Eye camera keeps render-frame interpolation/head-bob/lean sleep.

**Exit gate:** F5 `World.tscn` behaves like the old accepted FPS scene. Validate one player, one camera, one environment, one HUD, one equipment owner, no duplicate spawns; test entering the store, lodge/stairs, police station, shooting zombies, opening/pushing doors, menus and world building unload/reload. Explicit owner acceptance before FPS2.

## FPS2 — Remove overhead input, camera and combat adapters (medium risk)

**Goal:** No playable overhead/isometric presentation or screen-relative control path in canonical gameplay.

**Tasks:**

- Convert `CAMERA/Camera.cs` to the smallest camera contract still needed by FPS, shared world, debug, streaming and culling. Retain `Initialize`, `ViewChanged`, `SettingsChanged`, far-range notifications and rendering-offset methods **until all consumers are audited**. Remove orbit input, MMB rotation, wheel zoom, overhead pitch/follow/orthographic projection and their settings.
- Promote/keep the active FPS camera mouse-look, capture, recoil pitch, lean, eye interpolation/head bob and FOV. Keep Far clipping and `WorldRenderCulling` callbacks accurate.
- Remove overhead movement-vector caching from `PLAYER/Movement/PlayerMovement.cs` and view-only branches from player/visual scripts **without losing shared movement, physics, stance, hit probes or animator hooks**. FPS uses camera yaw for WASD/arrow movement as currently configured; do not change bindings gratuitously.
- Remove `PLAYER/Equipment/PlayerWeaponController.cs` and `TopDownWeaponRecoil.cs` **only after** the default world binds `FirstPersonWeapon`, and all HUD/interaction/weapon events are correct. Keep `WeaponSlots`, `FirearmController`, FPS recoil and actual shots.
- Keep `FirstPersonInteraction`; remove `TopDownInteraction` and cursor/faded-selection from the view contract and shared targeting as appropriate. Always keep **player reach and solid-cover** verification.
- Delete overhead camera/debug controls from `DEBUG/Overlay/DebugOverlay.cs` and obsolete settings/copy fields; retain shared F1 spawner, environment controls, FPS fog, zombie/corpse counters, foliage shader tint and global colour grading.
- Remove view-conditional horde defaults if genuinely obsolete; retain shared village/store zombie stress population as a normal optional FPS test.

**Exit gate:** No overhead camera can become current; input remains captured FPS; movement, combat, interaction, inventory, F1, fog, streaming and gameplay all work. Source no longer selects a top-down adapter, but defer complex fade removals to FPS3 if needed.

## FPS3 — Delete overhead visual fading, not physical structures (medium risk)

**Goal:** No top-down transparency/occlusion bookkeeping for buildings, roofs, walls, pine canopies or DF2 crowns. FPS sees ordinary solid geometry and uses normal rendering/fog/streaming.

### 3A. Buildings, door links and camera fade

1. Confirm `WorldController.InitializeOcclusion` is no longer used in canonical FPS. Remove `CameraOcclusion` registration, provider events and camera-only physics proxies/layer work; do not tamper with world collision/LOS layers.
2. For all authored prefabs `Building001`–`004`, `PoliceStation001`, `ConvenienceStore001`, remove `StructureVisibility` scene child/script/resource references while preserving original `.tscn` scripts, generators, settings, authored geometry, **physical** roof/floor/wall/door collision and editor-only preview.
3. Retire `StructureVisibility.cs`, `BuildingFloorVisibility.cs`, `BuildingObstructionVisibility.cs`, `CameraOccluder.cs`, `CameraOcclusion.cs`, opacity/provider/mesh-target support, `FadeAwareAim` and associated exports only after repo-wide symbol/path searches. Remove obsolete fade-only shader/material metadata and shape-to-faded-mesh links, but leave normal collision shapes and weapon obstruction.
4. **Keep/extract:** `StructureLayout.Occupied`, `structure_occupied_bounds`, relevant `StructureLayout.Level/Visual` authoring semantics, transform/bounds utilities, `StructureClearance`, editor previews, scenery reservation and building footprint capture. Refactor dependency on `CameraOcclusionMath.TransformBounds` to neutral geometry code before deleting occlusion math.
5. In `BUILDING_AUTHORING_README.md` and `BUILDING_DESIGN_GUIDE.md`, replace runtime fade instructions with FPS-oriented design/collision/haze/streaming authoring guidance; retain practical PoliceStation001 errors/lessons and manual editor inspection controls. Don't remove doorway/circulation rules.
6. Remove fade-only `StructureSettings` exports and matching `.tres` keys only after confirming which per-building settings are truly presentation-only.

### 3B. Pine, broadleaf and shaders

- Remove `ICameraOcclusionProvider` and fade-target cache/population/registration from `PinePlacement.cs` and `ForestPlacement.cs`.
- In `LeafWind.gdshader` and `CanopyFoliage.gdshader`, eliminate only overhead-opacity/shader-discard code that has no FPS purpose. Preserve wind, vertex/instance colours, shared `wm_foliage_tint`, colour-safe DF2 palette behaviour, LOD transitions, per-instance data used elsewhere and material compatibility.
- Verify near/mid/far imported stylized pines, DF2 leaf/bark colours, broadleaf silhouettes, shadows, stable chunk seeds, trunk collisions and actual camera-distance LOD. Do not accidentally turn DF2 white again or destroy low-cost MultiMesh batching.

### 3C. Fog/rendering

- Eliminate unused overhead `PlayerDistanceFog` mode and overhead-specific world updates; keep the FPS camera-distance fog path, `FirstPersonFog` settings sliders, atmospheric sky, ground haze, house/canopy haze exclusions and `WorldRenderCulling`.
- Keep normal static-scene culling, depth-based FPS fog, fog-hidden render buffer, camera far-clip updates, and debug render limits. Do not mistake terrain or vegetation LOD for top-down-specific behavior.

**Exit gate:** Walk indoors/outdoors and upstairs in FPS. Roof, upper decks, wall segments and railings remain visible and solid, bullets stop on real cover; canopy and buildings never fade merely because of camera obstruction; ground haze still fades away smoothly beneath actual roofs/canopies but remains in outdoor parking lots; trees remain green, correctly LODded and chunk-streamed. No camera-only proxy or fade callback remains active. Build + focused scene/source checks, then owner visual acceptance.

## FPS4 — Documentation, settings, tests and final reference cleanup (low–medium risk)

- Update [README.md](README.md), [AGENTS.md](AGENTS.md), [CodePreferences.md](CodePreferences.md), `WORLD/README.md`, `WORLD/FirstPerson/README.md` and affected camera/player/interaction/building/vegetation/environment/debug docs to say **FPS-only**. Clean superseded *instructions* without erasing useful historical plans. Keep known implementation-versus-acceptance states honest.
- Reconcile `project.godot` F5 scene, `World.tscn` root and path indexes, `CameraMaster`/FPS master duplication, dead `.tres` exports, scene resources, debug controls, compile references, old tests and stale text mentioning required top-down QA.
- Update interaction regression smoke to exercise FPS center ray, solid wall/floor rejection, player reach and safe input gating. Retire overhead projection/faded-ray cases only when feature is no longer supported. Do not delete all tests.
- Search code/resources/scenes/shaders for `TopDown`, `Isometric`, `CameraOcclusion`, `StructureVisibility`, `FadeAwareAim`, `ObstructionFade`, `camera_opacity`, `UseFadedCameraSelection`, `Orthographic`, and former scene paths. **Triage every match**: active dead dependency to remove, general engine concept to keep, historical documentation to label.
- The owner may separately decide later whether to delete `WORLD/testworld.tscn` or `PLAYER/LegacyFirstPerson/`. They are not automatically removed by this plan.
- Finally run focused build/smoke and one FPS gameplay acceptance walk. Verify that opening the Godot project finds the intended main scene and no missing C# class/script/resource. Record any unverified visuals/performance.

**Final acceptance:** FPS is the only supported gameplay view, F5 launches `WORLD/World.tscn`, no top-down runtime features or required compatibility shims remain, and all preserved gameplay/scene systems still function.

## Validation checklist for **every** approved implementation phase

**Quick source/build check (when tools available):**

- Change only the phase's paths; inspect `git status` and `git diff --check`; inspect newly edited C# loop syntax and resource paths.
- `dotnet build war-machine.csproj` if Godot/.NET SDK available. If not, explicitly report **compilation not verified**. One baseline check; no repeated broad test suites unless requested.
- Confirm no unhandled nulls, stale nodes, duplicated runtime owner or lingering event subscriptions on scene exit.
- For scenes/shaders touched, check external resources/UID paths and whether a local Godot import/visual check is still required.

**Owner's F5/F6 acceptance (as relevant to phase):**

- Spawn in village with correct eye/capsule, mouse capture, movement, sprint/stamina, jump, crouch, prone, leaning and head bob; no involuntary top-down camera.
- Use rifle, machine gun, slot-3 bat, reload, shove, accepted-shot recoil and ammo UI. Aim and shoot zombies; normal walls and doors still stop bullets.
- Hold E + LMB to interact with test target if enabled; solid cover and maximum reach always apply; no accidental firing behind F1 UI.
- Enter convenience store, lodge, field office and police station; walk stairs, upper floor, doors, ceiling and railings; inspect real wall/floor/roof collision.
- Confirm F1/debug, F7 sight cones, placeable catalogue, colour grading and FPS fog/haze remain usable; F1 close/escape/recapture work.
- Move across sections, approach/leave village and forest and return; check static building load/unload, vegetation chunk seed stability, tree LOD/wind/shadows, no missing geometry, and expected zombie populations.
- Check indoor haze exclusions with soft boundaries; check DF2 broadleaf materials are not white. Visual/FPS/performance acceptance belongs to the owner.

**Performance and scope:** Preserve activation/shutdown/low idle cost and existing streaming. No new scene tree scanning, persistent camera obstruction timer or expensive per-instance fade loop. If removing code yields a measurable change, record conditions; do not invent FPS gains.

## Codex handoff protocol

At the beginning of **each** phase:
1. Refresh `AGENTS.md`, `CodePreferences.md`, root `README.md`, **this plan**, owner feature READMEs and actual current branch code/scenes. Verify `implement-td0-td1`.
2. State **which one phase** was requested; confirm live dependencies before deletion, and preserve the source-backed FPS baseline.
3. Implement only that phase. Prefer **one logical commit**, push to `implement-td0-td1` only when authorized. No force resets, no unrelated refactors.
4. Update this phase table/status and any enduring owner docs affected by the code; distinguish implementation, build and owner acceptance.
5. Handoff: exact changed files, commit SHA, checks actually run, checks still needed in Godot, relevant risks and suggested next phase. **Do not proceed to the next phase without an explicit new request.**

**Suggested first Codex instruction:** “Read `AGENTS.md`, `CodePreferences.md`, `README.md` and `FPS_ONLY_CONVERSION_IMPLEMENTATION_PLAN.md` on `implement-td0-td1`. Implement **FPS0 only** and report the F5 validation handoff. Do not start FPS1.”

**Important:** This is a migration roadmap based on inspected source, not a guaranteed exhaustive deletion manifest. Recheck all references against the **current** branch before editing; any newly introduced dependencies must be accounted for.