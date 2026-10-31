# Dirt patch configuration (DTP1)

`DirtPatchMaster.tres` configures an **explicit, one-shot** native Terrain3D soil bake into `WORLD/Terrain/GameplayDirtPreviewV3/`. It never writes the source `Gameplay/` regions.

## Current authored defaults (map-wide)

- `SingleSpawnPreviewPatch=false` enables deterministic dirt coverage across the **1000 × 1000m** usable area (X/Z approximately 12..1012) within the native 1024m terrain.
- `ClusterCount=200`, 2–5 overlapping uneven blotches per cluster, plus 12 short winding soil trails; the validated starter patch at world X492/Z542 is retained.
- `Seed=73421`, `AreaHalfExtent=500`, feather 1.7m, warp 2.2m and strength 0.92 control distribution and appearance.
- `ShowNearSpawnPatches=false` leaves out extra old diagnostic witness shapes.
- `UsePreviewInGameplay=true` uses the V3 copy when the source-hash manifest and current bake version are valid; set false to view original grass/rock terrain.

## Workflow and folder ownership

Pull/build, run `WORLD/Terrain/BakeDirtPreview.tscn` with **F6 once** to regenerate the full map. The baker reuses the same V3 output directory, verifies persisted control-map bytes, safely replaces its own prior V3 and then removes **only** recognized obsolete V1/V2 preview folders. Unknown files are protected and retained. Then run `WORLD/FirstPerson/FirstPersonWorld.tscn` F6. Future master parameter changes require explicitly running the same authoring scene again; FPS startup does not repaint.

Your original `WORLD/Terrain/Gameplay/` and unrelated `PineLandscape/` stay untouched. DTP2 vegetation thinning remains unimplemented, so grass/shrubs/ferns can still grow on soil.
