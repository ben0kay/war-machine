# Objects — reusable world asset library

**Current status (2026-10-28):** Bookcase001, Book001 and Bed001 are GLB artwork in stable reusable **visual-only `.tscn` scenes**, each with its own `.tres` size master. Bed001 has four duvet colours but uses only **one GLB model**. Colliders, pickup and permanent placement are future gameplay tasks.

## Object versus Prop

- `Static/` are not pickup-able: Bookcase001 and Bed001 are fixed furniture; they can later be interactive without being carried.
- `Props/` are physically pickup-able in a future IX phase: Book001 is a small book that will eventually support carry/drop/throw.
- `WORLD/Content/Assets/Structures/` owns building shells. Future furnishing should instance these **catalogue scenes** under appropriate `contents` visual roles, not bake the same art into procedural building geometry.
- `INTERACTION/` owns targeting, hinges, physics carrying and breakage. `INVENTORY/` and `EQUIPMENT/` own inventory/equipment identities, not reusable art.

AM0 consolidated the former libraries here. [Static](Static/README.md) includes the existing cars and lamps; [Props](Props/README.md) includes future liftable books. No duplicate legacy asset folders remain.

## Current GLB catalogue

- [Bookcase001](Static/Furniture/Bookcases/Bookcase001/README.md): original GLB, a new visual `Bookcase001.tscn`, plus `Configuration/BookcaseMaster.tres`.
- [Book001](Props/Books/Book001/README.md): original GLB, a new visual `Book001.tscn`, plus `Configuration/BookMaster.tres`.
- [Bed001](Static/Furniture/Beds/Bed001/README.md): **converted the original C# geometry to `Artwork/Bed001.glb`**, kept a stable visual `Bed001.tscn` and `Configuration/BedMaster.tres`, plus 4 selectable cloth palettes.

The common [Shared/PlaceableSizeSettings.cs](Shared/PlaceableSizeSettings.cs) exposes `SizeMultiplier = Vector3(1, 1, 1)`: X=width, Y=height/thickness, Z=depth/length, **relative to the GLB's native dimensions**. Change each asset's own master and respawn/reload to update all default instances. Use a duplicated/overridden master for individually sized copies. The common [PlaceableVisual.cs](Shared/PlaceableVisual.cs) applies size once on `_Ready`. The bed's [BedVisual.cs](Static/Furniture/Beds/Bed001/BedVisual.cs) changes materials only, **never rebuilds meshes**.

See [PLACEABLE_CATALOG.md](PLACEABLE_CATALOG.md) for canonical paths, dimensions, variant names and default masters.

## Authoring guidance for future AI chats

**Start with [OBJECT_AUTHORING_GUIDE.md](OBJECT_AUTHORING_GUIDE.md)** for the reusable GLB-first visual asset workflow, scene/pivot/material conventions, per-asset masters, catalogue/F1 registration and deferred collision boundary. AM0 is implemented; this is its canonical location.

1. Prefer optimized imported GLBs under `Artwork/` for furniture, small props and appliances. Use a stable scene wrapper and a `.tres` per asset for frequently tweaked sizes; do not duplicate GLB source files merely for colour variants.
2. Use `C/` only when generating visible geometry in C#. A small interaction/colour-setting script belongs alongside its scene or `Shared/`. Preserve pivot, dimensions and material surface naming/order on re-export.
3. Share meshes/materials across instances; avoid per-frame work or physics on decorative clutter. The size helper is event/ready-driven and dormant at rest.
4. Add physical collision only when authorized: Bookcase001 needs separate thin shelf colliders, Book001 can use one small sleeping rigid body, Bed001 needs appropriate frame/bed collision. Keep colliders in agreement with visible size, particularly when scale is not uniform.
5. Reuse IX0–IX8 in `INTERACTION/IMPLEMENTATION_PLAN.md`; do not place/migrate assets in active world or inventory as part of visual art authoring.
6. [Car001](Static/Cars/Car001/README.md) now lives under `Static/Cars/`; it retains its procedural geometry, original settings and existing collision. Its master is not a GLB size master.

## F1 visual previews

Run `WORLD/World.tscn` or the FPS debug scene with F6, press **F1**, then spawn Bookcase001, Book001 or any of the four Bed001 palettes using the [Placeable Spawner](../../../../DEBUG/PlaceableSpawner/README.md). All previews use the **canonical scene and corresponding `.tres`**. Bed and bookcase are grounded by their Y=0 source pivot; book hovers to allow close inspection. These are temporary previews without collision, gravity, pickup or saved placement. Godot import/build/visual acceptance remains pending locally.
