# ConvenienceStore001 master configuration

`ConvenienceStoreMaster.tres` now uses typed `ConvenienceStoreSettings : StructureSettings`, referenced by both the store generator and the direct shared `StructureVisibility` child. Defaults: 16m width, 12m depth, 4.2m wall height, 4m stockroom depth, 1.45m shelving, furniture enabled and a six-colour wall/trim/accent/floor/roof/shelving palette. Inherited exterior and indoor fade values are kept.

The scene generates geometry only once at `_Ready`; re-instance after changing these authoring values. This master does not affect any other building.

## WT1 painted-stucco controls (new material-only experiment)

- `UseTexturedWalls=true` → mottled wall finish; `false` → original flat StandardMaterial3D style.
- `Walls=Color(0.73, 0.71, 0.66)` → warm off-white tint multiplied by the shared plaster colour texture.
- `WallTextureRepeatsPerMeter=0.55` → triplanar tiling frequency in world metres, across all separate wall boxes (0.15–2).
- `WallBumpStrength=0.30` → fine grain normal strength (0 disables normal map).
- `WallRoughness=0.94` → matte surface finish.

Shared authored resources at `../Artwork/Materials/StuccoColor.tres` and `StuccoNormal.tres`. Two material instances per store, one outside and one brighter inside; shared textures and no C# frame processing. `ConvenienceStoreMesh` reads settings once at `_Ready`, so restart/reinstance after changing this master. Flat and textured options use the same walls, collisions and visibility owners. Godot compile/render pending.
