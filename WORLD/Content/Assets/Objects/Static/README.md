# Static objects — cannot be picked up

Fixed objects may still be interactive in a later feature. Folder membership does not automatically create a StaticBody3D.

- [Cars/Car001](Cars/Car001/README.md): original stationary procedural car with existing collision and configuration, now at its single canonical Assets path.
- [Furniture/Bookcases/Bookcase001](Furniture/Bookcases/Bookcase001/README.md): visual-only GLB bookcase with size master.
- [Furniture/Beds/Bed001](Furniture/Beds/Bed001/README.md): one visual-only GLB and four bedding palettes.
- [Furniture/Tables/Table001](Furniture/Tables/Table001/README.md): reusable visual-only six-seat dining table GLB, size master and three wood finishes.
- [Vegetation/Trees/PineTree001](Vegetation/Trees/PineTree001/README.md): high-detail visual-only imported pine tree GLB comparison, without modifying active procedural forest rendering.
- [Landscaping/Hedges/Hedge001](Landscaping/Hedges/Hedge001/README.md): clipped dark-green ornamental hedge GLB with per-asset size master; manual static landscaping, NOT procedural bush placement, visual-only.
- [Lighting/StreetLamp001](Lighting/StreetLamp001/README.md): reusable fixed lamp with existing configurable lighting; four instances belong to the convenience-store site.

Cars retain their current physical behavior; GLB furniture does not gain collision through this migration. See [Objects foundation](../README.md) and the [authoring guide](../OBJECT_AUTHORING_GUIDE.md). Buildings live in `Assets/Structures`, independently of their furnishings.
