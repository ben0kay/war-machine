> **SOC0 static shape integration (2026-10-28):** Bookcase001, Bed001, Table001 and Hedge001 use the shared `PlaceableVisual` AutoBox (one StaticBody3D/BoxShape3D derived from imported mesh bounds). PineTree001 retains no collision, and Car001/StreetLamp001/doors retain independent physics. For future bus-stop shelters or open shelves, switch to CustomBoxes. [Shared collision documentation](../Shared/README.md). Local Godot validation pending.

# Static objects — cannot be picked up

Non-pickup architectural objects may be interactive or physically moving (a door leaf may be a RigidBody3D). Folder membership does not automatically create a StaticBody3D.

- [Appliances/Fridges/Fridge001](Appliances/Fridges/Fridge001/README.md): two-door refrigerator/freezer GLB, four finishes (warm white/silver/graphite/seafoam), separate future door pivots, shared size master, SOC0 AutoBox and F1 previews.
- [Doors/Door001](Doors/Door001/README.md): DOOR1 reusable hinged rigid-body service door with a fixed hinge and DOOR2 handle anchors; installed only at the convenience store rear exit.
- [Containers/Dumpsters/DumpsterBin001](Containers/Dumpsters/DumpsterBin001/README.md): huge dark-green industrial dumpster, two black hinged-pivot lids, **no wheels**; original GLB, size master, SOC0 AutoBox and F1 discoverability, no permanent placement.
- [Containers/WheelieBins/WheelieBin001](Containers/WheelieBins/WheelieBin001/README.md): low-poly dark green household wheelie bin with separate black lid, rear wheels, standard size master and SOC0 AutoBox; F1 eligible, no permanent world placement.
- [Cars/Car001](Cars/Car001/README.md): original stationary procedural car with existing collision and configuration, now at its single canonical Assets path.
- [Furniture/Bookcases/Bookcase001](Furniture/Bookcases/Bookcase001/README.md): GLB bookcase with size master and SOC0 AutoBox collision.
- [Furniture/Beds/Bed001](Furniture/Beds/Bed001/README.md): one GLB, four bedding palettes and SOC0 AutoBox collision.
- [Furniture/Tables/Table001](Furniture/Tables/Table001/README.md): GLB six-seat dining table, size master, three finishes and SOC0 AutoBox collision.
- [Vegetation/Trees/PineTree001](Vegetation/Trees/PineTree001/README.md): high-detail visual-only imported pine tree GLB comparison, without modifying active procedural forest rendering.
- [Landscaping/Hedges/Hedge001](Landscaping/Hedges/Hedge001/README.md): clipped dark-green ornamental hedge GLB with per-asset size master; manual static landscaping, NOT procedural bush placement; SOC0 AutoBox collision.
- [Lighting/StreetLamp001](Lighting/StreetLamp001/README.md): reusable fixed lamp with existing configurable lighting; four instances belong to the convenience-store site.

Cars retain their independent existing physical behavior; **fixed GLB furniture now gets collision only when its canonical scene explicitly sets SOC0 AutoBox/CustomBoxes**. See [Objects foundation](../README.md) and the [authoring guide](../OBJECT_AUTHORING_GUIDE.md). Buildings live in `Assets/Structures`, independently of their furnishings.

**Future asset instruction:** Read [OBJECT_AUTHORING_GUIDE.md](../OBJECT_AUTHORING_GUIDE.md) before creating a new Static object. Decide and document `CollisionMode = 0` None / `1` AutoBox / `2` CustomBoxes, based on whether its openings must remain traversable. For a bus stop shelter prefer several custom boxes, not a blocking whole-envelope AutoBox.
