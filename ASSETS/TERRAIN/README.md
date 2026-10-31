# Terrain3D texture assets

This folder contains **identical copies** of the four active Terrain3D surface PNGs originally supplied in `demo/assets/textures/`. The originals remain untouched.

| Layer | Albedo/height | Normal/roughness |
| --- | --- | --- |
| Ground (Ground037) | `ground037_alb_ht.png` | `ground037_nrm_rgh.png` |
| Cliff (Rock023) | `rock023_alb_ht.png` | `rock023_nrm_rgh.png` |

**Used by:** `WORLD/Terrain/TerrainAssets.tres`, referenced by `WORLD/World.tscn` and rebound through `WorldController.BindTerrainAppearance`. The Terrain3D material and saved heightmaps live separately under `WORLD/Terrain/`. These are terrain-surface textures, not the instanced grass model atlas.

**Reimport:** Allow Godot to import the four new paths after pulling. Existing scene/resource IDs and Terrain3D layer ordering are preserved.

**Original sources and license:** ambientCG [Ground037](https://ambientcg.com/view?id=Ground037) and [Rock023](https://ambientcg.com/view?id=Rock023) (CC0 1.0); see the unchanged `demo/assets/textures/asset_licenses.txt`. Terrain3D addon licensing remains in `addons/terrain_3d/LICENSE.txt`.
