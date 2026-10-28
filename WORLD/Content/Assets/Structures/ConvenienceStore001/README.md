> **BW1 (2026-10-28): grouped wall fading + layout clearance implemented in source; local Godot validation pending.** The store is now the single-floor authoring reference. See [building authoring contract](../BUILDING_AUTHORING_README.md); older historical descriptions below remain relevant where not superseded.

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

Future: add optional glass, detailed product materials, real swinging doors and interactions, sign art, shopping mechanics and light/audio **only when requested**. The later WT1 painted-stucco material trial is documented below.

## Terrain-backed floor option

UseTerrainSupport defaults false, retaining standalone physical floor support. The village convenience-store site explicitly enables it and lowers the Structures group by 0.12m so its floor walking plane matches the surrounding graded Terrain3D. That instance omits the duplicate floor box, while walls/roof/shelves retain collision. Floor art is raised 1cm above its nominal physical plane to avoid z-fighting; no extra physical step or access wedge. Only enable this option over verified solid terrain covering the whole interior. Ground/occupancy metadata and store-only visibility remain unchanged. Local build/traversal check pending.

## WT1 — Convenience-store wall finish trial (2026-10-28)

**Implemented in source; awaiting local Godot/.NET build and visual testing.** Existing plain cream exterior walls and the interior stockroom partition now use two subtly different **opaque painted-stucco StandardMaterial3D** materials: shared mottled colour noise, fine normal-map grain and matte roughness. This is a **materials-only experiment**; wall geometry, holes, collision, checkout/shelving, roof and facade accents were not altered. Opaque StandardMaterial3D intentionally preserves the geometry-confirmed camera fade's structural-blocker detection.

**Tweak:** `Configuration/ConvenienceStoreMaster.tres` → `UseTexturedWalls` (false restores original untextured rendering), `Walls` (base warm-light colour), `WallTextureRepeatsPerMeter` (default 0.55), `WallBumpStrength` (0.30; zero disables normal mapping), `WallRoughness` (0.94). New shared `Artwork/Materials/StuccoColor.tres` and `StuccoNormal.tres` are 256×256 seamless `NoiseTexture2D` resources. World-space triplanar mapping keeps the wall pattern consistent between short and long segments. Reinstance/restart after master edits; no settings polling or per-frame C# work.

**Test:** pull branch, `dotnet build war-machine.csproj`, run `WORLD/World.tscn` with F6 and inspect convenience store front/rear and sides, interior stockroom, under daylight/night lighting. Flip `UseTexturedWalls` OFF/ON **with a scene restart** for an A/B comparison; lower `WallBumpStrength` if grain feels too sharp. Verify physically open doors/windows, collision and both top-down roof/wall fade and FPS visuals still work. Godot import, material/normal look and performance are unverified here. Triplanar shading adds GPU texture reads; profile before applying this to an entire town.

## BW1 — reference redesign of visibility and circulation

This pass does **not** merge the wall geometry into one mesh, move the parking lot or change the underlying floor, windows, doors, collision or store dimensions. Each visually connected wall is now a logical fade group: `FrontWall` (front pier/window spans/lintel, sign + entrance frame), `RearWall` (including service-door frame), `WestWall`, `EastWall` (including corner trim), and `StockroomPartition` (both sides, door head + frame). A screen-space obstruction on any wall mesh fades its *entire connected group*; adjoining walls stay separately controlled. Roof/floors/whole-building outside fading remain independent. No new physics or rendering processing.

**Circulation-first rule:** before calling Furnish, a `StructureClearance` object stores named, building-local protection volumes under `Generated/ClearanceVolumes/`: FrontEntrance, StockroomDoor, RearServiceExit, RetailMainAisle, StockroomCrossAisle, StockroomServiceAisle, two front-window apertures and two side-window apertures. Main store aisle runs from front entry (X≈0) to stockroom door (X≈0), turns east across the stockroom and continues rearward to the rear service door. Doors have their own headroom volumes; glazing remains absent, exactly as before. No pictures, clocks, signs or future GLBs are allowed to cover window/door openings. Existing shelf/counter/crate generators check their complete bounds against those reserved volumes *before* construction and warn/skip a violating fixture. For future model placement, use `StructureClearance.AllowsPlacement` against the same cached marker collection; physical player navigation should still be checked locally.

### BW1 local acceptance

After a C# build, F6 `WORLD/World.tscn` in top-down and then FPS. Enter the store through the open front door, walk down the central aisle through the partition, turn across the stockroom and exit the rear door. Verify no fixtures or decorations intersect either door or any window aperture; store appearance is substantially unchanged. Stand behind the stockroom wall (the reported half-fade case): both `DividerWest`, `DividerDoorHead`, `DividerEast` and their frame now fade together. Inspect each exterior wall separately; confirm neighboring perpendicular walls do not fade merely because an adjacent wall does. Verify indoor roof and exterior full-building opacity, AIM1 aim through faded walls, and **actual bullets and movement collision remaining solid**. Run both camera modes and confirm no warnings/errors. Shader/scene rendering, traversal and performance not tested in this repository-only pass.
