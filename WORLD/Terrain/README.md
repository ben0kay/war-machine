# Terrain resources and saved world

`Data/terrain3d_00_00.res` is an independently baked 1024m Terrain3D region containing height/control/color maps and grass instance data. It is loaded directly at runtime and remains editable with Terrain3D's editor tools. Commit saved sculpting changes deliberately.

`TerrainMaterial.tres` adapts the main Terrain3D demo's autoshader, cliff base (0), grass overlay (1), auto slope 0.45, macro noise, projection and dual scaling. World background is disabled so the finite edge is explicit. Better distant scenery belongs to WE2/WE3.

`TerrainAssets.tres` owns texture assignments and a generated three-card grass mesh. `GrassMaterial.tres` and `Grass.gdshader` share GPU wind across instances. Grass culls around 100m; no per-blade scripts. Shape/density/slope are bake settings in WorldMaster; wind strength/speed/direction are runtime settings in EnvironmentMaster. Zero wind strength gives stationary grass geometry; it does not remove instances.

Textures reference original files rather than duplicating large images. Retain `demo/assets/textures/asset_licenses.txt`: Ground037 and Rock023 are CC0. Grass shader technique is adapted from Terrain3D's MIT example; retain `addons/terrain_3d/LICENSE.txt`. No addon or demo source was modified.
