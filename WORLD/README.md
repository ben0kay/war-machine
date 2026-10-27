# WORLD

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
