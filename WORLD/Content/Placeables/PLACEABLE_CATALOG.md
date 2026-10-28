# Placeables catalogue — reusable GLB art and size masters

**Current GLB models:** Bookcase001, Book001 and Bed001. All have reusable visual-only `.tscn` wrappers, independent `.tres` scale masters and F1 debug preview. Units are metres; +Y up. No collision, item pickup, rigid-body simulation or permanent placement is implemented for these three.

| Asset | Class | Canonical scene | Native geometry and default size master |
| --- | --- | --- | --- |
| Bookcase001 | **Object** | [Bookcase001.tscn](Objects/Furniture/Bookcases/Bookcase001/Bookcase001.tscn) | [Bookcase001.glb](Objects/Furniture/Bookcases/Bookcase001/Artwork/Bookcase001.glb), ~1.215m W × 1.93m H × 0.415m D, 1,484 tris; [BookcaseMaster.tres](Objects/Furniture/Bookcases/Bookcase001/Configuration/BookcaseMaster.tres) |
| Book001 | **Prop** | [Book001.tscn](Props/Books/Book001/Book001.tscn) | [Book001.glb](Props/Books/Book001/Artwork/Book001.glb), ~0.245m W × 0.05m H × 0.318m L, 420 tris; [BookMaster.tres](Props/Books/Book001/Configuration/BookMaster.tres) |
| Bed001 | **Object** | [Bed001.tscn](Objects/Furniture/Beds/Bed001/Bed001.tscn) | **[Bed001.glb](Objects/Furniture/Beds/Bed001/Artwork/Bed001.glb)**, ~1.32m W × 1.183m H × 2.125m L, 288 tris; [BedMaster.tres](Objects/Furniture/Beds/Bed001/Configuration/BedMaster.tres). Four duvet/fold colours: SlateBlue/MossGreen/WarmSand/DustyRose |
| Car001 | **Object (legacy path)** | [Car001.tscn](../Objects/Cars/Car001/Car001.tscn) | Working static car at its established path; no migration, different settings. |

**How to resize:** Edit `SizeMultiplier = Vector3(1, 1, 1)` in an individual master's `.tres`. X is width, Y is height/thickness, Z is depth/length **relative to the original model**. For example, `Vector3(1.2, 1, 1)` widens the corresponding model 20%. Masters affect all default instances; override with a duplicate settings resource for specific instance variation. These values are **not absolute metres**. Imported GLB art is never rewritten by sizing.

**Bed material variation:** Same GLB for all four, 7 material surfaces total, 2 per-instance material overrides only. No runtime procedural mesh build.

**Preview:** F6 `WORLD/World.tscn` → F1 → select either original GLB wrapper or one of the four bed palettes, spawn and compare. Size changes show in **newly spawned/reloaded** previews. See [Placeable Spawner](../../../DEBUG/PlaceableSpawner/README.md). Future physics colliders must be built/scaled consistently with visuals.
