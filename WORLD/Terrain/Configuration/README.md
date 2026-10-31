# Dirt patch configuration (DTP1)

`DirtPatchMaster.tres` is the inspector-editable master for **explicitly baked** forest-soil preview patches. It controls the random seed, square authoring extent around the village, organic patch cluster/trail density, feather/warp, flat-ground threshold and specific village/road/structure exclusions. Set `UsePreviewInGameplay=false` to force the original saved Terrain3D gameplay region. The preview only loads when its source-file hash manifest is current.

**Changing master settings does not repaint the terrain.** Back up and deliberately remove only `WORLD/Terrain/GameplayDirtPreviewV2/` before running `WORLD/Terrain/BakeDirtPreview.tscn` (F6) again. Never delete `WORLD/Terrain/Gameplay/`. See `WORLD/Terrain/DTP_DIRT_GROUND_IMPLEMENTATION_PLAN.md` for lifecycle, limitations and deferred vegetation response.


**Visibility fix:** `ShowNearSpawnPatches=true` adds small witness areas near the player arrival. Village protection now uses `WORLD/Terrain/DirtPatchProtectedAreas.cs` rather than a huge editable rectangle that hid all local dirt. V1 preview files remain untouched; V2 is authored at `GameplayDirtPreviewV2/`.
