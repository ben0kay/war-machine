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
