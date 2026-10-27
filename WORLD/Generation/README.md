# Active gameplay generation

Run `BakeGameplayTerrain.tscn` only for a fresh active world with no saved terrain. `GameplayTerrainBake.cs` reads WORLD/Configuration/WorldMaster.tres, writes WORLD/Terrain/Gameplay, and refuses to overwrite. `GameplayTerrainStorage.cs` first reuses an existing user-local top-down bake by copying its native regions. WORLD/World.tscn loads saved terrain; it does not generate on launch.

The notes below describe the **parked first-person wilderness generation**, whose config is now LegacyWorldMaster.tres.

# Explicit terrain authoring

`TerrainBake.cs` runs only through `BakeTerrain.tscn`. It creates deterministic FastNoiseLite relief plus a sinuous valley, imports a 1024m float heightmap, places seeded grass on gentle slopes, and saves native Terrain3D data/instances. No runtime dependency on this scene exists.

Do **not** run this to play: use `WORLD/testworld.tscn` instead. The first world is already baked.

For a new landscape: back up authored data, select a **new empty** `DataDirectory` in `WORLD/Configuration/LegacyWorldMaster.tres`, adjust seed/elevation/density, build, then run:

```sh
dotnet build
godot --headless --path . res://WORLD/Generation/BakeTerrain.tscn
```

Use your Godot .NET executable path in place of `godot`. Existing `.res` data causes exit 1 without changes. WE1 supports Size=1024 only; extending world size is later scope. LegacyWorldMaster's directory is authoritative at runtime; update testworld's Terrain3D `data_directory` too when changing it so the editor previews the same data. Settings affect new bakes, never automatically rewrite saved land. A new directory requires its own committed generated files.

This scene is an offline authoring utility, not another playable world. Generation does bounded one-shot work and then exits. No streaming or general generation framework is introduced.

## Pine landscape revision

LegacyWorldMaster selects a separately baked 2048m square landscape in `Terrain/PineLandscape` (four 1024m regions). Original `Terrain/Data` remains intact. ValleyWidth, MeadowBlend and MeadowRelief control the broad gently rolling floor; Elevation controls surrounding mountains. Baking still refuses any existing `.res` data: choose a new empty directory for revisions. GPU grass handles placement, so new bakes omit obsolete saved crossed-card instances.
