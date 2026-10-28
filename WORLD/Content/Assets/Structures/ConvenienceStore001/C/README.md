# ConvenienceStoreMesh.cs — store-only implementation

The `[Tool]` store builder generates a single-storey retail shell with genuine unglazed storefront/side windows, a till counter, four customer display shelves, partitioned stockroom, stock shelving, rear exit, fascia and a flat roof. Real structural walls/roof/floor and the larger interior fixtures have separate static collider shapes. Small decoration and products have no collider. It reuses shared `StructureGeometry` meshes/material helpers and explicitly registers occupied/floor/role metadata for shared `StructureVisibility`.

Only one build per instance; `Generated` prevents duplicates and `_Process/_PhysicsProcess` are disabled. No independent camera, terrain, store inventory, scene placement or other game system references. Inspect the authored scene in Godot and check physical openings, particularly the rear stockroom door and big front windows.
