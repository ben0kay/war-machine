# ConvenienceStore001 master configuration

`ConvenienceStoreMaster.tres` now uses typed `ConvenienceStoreSettings : StructureSettings`, referenced by both the store generator and the direct shared `StructureVisibility` child. Defaults: 24m width, 16m depth, 4.2m wall height, 5.2m rear-room depth, 1.45m shelving, furniture enabled and a six-colour wall/trim/accent/floor/roof/shelving palette. Inherited exterior and indoor fade values are kept.

The scene generates geometry only once at `_Ready`; re-instance after changing these authoring values. This master does not affect any other building.

## WT1 painted-stucco controls (new material-only experiment)

- `UseTexturedWalls=true` → mottled wall finish; `false` → original flat StandardMaterial3D style.
- `Walls=Color(0.73, 0.71, 0.66)` → warm off-white tint multiplied by the shared plaster colour texture.
- `WallTextureRepeatsPerMeter=0.55` → triplanar tiling frequency in world metres, across all separate wall boxes (0.15–2).
- `WallBumpStrength=0.30` → fine grain normal strength (0 disables normal map).
- `WallRoughness=0.94` → matte surface finish.

Shared authored resources at `../Artwork/Materials/StuccoColor.tres` and `StuccoNormal.tres`. Two material instances per store, one outside and one brighter inside; shared textures and no C# frame processing. `ConvenienceStoreMesh` reads settings once at `_Ready`, so restart/reinstance after changing this master. Flat and textured options use the same walls, collisions and visibility owners. Godot compile/render pending.

## CS2 larger-store dimensions

`Width=24` (range 16–32m), `Depth=16` (range 12–24m), `StockroomDepth=5.2` (range 4.5–8m). The default is intentionally **double the 16×12m floor area**, not double each axis (which would quadruple floor area). Rear rooms are divided by a fixed central service passage and have two independent internal apertures; only the front and rear are external entrances. Site `.tscn` uses a fixed position/marker chosen for **these default dimensions**; if resizing the master again, resurvey the `ConvenienceStoreSite001.tscn` placement, approach geometry and site markers to prevent gaps/collisions. Geometry is rebuilt only when the prefab is re-instanced. No new runtime settings processing.
