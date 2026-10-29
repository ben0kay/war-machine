# PoliceStation001 — PS0 two-storey police-station shell

**Branch:** `implement-td0-td1` · **State:** implemented in source; awaiting Godot .NET build, scene preview and physical/visibility tests.

## Open it directly in the Godot editor

1. Pull the branch and **build the C# project** in Godot .NET (or run `dotnet build war-machine.csproj`).
2. Open **`res://WORLD/Content/Assets/Structures/PoliceStation001/PoliceStation001.tscn`** directly. No World scene/F6 or terrain bake is required.
3. Select the root **PoliceStation001** in the Scene tree. Under **Editor-only preview → Editor Preview** switch:
   - **GroundFloor (default):** upper deck/walls and roof are hidden in the editor, so you can inspect ground rooms and stairs from overhead.
   - **UpperFloor:** upper deck, offices and the open stairwell are visible; roof is hidden.
   - **Exterior:** the complete two-storey facade and flat roof are visible.
4. Orbit/pan/zoom the **3D editor viewport**. The `[Tool]` generator creates `Generated` when the scene is loaded; it does not run every frame. The selector changes **visuals only**, not collision. Changes to `PoliceStationMaster.tres` that affect geometry require reopening/reinstancing the scene.

**Important:** This editor view is not a running gameplay/camera preview. Runtime always builds both floors; `ShowRoof=true` is the default, and shared `StructureVisibility` provides top-down fades once the prefab is later placed under the normal world `Structures` tree. It is **not added to WORLD/World.tscn** in PS0.

## Dimensions / shell layout

Footprint **48 × 64m = 3,072m²**, exactly **eight times** ConvenienceStore001's 24 × 16m (384m²) footprint. Ground elevation is 0; upper slab walking plane is **Y=4.2m** by default; upper walls top at **Y=8.0m**. Front/public entrance faces **+Z**, rear service exit **-Z**.

| Level | Shell zones, excluding later furniture |
| --- | --- |
| Ground (0) | Front full-width public lobby (Z=19..32); 5m-wide long central corridor (X ±2.5, Z≈-31..19); multiple west/east rooms divided by cross walls; west-rear stair core; rear exit |
| Upper (1) | West wing subdivided into office rooms; east/front larger open-plan office; east-rear rooms and an empty bathroom enclosure; long central corridor and stair landing |
| Both | Repeated front/rear/side true window apertures with simple tinted **collidable** glass panels; continuous exterior/partition wall assemblies with independent collision and door openings |
| Stair core | Straight 24-tread flight at X≈-13, Z≈-29.5..-19.5 climbing **toward +Z**, with single inclined walkable collider, continuous upper landing and a **real 6 × 14.2m upper deck void** |

The roof is a flat concrete slab with parapet. The exterior door frames include **noncolliding visual leaves propped open**; side-office doors also have open visual leaves. **These are not operational swinging doors:** no hinge, lock, mouse interaction, keycard or motor in PS0. Their openings and matching wall collisions are real. Glazed windows are physically solid, not painted wall rectangles.

## Authoring/performance contract

- Reuses `StructureGeometry`, `StructureLayout.Occupied/Level/Visual/FadeGroup`, `StructureClearance`, `StructureVisibility` and `FadeAwareAim.Link` via the existing structure primitives.
- Logical wall groups fade together from the top-down view. Deck, walls, stairs and roof have separate semantic owners. **Visual opacity never changes collision**.
- `Generated/ClearanceVolumes` reserves both exterior entrances, all office doorways and windows, the continuous corridors, lobby and stair shaft/landing. These are *nonphysical authoring metadata* for a later furniture pass.
- Step risers are visual only; the **single inclined collision shape** prevents a CharacterBody3D from getting stuck on each 17.5cm riser. The upper deck is split around the shaft and the gap is bridged only by the physical upper landing.
- One build on `_Ready`; both `_Process` and `_PhysicsProcess` disabled. No AI, pickups, furnishings, lights, decor, extra test scenes or per-frame geometric updates. This is intended to expose drawing/collision/visibility costs before decoration.
- Root `UseTerrainSupport=false` on the master by default, so the shell is a stand-alone collidable prefab. Set it only when the **entire site is graded and supported** by Terrain3D to avoid overlapping physical floors.

## Local validation checklist

1. **Compile:** `dotnet build war-machine.csproj`; then open `PoliceStation001.tscn` in Godot .NET. Check the preview selector updates without duplicating `Generated` or re-running geometry.
2. **Visual:** Ground, upper and exterior previews; every real door/window gap, upper shaft opening, rear exit, roof and floor elevations. Make sure no corridor crosses solid partition/collision.
3. **Walkability when later placed in the active world:** enter from +Z, walk the full hall, enter side rooms, climb the stair's sloped collision to Y≈4.2, reach the upper corridor and bathroom doorway, return down, exit at -Z. The editor itself does **not** simulate player movement.
4. **Presentation:** Outside/inside wall fade groups, upper deck/roof reveal, AIM1 collision-to-visual aim mapping, FPS opaque surfaces, and camera occlusion while climbing.
5. **Performance:** Compare idle unplaced scene draw-call/material and collision shape counts, then add/stream this one prefab through a **separately authorized** world placement/stress scene. Do not claim FPS gains or acceptable budgets without local measurements.

**Not included:** furnishing or actual furniture placement, working hinged doors, reception desk, bathroom fixtures, indoor lighting, enemies, placement in the active village, navigation mesh, terrain modifications, item/interaction state, save or streaming ownership.

[PS phase tracker](IMPLEMENTATION_PLAN.md) · [Building authoring contract](../BUILDING_AUTHORING_README.md) · [Convenience store reference](../ConvenienceStore001/README.md) · [Multi-floor reference](../Building002/README.md)
