# ConvenienceStore001 — standalone roadside convenience-store prefab

**Status: store-only model implemented; local Godot C# build, render and walking tests pending.** This is **not the complete PL0B phase**. The store now appears as an instance in the reusable convenience-store site; neither is placed in `WORLD/World.tscn` or the FPS world.

## What is modeled

- Default **16 × 12 m** footprint, **4.2 m** high, flat roof with parapet, short awning, coloured fascia/sign backing and small roof equipment. Local **+Z is the parking-facing storefront**; **-Z is the rear service side**.
- **Two very large front-facing, physically empty window openings**, flanking a central **2.5m walk-through entrance**. Large empty openings in both side walls. The window apertures are actual gaps between structural wall spans, not hidden glass or cutaway colour. There are **no glass meshes, transparent panes or pane colliders**.
- Retail interior with **four shelved aisle fixtures**, simple merchandise, a recognizable **checkout/till counter**, counter top, register, till screen and card reader. An open central aisle connects front entrance to stockroom.
- Separate **rear stockroom**, divided by real colliding walls with a **2.32m clear interior doorway**, two stock shelves and a supply crate. A **2.4m rear service door opening** exits outdoors; no door leaves yet.
- Solid physical static collisions for main floor, walls, roof, large shelves, counter and supply crate. Cosmetic merchandise, painted fascia, window frames and awning are decorative only. The 0.12m-tall base slab can later align with ParkingLot001's 0.12m-high asphalt surface, provided the complete site is placed on suitably level terrain.
- Three passive Marker3D anchors: `Generated/Markers/FrontEntrance`, `StockroomEntrance` and `RearExit`. No interactions, pickups, AI, loot, sounds, lighting or car placement.

## Ownership and shared visibility

`ConvenienceStore001.tscn` has one `[Tool]` `ConvenienceStoreMesh` root and one **direct `StructureVisibility` child**, referencing the same `Configuration/ConvenienceStoreMaster.tres`. `ConvenienceStoreSettings.cs : StructureSettings` exposes dimensions, stockroom size, shelf height, furniture toggle and six basic palette colours plus the inherited fade settings. `C/ConvenienceStoreMesh.cs` builds using `Structures/Shared/C/StructureGeometry.cs` and metadata from `StructureLayout.Occupied/Level/Visual`. Independent `walls`, `contents` and `roof` roles enable the same top-down reveal/occlusion logic as existing buildings; collision never fades. No per-frame or per-physics process.

**This building is now instanced by the parent site; do not place the whole site in the world until PL1.** For top-down visibility when it is eventually instanced, the building needs to be discoverable under the world's `Structures` hierarchy, or deliberately registered by the owner. Neither the parking lot nor street lamps should inherit its fading.

## Testing and later additions

Open `ConvenienceStore001.tscn` directly in Godot after C# compilation. Observe the big open windows and roof. For a temporary collision test, walk from front entrance through the central aisle, into stockroom and out the rear exit; check that no wall/glass colliders block windows or doors, and counters/shelves block correctly. Check the shared roof fade by temporarily instancing under the normal top-down structure root. The mesh is built at `_Ready`, so re-instance to apply master changes (no polling rebuild).

Future: add optional glass, detailed product materials, real swinging doors and interactions, sign art, shopping mechanics and light/audio **only when requested**. This pass deliberately includes no new texture/material system.
