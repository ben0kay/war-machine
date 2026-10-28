# WORLD

**Current village layout:** [VILLAGE_IMPLEMENTATION_PLAN.md](VILLAGE_IMPLEMENTATION_PLAN.md) — VG0–VG3 implemented, awaiting local validation. ConvenienceStoreSite001 is the centre; terrain is graded across measured footprints, four buildings sit farther north, robot patrols the western perimeter, and 9 slow + 1 runner replace the default horde. Earlier showcase locations below are historical. See current handoff for coordinates/checks.



**Active game scene: `WORLD/World.tscn` — open it and press F6.** `WorldController.cs` composes the player, camera, sky/weather, robot, rifle and zombies. Player/camera scenes and behaviour now live in PLAYER/ and CAMERA/; entities and equipment retain their own folders.

## Ownership

- `WorldSettings.cs` and `Configuration/WorldMaster.tres`: active terrain generation/spawn/zombie placement defaults.
- `Encounters/RobotPlacement.cs`: world-owned robot route/ground-placement adapter; robot logic stays in ENTITIES.
- `Generation/GameplayTerrainBake.cs` and `BakeGameplayTerrain.tscn`: explicit offline authoring for active terrain. `GameplayTerrainStorage.cs` copies an existing local prototype bake to `Terrain/Gameplay/` once, retaining originals.
- `Terrain/Gameplay/`: active saved terrain, separate from legacy wilderness data. Runtime scene assigns this directory after migration; no terrain is regenerated on play.
- `Archive/`: historical TD notes and the cleanup handoff. No active scripts remain under TopDownWorldTest.
- `testworld.tscn`, `TestWorld.cs`, `WorldConfig.cs`, `Configuration/LegacyWorldMaster.tres`, main-world generation and Content/: parked first-person wilderness. Its saved terrain/weather/art are retained.

F5 still starts the original testworld.tscn; startup selection was not changed. If an existing prototype bake is available at the old TopDownWorldTest/Terrain/Data path, opening World.tscn reuses it through a non-destructive copy. For a fresh checkout without any terrain, F6 `Generation/BakeGameplayTerrain.tscn` once, then F6 World.tscn. The bake refuses to overwrite existing terrain.

Active defaults are split between WORLD/Configuration/WorldMaster, PLAYER/Configuration/PlayerMaster, CAMERA/Configuration/CameraMaster and ENVIRONMENT/Configuration/GameplayEnvironmentMaster. The tuning panel edits runtime copies and Copy settings includes camera/environment values. No intended gameplay or update-frequency changes. Read `RESTRUCTURE.md` for validation status.

## Grass

The existing VEGETATION/Grass/TerrainGrass renderer is now initialized around the active player using the shared VegetationMaster (simplified overhead defaults). The tuning panel exposes enabled/density/height/wind and copies those values. Coverage adapts to projection/zoom within a bounded radius/budget. No rebake required; see VEGETATION/README.md for defaults and pending local checks.

## Pine integration

Pine trees now use VEGETATION/Trees/Pine: lighter shared meshes/settings, event-driven player-centred batches and tuning controls. No rebake required; see the pine README for limits and pending local checks.

## Camera obstruction fading

WorldController initializes CAMERA/Occlusion/CameraOcclusion after the player/camera/pines are ready. Scenery-only fading is enabled at 50%; pine canopies support individual fades and Building001 is opted in for future placement. Entities remain opaque. No terrain rebake required.

## Terrain appearance startup correction

After selecting the saved gameplay data directory, WorldController explicitly binds runtime copies of the terrain asset list and material, in that order, then disables checker/colormap debug views. It checks that both texture layers loaded and displays an error if they did not. This avoids retaining an empty-terrain initialization debug state and leaves source masters/saved terrain unchanged. The Output message reports the layer count. No rebake required. Source/reference/whitespace checks only; local Godot texture rendering remains unverified.

## Building placement and trunk collision

Building001 now spawns on sampled terrain at WorldSettings.Building001Offset=(4,-32), about 32m north of player spawn. Its default footprint is roughly 30m from the robot patrol route at the nearest edge, with no terrain edits. Pines are excluded from the footprint/loading apron/approach and initial zombie spawn points. Existing prefab wall/roof collision and camera fade are reused.

Validation for this follow-up: source/resource paths, collision masks, shared thickness sampling, building-versus-route spacing and whitespace reviewed. Godot/.NET unavailable: local build, tree collision, depot grounding/doorway and fade checks remain pending. Pull/build and F6 WORLD/World.tscn; walk north to the depot, bump trunks with player/zombies, check rifle obstruction and pine OFF/rebuild. No rebake required.

## Alternate first-person test

DEBUG/TEST/FirstPerson/FirstPersonTest.tscn inherits World.tscn. Small virtual composition hooks let it replace presentation/input adapters and supply its own grass profile without copying world/entity logic. The normal scene keeps its existing defaults. See the test README for controls and pending local validation.

## Shared scenery layout (top-down and FPS)

Both WORLD/World.tscn and DEBUG/TEST/FirstPerson/FirstPersonTest.tscn now use the same four prefabs, placed once by WorldController:

| Prefab | Offset from player spawn | Direction |
| --- | --- | --- |
| Building001 maintenance depot | (4, -32) | North |
| Building002 ranger lodge | (-34, -42) | Northwest |
| Building003 fuel station | (42, 32) | Southeast |
| Car001, driver door already open | (24, 42) | Beside the fuel station |

Offsets live in Configuration/WorldMaster.tres. Each root samples saved terrain height; no terrain editing/rebake. Building entrances face +Z (south). All are clear of the default robot patrol west/southwest and initial player/zombie positions. One startup-only subtree bounds pass reserves pine clearings around all visible prefab geometry, plus walking approaches. This includes the lodge veranda, station forecourt/sign and swung car door, and respects prefab dimension changes. Prefab collision and overhead camera fading are reused; FPS continues to omit the fading manager.

Validation: source/resource references, default layout and whitespace checks. Godot/.NET unavailable; local compilation, grounding, entrances, collision and both camera views remain unverified. No added processing loops; placement/bounds work ends after startup, existing entity/vegetation streaming remains shared.

## Grass scenery exclusion

Shared WorldController now places scenery before initializing grass and supplies tighter world-XZ rectangles from actual generated prefab bounds, plus a 0.3m edge buffer. All three building interiors, porch/apron/forecourt areas and the car envelope reject grass in both top-down and inherited FPS views. Tree clearings retain their larger approach margins. Rectangular envelopes are conservative around irregular outlines; terrain painting/saves are unchanged and no rebake is needed.

TerrainGrass.SetExclusionAreas caches up to 64 rectangles and uploads the mask on initialization, explicit layout changes or grass rebuilds. GrassPlacement rejects randomized roots inside those rectangles before terrain sampling. Streamed tiles and quality/OFF/ON rebuilds reuse the cached mask; an explicit layout update restarts existing tiles once. No per-blade physics queries, CPU polling or new frame callbacks. GPU particle slots remain budgeted even when rejected.

Validation: source review of mask packing/comparisons, initialization ordering, rebuild retention and whitespace checks. Godot/.NET unavailable here; shader compilation and in-game checks remain pending. Build then F6 either view, check station pavement/interiors and grass outside, toggle/rebuild grass in top-down and walk away/back to stream tiles. Changes to scenery transforms after startup need an explicit mask refresh; current props are stationary.

## Scene roots — SO0/SO1

World.tscn now organizes authored nodes as TerrainRoot/Terrain3D, WorldActorsRoot/PersistentEntitiesRoot/{Player,RobotPatrol}, root Camera, and DebugUIRoot/Controls/Status. The inherited FPS player override and both debug UI paths were updated. Original transforms, resources and robot subtree data were preserved; no terrain rebake needed.

SO0 audit complete; SO1 implemented, awaiting local Godot validation. SO2 now routes runtime buildings/car, zombies, vegetation and environment into the stable roots described below. No new managers, idle callbacks or settings. Source scene/property/path/index and whitespace checks passed; build/F6 both views and visual checks remain local. See WORLD_SCENE_RESTRUCTURE_PLAN.md for the audit map and handoff.

## SO2 runtime roots and top-down edge fog

Buildings spawn beneath WorldActorsRoot/PersistentPropsRoot/Structures; Car001 beneath PersistentPropsRoot/Objects/Cars; zombies beneath PersistentEntitiesRoot/Zombies. SystemsRoot/VegetationRoot owns Grass/Pines; SystemsRoot/EnvironmentRoot owns sky/controller/weather. Authored containers are reused, with no duplicate roots. Both views inherit this organization; Camera remains root child index 2. Props still spawn at runtime, using the same placement/grounding/exclusion rules.

Top-down now adds player-centred visibility fog: clear within 24m, progressively opaque by 48m, with soft irregular edges. Tune enable/radii/opacity/variation/color in the live panel or GameplayEnvironmentMaster; Copy settings includes these values. FPS omits this effect. Visual fog does not stop hidden zombies simulating or change detection/spawning. See ENVIRONMENT/Fog/README.md for depth limitations, lifecycle and local validation. Enabled cost is one fullscreen depth overlay, no C# frame loops; disabled/zero opacity removes draw and movement subscription.

Source/scene/path/uniform/whitespace checks passed; C# build, shader compilation, both F6 views, night tint, tree/prop masks and Remote tree remain locally unverified. No rebake required.

## Rocks and raised cliff path

The shared world now loads the nine existing rock variants with active static collision beneath PersistentPropsRoot/Rocks. Building/tree/rock exclusion areas preserve interiors and approaches; rock clearance includes boulder size. The old mesh ramp was replaced with an actual Terrain3D mountain south of spawn. A broad curved approach begins ~20m south and climbs to a softly domed summit ~12m above the base, with steep irregular cliff edges. Native terrain textures/collision handle it; only the walking route is cleared of vegetation, while the summit/shoulders use normal slope-aware scatter. Saved files are not overwritten and no rebake is needed. WorldMaster owns cliff dimensions/offset; Rocks/Configuration/GameplayRockMaster owns active scatter. See Content/Rocks/README.md for implementation, collision limits and pending local checks.

Zombie hill-foot fall-through follow-up: full-map collision/masks are configured correctly in source, but the reported runtime failure is unconfirmed. World placement supplies optional terrain height for the zombie's falling-only, existing-timer diagnostic/recovery. It only lifts an already underground actor onto ray-confirmed walkable ground with clear capsule space; ordinary cliff falls remain. See Zombies README for checks/limits. No rebake.

The terrain preview is a bounded startup-only heightmap sculpt before actor/foliage initialization; no new per-frame work. See Generation/README.md. Actual Terrain3D visuals, native collision and walkable joins still need local F6 validation.

Zombie PER0/PER1 now use frontal eyesight (160°), range and head/torso cover rays; crouch samples follow actual capsule height. F7 toggles the faint red potential-vision cones (debug only, not occlusion-clipped). Tune ENTITIES/TYPES/Zombies/Configuration/ZombieMaster.tres. Glass still blocks pending PER3; memory/hearing remain future. Both World and FPS share these checks; robot unchanged. Awaiting local Godot build/runtime checks.

Top-down buildings: Building002 now detects local player floor/stairs and composes section fades; upper content restores at the landing, roof stays faded indoors, ground walls restore upstairs. Building003 fades only independent shop-roof/canopy sections, preserving pumps/furniture. Tune lodge visibility in Building002Master and camera obstruction in CameraMaster. Physics/LOS and FPS visibility unchanged; local build/visual validation pending.

Shared test equipment/population: **1 = Rifle_001, 2 = MachineGun_001**; rifle starts selected, switching cancels held fire and reuses cached gun scenes. Press/click again to shoot after switching. Both scenes now have 12 existing slow zombies plus 3 Zombie_002 runners, with independent WorldMaster.RunnerSpawnOffsets at (18,22), (-15,30), (32,-18) from spawn. Runners wander slowly and sprint at their authored 8.3m/s only when sight confirms the player; no attacks/hearing added. F7 shows vision; tree/rock spawn clearings include runners. Local build/weapon placement/shooting/runner checks pending. Existing FPS fog profile and other recent commits preserved.

## Top-down horde stress test — 20 additional zombies

In `WORLD/World.tscn` only, the world now generates one deterministic, loosely packed cluster of **20 additional zombies** southwest of the player spawn: **14 Zombie 001 Shamblers + 6 Zombie 002 Runners** by default. The previous population of 12 Shamblers and 3 scattered Runners remains, for a **top-down total of 35**. `FirstPersonWorld` explicitly opts out; its inherited test remains at 15.

Tune `WORLD/Configuration/WorldMaster.tres` under `Horde preview (top-down only)`:
- `HordeCount = 20`: extra zombies (0 disables; capped at 60 for a deliberate preview)
- `HordeRunnerCount = 6`: how many of the extra count are Runners (clamped to total)
- `HordeCenterOffset = Vector2(-38, -16)`: centre relative to the player start, away from structures, hill and robot patrol
- `HordeSpacing = 2.5`: metres between starting actors; small deterministic jitter avoids identical rows

The startup layout is calculated once and reused for pine and rock collision exclusion rectangles, native terrain-height placement, seeded zombie initialization and F7 vision-debug registration. This **does not** change AI, patrol/wander/chase ranges, weapon damage, zombie attack behaviour (none), Terrain3D bake, world saving or zombie spawning during play. The horde is initially beyond normal 18m visual range: approach from spawn to wake a chase. Its motion is not a coordinated group/flocking AI; it is a clustered population of independently wandering/pursuing zombies, deliberately useful for observing crowd behaviour and performance.

**Minimal local checks:** compile C#; F6 `WORLD/World.tscn`; approach the southwest group, count about 20 new actors, see both models, run/chase and shoot; toggle F7, verify the scatter systems didn't put trunks/boulders inside the initial formation. Confirm `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` still loads its original 15 population. Set `HordeCount = 0` and restart to disable the preview group. Briefly watch CPU/FPS and the existing terrain grounding issue with the larger population. Local build and visual testing are pending.

## Gameplay HUD — visual hotbar

The shared `WORLD/World.tscn` instances `UI/GameplayHUD/Hotbar/Hotbar.tscn` as `GameplayHUD`. Nine fixed, empty numbered slots are centered near the bottom; Slot1's accent border is an illustrative selection only. This is static UI without inventory, quick-key actions, weapon-selection or item binding. It appears in the inherited FPS debug test as well. See `UI/GameplayHUD/Hotbar/README.md` for future integration and validation notes.
Structure visibility now comes from each prefab's StructureVisibility component and metadata, rather than named world cases. See Content/Structures/BUILDING_AUTHORING_README.md before adding authored/procedural buildings. Existing three masters inherit the same visibility settings, and later instances register under Structures automatically. Top-down-only context; local build/visual validation pending.

## Shared environment prefab

EnvironmentRoot is now an instance of ENVIRONMENT/Environment.tscn. WorldController binds the ready terrain/player/camera rather than independently constructing sky, weather and haze. FirstPersonTest inherits that same instance, including haze. Shared defaults live on the prefab's GameplayEnvironmentMaster reference; edit those once for both views. See ../ENVIRONMENT/SHARED_ENVIRONMENT_README.md. Gameplay/terrain/physics ownership is unchanged; local build/visual checks pending.

## Building004 — second multi-storey comparison

A new [Building004 field office](Content/Structures/Building004/README.md) is placed once at `WorldMaster.Building004Offset = (-34,-70)`, roughly 26m beyond the Building002 lodge (north-west of spawn). Both active top-down and inherited FPS use shared WorldController placement and generated-bounds clearings. The concrete/brick structure differs from the timber lodge with a flat roof, rooftop vent, upper walk-out balcony and crosswise 21-step stair. A real four-span collision deck has an open stairwell. Metadata and a scene-owned StructureVisibility child use the common per-floor, roof and whole-building camera fades automatically; no new building-specific camera rules, terrain bake, player/AI changes or process loops. Set Building004Offset in WorldMaster to adjust location. Test F6 both views, entrance, stairs, upstairs balcony and original structure fades. Local Godot build and visual checks remain outstanding.

## VG0–VG3 village implementation — current

**Implemented; Awaiting local compilation, traversal and visual acceptance.** Main scene remains WORLD/World.tscn (F6). The convenience-store site is now authored beneath PersistentPropsRoot/Structures; WorldMaster/VillageOriginXZ anchors its placement independently of SpawnXZ. The nested store registers through existing StructureVisibility; lot, lamps and cars are not added to its building target. OV1 and PER2 are preserved.

Default village origin (512,512); player arrives at (512,550). Building001 is (-35,-90), Building002 (-35,-145), fuel station Building003 (80,-75), and Building004 (45,-190), relative to village origin. Existing standalone showcase car is omitted in village mode; three site cars remain. Dirt connections run out of the driveway, east to X=35 and north, with branches ending in front of measured building envelopes. The outer-west robot loop is centred (-115,-50), local corners +/-20 X and +/-45 Z; its complete edge corridor is reserved from trunks/rocks and checked against expanded structure/site envelopes. Three low hills frame the west/east/far-north scenery; the old cliff test sculpt is bypassed in village mode.

VillageLayout measures generated transformed mesh bounds once, reserves the full site plus rear access/driveway, surveys every platform/road on a <=2m grid including edges, picks the site's median height and enforces WorldMaster.VillageMaxCutFill (8m default). VillageTerrain duplicates native height images, levels platforms/roads and blends their shoulders (18m default); hills are masked out of protected platforms/roads and the robot corridor. Platform interpolation edges receive extra vertex padding and a full post-grade grid check (3.5cm tolerance). No saved region is overwritten or terrain rebaked. Runtime work is bounded to a 480x500m area around the village; moving plots outside it fails post-validation rather than silently leaving them unsupported. Road width 6m, hill scale 1, and village wander radius 8m are grouped in WorldMaster. Layout failures leave actors dormant with an explicit error.

Two shallow collidable ramps join the 12cm driveway/rear slab edges to graded ground. Native terrain collision is rebuilt after grading; world startup waits two physics ticks before clearance/spawn queries. The world startup callback then stops. Arrival support and player capsule clearance are checked before activation. Nine slow + one runner are proposed at village-relative markers; each gets actual-surface support/slope, capsule, player-buffer (20m), spacing and patrol checks, with invalid markers skipped/logged. Their runtime wander radius is 8m; PER2 subsequently relocates their anchor at their actual stopping point. Default HordeCount/HordeRunnerCount are 0; horde settings remain an optional debug opt-in. No interior default spawns, repeated respawning, actor AI rewrite or navigation system.

Performance: bounds/surveys/sculpt, road/ramp construction and spawn tests happen once. Grass/pine/rock exclusions are supplied before placement. Robot edge/body and foot-height checks run at initialization; actual patrol uses existing lifecycle. No village polling manager, new permanent world frame callback, per-frame terrain edits or queries. Native terrain/render/active lamps and existing actor work still cost resources; no measured FPS improvement is claimed. Four lamps retain their existing steady-on preview lighting; PL2 flicker/time-of-day relevance and store-glow work remain separate.

Checks here: source/resource/path/for-foreach and lifecycle review; World scene load_steps/resource paths, one site instance and inherited FPS root-camera index; numeric conservative patrol separation, initial player/zombie buffer and grading-weight endpoints; git diff --check. Attempted `dotnet build war-machine.csproj`: command unavailable (exit 127). No Godot executable found. **Compilation, native terrain APIs, generated-art footprint survey, collision/ramp traversal, rendering and patrol cycles are not verified here.** Numeric checks use conservative envelope approximations and do not substitute for runtime measured art.

Local checks: pull, `dotnet build war-machine.csproj`, open WORLD/World.tscn and F6. Existing terrain is reused; no rebake needed. Inspect Output for survey min/max/target, no cut/fill/platform/road/patrol errors and expected spawn skips. Walk driveway, parking perimeter, central entrance, store floor and rear exit; inspect every slab edge for floating/clipping and no foliage/rocks on hard surfaces. Follow roads north to all four buildings and check entries/stairs/fades. Stand in front/behind store: only its art should fade, not lot/lamps. Observe two western robot cycles and its feet, no structure/trunk/rock intersections; inspect night/day and mist composition. Check local wanderers, PER2 searching/anchors, combat, player buffer and default horde OFF. Briefly F6 DEBUG/TEST/FirstPerson/FirstPersonTest.tscn for inherited scene/collision/equipment regressions. Check startup settles with no world processing and saved terrain files remain unmodified. If platform tolerance or placement fails, supply exact Output before tuning authoring limits. Final visual/performance acceptance belongs to the owner.

Current convenience-store site is now 56x36m with 14 bays/seven parked cars. Full-footprint village reservations/terrain adapt to generated bounds, road/rear ramp follow the updated site markers, and rear zombie candidates have moved clear. See Content/Sites/ConvenienceStoreSite001/README.md for exact transforms and pending local checks.
