# Terrain3D texture assets

This folder contains **identical copies** of four original ambientCG-based Terrain3D surface PNGs from `demo/assets/textures/`, plus a new **original generated seamless dirt texture pair**. The original demo images remain untouched.

| Layer | Albedo/height | Normal/roughness |
| --- | --- | --- |
| Ground (Ground037) | `ground037_alb_ht.png` | `ground037_nrm_rgh.png` |
| Cliff (Rock023) | `rock023_alb_ht.png` | `rock023_nrm_rgh.png` |
| **Forest Dirt (TDT1)** | **`forest_dirt_001_alb_ht.png`** | **`forest_dirt_001_nrm_rgh.png`** |

**Used by:** `WORLD/Terrain/TerrainAssets.tres`, referenced by `WORLD/World.tscn` and rebound through `WorldController.BindTerrainAppearance`. The Terrain3D material and saved heightmaps live separately under `WORLD/Terrain/`. These are terrain-surface textures, not the instanced grass model atlas.

**Reimport:** Allow Godot to import the four new paths after pulling. Existing scene/resource IDs and Terrain3D layer ordering are preserved.

**Original sources and license:** ambientCG [Ground037](https://ambientcg.com/view?id=Ground037) and [Rock023](https://ambientcg.com/view?id=Rock023) (CC0 1.0); see the unchanged `demo/assets/textures/asset_licenses.txt`. Terrain3D addon licensing remains in `addons/terrain_3d/LICENSE.txt`.


### Forest Dirt — paintable texture slot 2

The two **1024×1024 RGBA** PNGs use Terrain3D channel packing: **RGB colour + alpha height** for `alb_ht`, and **RGB OpenGL normal + alpha roughness** for `nrm_rgh`. It is a repeatable brown forest soil/grit/pebble tile authored procedurally with periodic noise and boundary-wrapped detail, not a photograph or a complete prepainted dirt path. The ground layer (slot 1) and cliff layer (slot 0) still work exactly as before. Select **Forest Dirt** in the Terrain3D paint brush to add curved earth patches/paths; save resulting control-map changes only when desired. If these images import with a different GPU compression format than the existing terrain images, set their import to VRAM Compressed, High Quality, Mipmaps ON and Normal Map Disabled, then reimport; **all Terrain3D albedo textures and all normal textures must match size and GPU format**. No material, heightmap or scene-node changes are required.
