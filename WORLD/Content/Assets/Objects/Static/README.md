> **SOC0 static shape integration (2026-10-28):** Bookcase001, Bed001, Table001 and Hedge001 use the shared `PlaceableVisual` AutoBox (one StaticBody3D/BoxShape3D derived from imported mesh bounds). PineTree001 retains no collision, and Car001/StreetLamp001/doors retain independent physics. For future bus-stop shelters or open shelves, switch to CustomBoxes. [Shared collision documentation](../Shared/README.md). Local Godot validation pending.

# Static objects — cannot be picked up

Non-pickup architectural objects may be interactive or physically moving (a door leaf may be a RigidBody3D). Folder membership does not automatically create a StaticBody3D.

- [Doors/Door001](Doors/Door001/README.md): DOOR1 reusable hinged rigid-body service door with a fixed hinge and DOOR2 handle anchors; installed only at the convenience store rear exit.
- [Cars/Car001](Cars/Car001/README.md): original stationary procedural car with existing collision and configuration, now at its single canonical Assets path.
- [Furniture/Bookcases/Bookcase001](Furniture/Bookcases/Bookcase001/README.md): visual-only GLB bookcase with size master.
- [Furniture/Beds/Bed001](Furniture/Beds/Bed001/README.md): one visual-only GLB and four bedding palettes.
- [Furniture/Tables/Table001](Furniture/Tables/Table001/README.md): reusable visual-only six-seat dining table GLB, size master and three wood finishes.
- [Vegetation/Trees/PineTree001](Vegetation/Trees/PineTree001/README.md): high-detail visual-only imported pine tree GLB comparison, without modifying active procedural forest rendering.
- [Landscaping/Hedges/Hedge001](Landscaping/Hedges/Hedge001/README.md): clipped dark-green ornamental hedge GLB with per-asset size master; manual static landscaping, NOT procedural bush placement, visual-only.
- [Lighting/StreetLamp001](Lighting/StreetLamp001/README.md): reusable fixed lamp with existing configurable lighting; four instances belong to the convenience-store site.

Cars retain their current physical behavior; GLB furniture does not gain collision through this migration. See [Objects foundation](../README.md) and the [authoring guide](../OBJECT_AUTHORING_GUIDE.md). Buildings live in `Assets/Structures`, independently of their furnishings.
