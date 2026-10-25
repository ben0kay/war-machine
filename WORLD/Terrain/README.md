# Terrain resources and saved world

`Data/terrain3d_00_00.res` is an independently baked 1024m Terrain3D region containing height/control/color maps and grass instance data. It is loaded directly at runtime and remains editable with Terrain3D's editor tools. Commit saved sculpting changes deliberately.

`TerrainMaterial.tres` adapts the main Terrain3D demo's autoshader, cliff base (0), grass overlay (1), auto slope 0.45, macro noise, projection and dual scaling. World background is disabled so the finite edge is explicit. Better distant scenery belongs to WE2/WE3.

`TerrainAssets.tres` retains the original baked crossed-card asset as **LegacyBakedGrass, enabled=false**. `GrassMaterial.tres` / `Grass.gdshader` are retained only for legacy authoring and before/after comparison. They are not the visible grass renderer. Native saved instance data is preserved without modification.

Visible, terrain-following ribbon GPU grass now belongs to `VEGETATION/`, with density/range/height/quality under VegetationMaster and wind under EnvironmentMaster. No regeneration of terrain or native grass instances is needed; WorldMaster's Grass Bake settings affect only legacy offline authoring. See `VEGETATION/README.md` for the active renderer.

Textures reference original files rather than duplicating large images. Retain `demo/assets/textures/asset_licenses.txt`: Ground037 and Rock023 are CC0. Grass shader technique is adapted from Terrain3D's MIT example; retain `addons/terrain_3d/LICENSE.txt`. No addon or demo source was modified.
