# War Machine — World scene root hierarchy restructuring plan

**Status: Planned.** Updated after the owner's hierarchy/procedural-content discussion. Target branch: `implement-td0-td1`. This update changes documentation only; SO0/SO1 have not been implemented.

## Goal and agreed direction

Use the organizational roots in `ben0kay/isometric-fantasy-2d/SCENES/World/World.tscn` as a reference, adapted to 3D. Keep one actual World root with lightweight child containers. A container is a stable destination for content, whether a scene is authored in the editor or instantiated procedurally at runtime.

The priority is predictable debugging: a generated building should appear beneath the props/structures destination in Godot's **Remote** tree, rather than being mixed into the world root. Runtime instances do not need to become manually authored nodes to achieve this. Local shows saved scene nodes; Remote shows the running hierarchy.

Organization is separate from streaming and performance. Container nodes do not reduce rendering/physics cost or automatically unload anything. As the world grows, limit loaded objects through future chunk streaming, keep inactive behavior dormant and retain batched MultiMeshes/GPU grass for dense vegetation. No per-tree scene or callback is introduced by this work.

## Recommended hierarchy

| Destination | Responsibility |
| --- | --- |
| `TerrainRoot/Terrain3D` | Native terrain and its existing resources/collision |
| `WorldActorsRoot/PersistentEntitiesRoot` | Player, complete RobotPatrol subtree and current test zombies |
| `WorldActorsRoot/PersistentPropsRoot/Structures` | Current building instances, initially still spawned at runtime |
| `WorldActorsRoot/PersistentPropsRoot/Objects/Cars` | Current car instance, initially still spawned at runtime |
| `SystemsRoot/VegetationRoot` | Current grass/pine placement owners and their existing bounded batches |
| `SystemsRoot/EnvironmentRoot` | Sky, sky controller and weather |
| `Camera` | Existing camera, left directly under World |
| `DebugUIRoot/Controls` | Status, overhead tuning panel or FPS crosshair |

Create only the containers required by the current implementation stage. PersistentPropsRoot and system subcontainers can be added when SO2 actually populates them. Defer empty GameplayUIRoot, PreviewSceneryRoot and ChunkActorsRoot until their corresponding content exists.

Future streamed props/entities should live under `WorldActorsRoot/ChunkActorsRoot/<chunk>/<category>`. Future terrain chunks remain under TerrainRoot. These are ownership recommendations, not authorization to implement streaming now. Current test zombies are kept loaded for the session; their placement under PersistentEntitiesRoot does **not** claim save persistence. Long-term save identity and runtime parent paths are separate concerns.

## Confirmed current behavior

- WORLD/World.tscn authors Terrain3D, Player, Camera, Controls/Status and the complete RobotPatrol subtree directly under World.
- WorldController creates sky, Environment controller, Weather, Grass, Pines, Structures, Objects and Zombies at runtime. The Structures/Objects groups contain three buildings and one car.
- All three buildings and Car001 are already placed in both top-down and inherited FPS views. WorldMaster offsets, terrain grounding, collisions and separate grass/tree exclusion masks must be preserved.
- DEBUG/TEST/FirstPerson/FirstPersonTest.tscn inherits World.tscn and overrides Player and Camera scripts by node path. FirstPersonWorld separately looks up Controls for its crosshair.
- Camera/player/vegetation/enemies receive explicit references. Their internal node paths should not change just because their owner is reparented.
- The fantasy 2D reference has TerrainRoot, SystemsRoot, DebugUIRoot, WorldActorsRoot (PersistentEntitiesRoot, PersistentPropsRoot, ChunkActorsRoot, PreviewSceneryRoot) and GameplayUIRoot. Its YSort/2D metadata is not applicable here.

## Work phases

| Phase | Goal and scope | Suggested assistant | Status |
| --- | --- | --- | --- |
| SO0 | Audit node paths, runtime parenting, inherited FPS overrides and cleanup; record the exact reparent map before editing scenes. | Codex/ChatGPT Work: cross-file dependency review | Planned |
| SO1 | Add the essential authored roots; move Terrain3D, Player, complete RobotPatrol and debug UI; update all affected paths in both views. | Codex/ChatGPT Work: scene and code integration | Planned |
| SO2 | Route existing runtime entities, scenery, vegetation and environment into stable destinations. Keep procedural spawning and prevent duplicate containers/objects. | Codex/ChatGPT Work: initialization/lifecycle integration | Planned |
| SO3 | Optional editor-authored showcase placement, only if later requested. Keep it independent of procedural placement. | Codex/ChatGPT Work; Regular Chat can review layout | Planned (optional) |
| SO4 | Brief local build/F6 and Local/Remote inspection in both views; record acceptance and limitations. Documentation/source checks also accompany each earlier phase. | Copilot for local diagnostics; owner for visuals | Planned |

## SO0 — Audit and safe reparent map

**Objective:** establish the actual current dependencies before moving anything. Read root README, CodePreferences, WORLD README, this plan and the actual scenes/controllers. The fantasy reference has been inspected for this plan update; inspect again only if its relevant structure changes.

**Files:** WORLD/World.tscn, WorldController.cs, Encounters/RobotPlacement.cs; DEBUG/TEST/FirstPerson/FirstPersonTest.tscn and FirstPersonWorld.cs. Check camera/player/vegetation consumers for ancestor/sibling lookups, and all resource/NodePath references involving moved nodes.

**Starting map (verify against current source):**

| Current path from World | SO1 destination |
| --- | --- |
| `Terrain3D` | `TerrainRoot/Terrain3D` |
| `Player` | `WorldActorsRoot/PersistentEntitiesRoot/Player` |
| `RobotPatrol` and every descendant | `WorldActorsRoot/PersistentEntitiesRoot/RobotPatrol` and unchanged relative descendants |
| `Controls/Status` | `DebugUIRoot/Controls/Status` |
| `Camera` | `Camera` (unchanged) |

Inventory runtime AddChild sites separately for SO2: sky/controller, weather, grass/pines, Structures/Objects and Zombies. Preserve player-owned equipment, camera-owned occlusion and prefab-owned CameraOccluder nodes; those already have sensible local ownership.

**Validation/pass:** record all consumers of the old paths, including FPS inherited Player override, inherited sibling indices and FPS crosshair Controls lookup. Confirm Robot/RouteMarkers paths remain relative within RobotPatrol. No gameplay/config/resource changes in the audit. No new processing cost.

## SO1 — Essential authored roots and static node moves

**Dependencies:** completed SO0 map. **Scope:** the existing authored nodes and their consumers only. Add TerrainRoot, WorldActorsRoot/PersistentEntitiesRoot and DebugUIRoot. SystemsRoot may be established here, but dynamic parenting is SO2. Omit future-only empty containers.

Use identity transforms for new 3D containers; keep the complete robot subtree intact. Preserve resource references, transforms, visibility, terrain settings, camera registration, master configurations and setup order. Update WorldController terrain/player/robot/status/panel lookups and FirstPersonWorld crosshair lookup. Update the FPS inherited Player override to its new parent path and verify Camera's sibling index after the root layout changes. Prefer one small shared path constant/property for reused debug UI access if helpful; no general hierarchy manager or repeated scene searches.

Runtime-created scenery and systems may temporarily remain at World until SO2. Document that staged state explicitly; do not imply SO1 alone completes runtime organization. Preserve all four existing props and grass/tree exclusions in both views. No rebake, gameplay changes, new managers or streaming implementation.

**Performance:** normal Node/Node3D containers only. Existing one-shot startup lookups stay one-shot; no new process callbacks, timers or polling. Existing activation/idle/shutdown remains unchanged.

**Validation/pass:** source/resource/NodePath review and whitespace checks; build/F6 both views if Godot .NET is available. Otherwise explicitly defer runtime checks. Confirm unchanged robot route/grounding, player/camera controls, debug UI and prop/exclusion behavior. Update WORLD and FPS READMEs and mark implemented versus awaiting validation honestly.

## SO2 — Runtime destinations, without changing procedural placement

Reuse authored containers rather than creating duplicate roots. Route existing zombies into PersistentEntitiesRoot/Zombies; scenery into PersistentPropsRoot/Structures and Objects/Cars; grass/pines into VegetationRoot; sky/controller/weather into EnvironmentRoot. Update the tuning panel's Environment reference and subscriptions/cleanup as needed. Keep world transforms and initialization order stable, including placing props and supplying grass exclusions before grass initialization. Containers own lifetime; no runtime reparent polling.

Keep the three building and car PackedScene assets independent and runtime-instanced by the current placement code. This already supports a future procedural layout. Do not convert thousands of foliage instances into nodes, change save behavior or add chunk generation. Pass criteria: predictable Remote tree, exactly one instance per current prop, unchanged gameplay and exclusion masks. Brief build/F6 when available; no broad tests.

## SO3 — Optional editor placement, not a dependency

This phase is **not required** for root organization or future procedural generation. Implement only if the owner wants selected showcase props visible/manipulable in the Local editor tree. Preserve all three buildings and the car unless explicitly told otherwise; the earlier instruction not to place Building002/003 is obsolete.

If authorized later, give each prop exactly one placement owner: authored instances must not also be runtime-spawned. Preserve terrain grounding, authored transform intent, reusable prefab code and grass/tree exclusions. Procedural areas can continue to use runtime instances. No automatic conversion of all generated objects into saved scene nodes.

## SO4 — Local acceptance and handoff

Build the C# project and F6 WORLD/World.tscn and DEBUG/TEST/FirstPerson/FirstPersonTest.tscn. Inspect Local/Remote roots and verify player movement/crouch/aim, robot patrol/scanning, zombies, terrain textures/collision, weather, pines, four props and clear grass on paved/interior areas. Check world unload/reload for duplicate nodes or subscription errors. Keep checks proportional per CodePreferences; no profiling claims without measurements.

One commit per authorized job, preserving unrelated branch updates. Record the eventual implementation commit SHA and actual checks in the handoff; update phase status after each authorized task. No implementation SHA exists for SO0/SO1 yet.

## Recommendations before implementation

1. Implement SO0/SO1 together, then SO2 as a separate authorized task. This separates path/reparent changes from runtime ownership changes.
2. Keep SO3 optional; runtime instances under clear roots already satisfy the owner's debugging goal.
3. Keep stable roots at identity transforms. Move individual content, not organizational containers, when laying out the world.
4. Defer streaming until required by world size. Root paths provide organization, not a substitute for a loaded-object budget.
5. Preserve both views through the same shared world; FPS-specific camera/input/aim code remains under DEBUG/TEST.
