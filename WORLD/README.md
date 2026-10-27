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
