# Dirt patch configuration (DTP1)

`DirtPatchMaster.tres` is the inspector-editable master for **explicitly baked** forest-soil preview patches. It controls the random seed, square authoring extent around the village, organic patch cluster/trail density, feather/warp, flat-ground threshold and a conservative village protection rectangle. Set `UsePreviewInGameplay=false` to force the original saved Terrain3D gameplay region. The preview only loads when its source-file hash manifest is current.

**Changing master settings does not repaint the terrain.** Back up and deliberately remove only `WORLD/Terrain/GameplayDirtPreview/` before running `WORLD/Terrain/BakeDirtPreview.tscn` (F6) again. Never delete `WORLD/Terrain/Gameplay/`. See `WORLD/Terrain/DTP_DIRT_GROUND_IMPLEMENTATION_PLAN.md` for lifecycle, limitations and deferred vegetation response.
