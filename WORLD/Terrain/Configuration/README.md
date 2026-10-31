# Dirt patch configuration (DTP1)

`DirtPatchMaster.tres` is the inspector-editable master for **explicitly baked** forest-soil preview patches. It controls the random seed, square authoring extent around the village, organic patch cluster/trail density, feather/warp, flat-ground threshold and specific village/road/structure exclusions. Set `UsePreviewInGameplay=false` to force the original saved Terrain3D gameplay region. The preview only loads when its source-file hash manifest is current.

**Changing master settings does not repaint the terrain.** Back up and deliberately remove only `WORLD/Terrain/GameplayDirtPreviewV2/` before running `WORLD/Terrain/BakeDirtPreview.tscn` (F6) again. Never delete `WORLD/Terrain/Gameplay/`. See `WORLD/Terrain/DTP_DIRT_GROUND_IMPLEMENTATION_PLAN.md` for lifecycle, limitations and deferred vegetation response.


**Visibility fix:** `ShowNearSpawnPatches=true` adds small witness areas near the player arrival. Village protection now uses `WORLD/Terrain/DirtPatchProtectedAreas.cs` rather than a huge editable rectangle that hid all local dirt. V1 preview files remain untouched; V2 is authored at `GameplayDirtPreviewV2/`.


**DTP1 V3 local test:** `SingleSpawnPreviewPatch=true` is the default. It paints just one irregular dirt oval centred at `SpawnXZ + (-20, -8)` (approximately X=492,Z=542) off the access road near the parking lot. `ShowNearSpawnPatches=false` suppresses prior extra witnesses. Re-run `BakeDirtPreview.tscn` F6 after pulling; this creates a new `GameplayDirtPreviewV3/`, preserving earlier previews and the original terrain. No locator/debug marker is added.
