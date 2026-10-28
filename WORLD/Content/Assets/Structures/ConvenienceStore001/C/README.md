# ConvenienceStoreMesh.cs — store-only implementation

The `[Tool]` store builder generates a single-storey retail shell with genuine unglazed storefront/side windows, a till counter, four customer display shelves, partitioned stockroom, stock shelving, rear exit, fascia and a flat roof. Real structural walls/roof/floor and the larger interior fixtures have separate static collider shapes. Small decoration and products have no collider. It reuses shared `StructureGeometry` meshes/material helpers and explicitly registers occupied/floor/role metadata for shared `StructureVisibility`.

Only one build per instance; `Generated` prevents duplicates and `_Process/_PhysicsProcess` are disabled. No independent camera, terrain, store inventory, scene placement or other game system references. Inspect the authored scene in Godot and check physical openings, particularly the rear stockroom door and big front windows.

## WT1 wall appearance only

`WallFinish(...)` builds just two opaque `StandardMaterial3D` wall finishes using shared `NoiseTexture2D` albedo and optional normal resources (outside stucco, lighter stockroom partition). World-space triplanar mapping avoids stretching across the procedural box wall spans. This keeps existing exterior camera-occlusion triangle checks working without a custom shader. `UseTexturedWalls=false` restores flat standard materials; no new wall meshes, physics shapes, gameplay logic or frame processes. Compare locally with and without in Godot, check missing-resource errors and top-down wall fades.

## BW1 grouped wall and clearance authoring

The store shell now creates logical FrontWall/RearWall/WestWall/EastWall groups beneath the existing `OutsideWalls` visual-role node; the stockroom `StockroomPartition` role is a marked group itself. Frames and wall decoration accompany their wall, while the separate `InteriorFixtures` group stays opaque indoors. All actual wall pieces and their collision shapes remain separate and the windows/doors remain physical apertures.

Before any shelves/counter/crate are constructed, `StructureClearance` reserves volumes for the three doors, four windows and continuous front-to-rear walking route. Furnishing helpers check their bounds and warn/skip obstructing fixtures. New GLB placement must use the metadata reservations and be last after the shell/circulation/art validation; do not block openings with pictures, clocks, signs or furniture. See the parent README and the shared authoring contract.
