# PoliceStation001 — PS1 two-storey police-station shell

**Branch:** `implement-td0-td1` · **PS1 shell correction:** stair door/approach and partition/window junction pass implemented in source; local Godot build, preview and physical acceptance pending.

## Open it directly in the Godot editor

1. Pull the branch and **build the C# project** in Godot .NET (or run `dotnet build war-machine.csproj`).
2. Open **`res://WORLD/Content/Assets/Structures/PoliceStation001/PoliceStation001.tscn`** directly. No World scene/F6 or terrain bake is required.
3. Select the root **PoliceStation001** in the Scene tree. Under **Editor-only preview → Editor Preview** switch:
   - **GroundFloor (default):** upper deck/walls and roof are hidden in the editor, so you can inspect ground rooms and stairs from overhead.
   - **UpperFloor:** upper deck, offices and the open stairwell are visible; roof is hidden.
   - **Exterior:** the complete two-storey facade and flat roof are visible.
4. Under the same Inspector group, click **Build / Refresh Preview** once if the 3D view is blank, particularly after pulling new scripts or compiling while the scene was already open. The **Output** dock should report `[PoliceStation001] Editor shell generated`. If it reports a C# exception instead, copy the first error.
5. Orbit/pan/zoom the **3D editor viewport** (press **F** after selecting the root to focus near its centre). The `[Tool]` generator creates `Generated` on scene load; the refresh button rebuilds it explicitly and the selector changes only visual visibility. **Saving the scene does not bake the procedural mesh/collision children into its .tscn**; only a lightweight `Generated` anchor may persist. Geometry changes in `PoliceStationMaster.tres` can be applied by clicking Refresh.

**Important:** This editor view is not a running gameplay/camera preview. Runtime always builds both floors; `ShowRoof=true` is the default, and shared `StructureVisibility` provides top-down fades once the prefab is later placed under the normal world `Structures` tree. It is **not added to WORLD/World.tscn** in PS0.

## Dimensions / shell layout

Footprint **48 × 64m = 3,072m²**, exactly **eight times** ConvenienceStore001's 24 × 16m (384m²) footprint. Ground elevation is 0; upper slab walking plane is **Y=4.2m** by default; upper walls top at **Y=8.0m**. Front/public entrance faces **+Z**, rear service exit **-Z**.

| Level | Shell zones, excluding later furniture |
| --- | --- |
| Ground (0) | Lobby, separate front-left waiting/intake zone, **front-right U-stair** by the entrance, 5m main corridor, larger administration/operations zones, varied interview/records rooms and service spaces |
| Upper (1) | West offices, east/front open-plan office, east-rear rooms and bathroom, and a new front-right upper stair landing/exit joined to the upper floor |
| Both | Repeated front/rear/side true window apertures with simple tinted **collidable** glass panels; continuous exterior/partition wall assemblies with independent collision and door openings |
| Stair core | **Front-right U-shaped stair** (X≈8.7..18.2, Z≈18..30.4), two 12-riser flights starting at Z=23, 2.4m turn landing, two slope colliders, real 9.5×12.4m upper void, with 2.2m level approach from the door centre |

The roof is a flat concrete slab with parapet. The exterior door frames include **noncolliding visual leaves propped open**; side-office doors also have open visual leaves. **These are not operational swinging doors:** no hinge, lock, mouse interaction, keycard or motor in PS0. Their openings and matching wall collisions are real. Glazed windows are physically solid, not painted wall rectangles.

## Authoring/performance contract

- Reuses `StructureGeometry`, `StructureLayout.Occupied/Level/Visual/FadeGroup`, `StructureClearance`, `StructureVisibility` and `FadeAwareAim.Link` via the existing structure primitives.
- Logical wall groups fade together from the top-down view. Deck, walls, stairs and roof have separate semantic owners. **Visual opacity never changes collision**.
- `Generated/ClearanceVolumes` reserves both exterior entrances, all office doorways and windows, the continuous corridors, lobby and stair shaft/landing. These are *nonphysical authoring metadata* for a later furniture pass.
- Step risers are visual only; **each U-stair flight** has one walkable slope collider, a solid turn landing and upper landing. A real upper deck opening surrounds the new core.
- One build on `_Ready`, with optional explicit **Build / Refresh Preview** button; both `_Process` and `_PhysicsProcess` disabled. No AI, pickups, furnishings, lights, decor, extra test scenes or per-frame geometric updates. This is intended to expose drawing/collision/visibility costs before decoration.
- Root `UseTerrainSupport=false` on the master by default, so the shell is a stand-alone collidable prefab. Set it only when the **entire site is graded and supported** by Terrain3D to avoid overlapping physical floors.

## Local validation checklist

1. **Compile:** `dotnet build war-machine.csproj`; then open `PoliceStation001.tscn` in Godot .NET. Confirm the editor-owned `Generated` anchor appears and the station mesh is visible. If already open when code was compiled, click **Build / Refresh Preview**; verify output and selectors, and ensure repeated refreshes never duplicate the generated geometry.
2. **Visual:** Ground, upper and exterior previews; every real door/window gap, upper shaft opening, rear exit, roof and floor elevations. Make sure no corridor crosses solid partition/collision.
3. **Walkability when later placed in the active world:** enter from +Z, walk the full hall, enter side rooms, climb both stairs and the middle landing to Y≈4.2, reach the upper corridor and bathroom doorway, return down, exit at -Z. The editor itself does **not** simulate player movement.
4. **Presentation:** Outside/inside wall fade groups, upper deck/roof reveal, AIM1 collision-to-visual aim mapping, FPS opaque surfaces, and camera occlusion while climbing.
5. **Performance:** Compare idle unplaced scene draw-call/material and collision shape counts, then add/stream this one prefab through a **separately authorized** world placement/stress scene. Do not claim FPS gains or acceptable budgets without local measurements.

**Not included:** furnishing or actual furniture placement, working hinged doors, reception desk, bathroom fixtures, indoor lighting, enemies, placement in the active village, navigation mesh, terrain modifications, item/interaction state, save or streaming ownership.

[PS phase tracker](IMPLEMENTATION_PLAN.md) · [Building authoring contract](../BUILDING_AUTHORING_README.md) · [Convenience store reference](../ConvenienceStore001/README.md) · [Multi-floor reference](../Building002/README.md)

## PS1 structural redesign — 2026-10-28

Following the owner's direct Godot preview feedback, the original rear-left straight staircase has moved into the **front-right of the public lobby**, with two opposing twelve-riser flights and a real 180-degree turn landing. The ground floor now has a separate public waiting/intake room, a large admin zone, nonmatching interview/records rooms and larger rear service spaces, rather than rows of identical rectangles. Every new doorway has a true hole in the wall/collision plus `StructureClearance` metadata. The upstairs corridor and offices are retained, but the new stair opening and arrival landing are now at the front. No furniture, operational doors, world placement or additional tick loops. Local Godot build, editor/physics and camera acceptance remain pending.

## PS1 shell clearance correction — 2026-10-28

The owner identified a blocked stair doorway/bottom step (two editor screenshots) and interior walls cutting into facade windows. This correction moves the first tread to Z=23, the stair-entry centre to Z=20.8 and the lower flight sideways to X=11, creating **2.2m of flat front approach** and **2.24m lateral width** from the lobby-facing doorway wall to the lower rail. The middle landing and return flight remain a genuine U configuration, and the guardrails belong to the upstairs visibility tree so they hide with UpperFloor in ground-level editor preview. The left public waiting-room partition now meets a solid pier between front windows, and the side-joining ground partitions have been moved off windows (west Z=-10.4, east Z=-4). Both floors use a fail-fast side-window-junction guard for perimeter-ending partitions, with a 0.5m clear edge margin. Named `StairBottomLanding` and `StairLobbyApproach` clearance zones preserve the flat apron for later furniture.

These new guards are structural source checks, unlike the existing furniture-only `StructureClearance.Allows`. See the updated [building authoring contract](../BUILDING_AUTHORING_README.md). **Validation still needed locally:** build, refresh the editor scene, walk/inspect the ground-floor stair door, the flat approach before the first tread, U-turn, upper exit and upper floor/roof previews; check all exterior window/partition junctions from the room side. No world placement or terrain changes included.
