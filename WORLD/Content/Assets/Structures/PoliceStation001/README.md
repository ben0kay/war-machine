# PoliceStation001 — PS1 two-storey police-station shell

**Branch:** `implement-td0-td1` · **PS1.2 cleanup and lights:** implemented in source; local Godot preview, stair/wall-junction inspection and lighting performance checks pending.

**Shared future-building lessons:** [architectural design guide and PoliceStation001 failure register](../BUILDING_DESIGN_GUIDE.md) · [technical building authoring guide](../BUILDING_AUTHORING_README.md). Check both before repeating shell revisions.

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

## PS1.2 — wall/floor seam correction and ceiling fixtures (2026-10-28)

The owner identified lower-wall tops projecting through the upper slab and additional ugly wall stubs. Ground-floor **interior partitions** end at **Y = GroundFloorHeight - 0.26m**, just below the actual underside of the 0.24m upper deck. Upper-floor **interior partitions** stop 0.02m short of the roof underside. Exterior walls are handled separately (see PS1.3 below). Collision follows these shorter wall spans; upper floor walking height stays unchanged. All horizontal perimeter/hall partitions now trim their ends to the receiving wall's inner face (0.115m exterior / 0.09m hall). The main corridor stops at the near face of its lobby cross-wall. Waiting-room/stair-core runs stop at the inner facade face. Ground interview, records, upstairs office and bathroom segments have also been shortened or aligned to meet solid walls without projecting into adjacent rooms. Existing stair opening, landing and side-window guards are retained.

Basic lighting now uses [reusable IndoorCeilingLight](../../../../../ENVIRONMENT/Lighting/Fixtures/IndoorCeiling/README.md): a compact white-emissive panel for rooms, a 2.1m slim strip for corridors and one inexpensive unshadowed downward native spotlight per fixture. Six long fixtures on each corridor; several major rooms have centrally placed panels while some service rooms are intentionally unlit. `PoliceStationMaster.tres` inherits `CeilingLightsEnabled`, `CorridorLightsEnabled` and `RoomLightsEnabled` toggles; light energy/colour/range are controlled through the shared indoor fixture master. Ground lights hang just below the upper slab underside; upstairs lights hang just under the roof. Lights belong to their correct per-level `contents` visibility role and are created once in the editor or runtime, without per-frame logic or shadow maps. They do **not** use outdoor streetlamp scheduling/volumetric cones.

**Owner acceptance:** pull/build C#, refresh the standalone scene, look along all wall lines at ground and upper floors and inspect all facade/window/door junctions. Verify the ground walls do not poke through the upper slab with editor preview switched to UpperFloor; verify all fixture panels and real illumination at night, and compare performance with `CeilingLightsEnabled=false` for an A/B baseline. No live Godot build or playtest was run in this GitHub authoring session.

## PS1.3 — continuous exterior facade and floor slab seam (2026-10-28)

**Fix for owner screenshot:** the original PS1.2 trimming was incorrectly applied to the *entire* ground exterior shell, leaving a conspicuous **0.26m-wide-height horizontal gap** between the ground facade top and the upper exterior facade bottom. The floor-deck slab is **inset from the exterior wall**, so it does not fill that external gap. PS1.3 separates facade height from interior partition height:

- Ground-floor exterior walls and their matched collision terminate at **Y=H**, exactly where upper exterior walls start. No intervening facade band or unintended opening.
- Ground-floor **interior** walls stop at **Y=H−0.24−0.02m**, below the floor slab underside, preserving the fix for wall tops showing through the upstairs floor.
- Upper exterior walls meet the roof slab underside at **Y=H+UpperFloorHeight**, while upstairs interior walls keep their 0.02m clearance.
- The upper deck reaches the **inside face** of the exterior wall (0.115m inset from its centreline). It still has its real central stairwell opening and unchanged physical walking height.

**Validation:** switch editor preview to **Exterior**, inspect the entire 48×64m perimeter at the ground/upper seam and roof line, then inspect **UpperFloor** for interior partition tops. The seams have been checked mathematically against current authored dimensions; editor/Godot compilation and rendered appearance still require local validation. This change does **not** convert the police station's door placeholders into physical doors.

### Door implementation boundary

PoliceStation001 currently contains open, noncolliding visual placeholder leaves for exterior/office openings. The convenience store instead uses the reusable [Door001](../../Objects/Static/Doors/Door001/README.md) physics prefab (RigidBody3D + HingeJoint3D, with character push by physical contact) **only at its rear exit**. This mechanic is designed for reuse but has not been integrated into police-station openings. It must first pass local hinge/contact tests; each future installation requires compatible per-door widths/heights, hinge-side orientation, wall/frame collision, protected swing space and independent moving-body fade. Door conversion is not part of PS1.3.

## PS1.4 — upper stair opening and floating wall cleanup (2026-10-29)

Owner's four Godot screenshots exposed three related flaws. The second-floor slab was cut away over the **entire** large stair rectangle, including the solid walkway between the upper step and main upstairs floor; the front edge had no guardrail, while upstairs hallway walls ended with loose/nubbed tips.

**Source correction:** The upper floor's structural deck now fills the complete approach up to **Z=23**, where the upper flight meets the floor at Y=H. The remaining real stairwell opening is only X=8.7..18.2, Z=23..30.4. The previous separate overlapping UpperLanding box/collider is removed, preventing a double floor seam. Two short physical guard segments protect the newly defined south rim **outside** the 3.52m-wide upper stair exit; existing side and north guardrails still guard the open well. The top tread reaches the new deck edge and visual stair treads meet without 15mm step-to-step gaps. The opening remains unfilled over the actual flight and U-turn.

**Upstairs partitions:** both central hallway walls are trimmed to the connecting cross-wall at Z=18.5 instead of extending a 0.41m nub or ending unsupported. A cross-wall on the east side joins the corridor to the exterior with a real 3.6m doorway at X=15.3, giving the stair landing an accessible route into the east offices and down the main corridor. Existing west room divider at Z=18.5 still joins the west hall. Wall geometry and collision are generated together; no new runtime tick, furnished room, active door, world placement or change to the ground-floor lobby.

**Acceptance:** refresh `PoliceStation001.tscn` in **UpperFloor** editor preview. View from above and at walking height; the stair exit must have solid decking all the way to the open upper lobby, and barriers cannot obstruct movement at the upper tread. Inspect the west/east upstairs hall termini and vestibule doorway, then walk both stair flights after separately authorizing world placement. Preview geometry/physics still need local Godot acceptance; static checks do not prove visual perfection.
