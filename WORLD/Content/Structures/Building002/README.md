# Building002 — Forest ranger lodge

**Status:** Reusable procedural scene, now placed by WorldController in both the active top-down world and inherited FPS test. Source complete; local build/render/physics validation pending.

## Two-storey lodge and stairs (new)

Building002 now has **two usable storeys in the same 17m × 13m default footprint**. The pre-existing wall height (4.0m) becomes the ground-to-upper floor elevation. Upper wall height defaults to 2.8m, lifting the gable roof base to 6.8m and roof ridge to ~9.4m. Roof/chimney move upward; no new Terrain3D bake or additional building placement.

The ground-storey reception, office, equipment room and porch are retained. Inside, a **straight 22-tread timber stair** rises from the reception/common area to an upstairs landing. There is a **genuine missing floor section above the stairwell**, bounded by upstairs safety railings. Decorated upstairs rooms include a furnished sleeping room and a small desk/supply area; upper facades have actual window apertures and matching glazing.

**Stair collision:** the visible individual treads/risers are cosmetic. A single aligned, invisible **inclined BoxShape3D** provides a walkable slope beneath them, avoiding the CharacterBody3D problem of colliding with each tiny step riser. Both deck segments and windows/walls have their own static collision. Confirm the final walking feel in Godot, particularly cresting the top step and reversing downstairs.

**Generated hierarchy and tagging** (ordinary runtime/generated nodes, not separately authored scene roots):

```text
Building002
├── CameraOccluder                           (existing whole-building occlusion proxy)
└── Generated
    ├── Level_0                              floor_level=0, floor_elevation=0
    │   ├── Visuals
    │   │   ├── Exterior
    │   │   ├── TimberAndProps
    │   │   ├── Rooms
    │   │   ├── ShowcaseExterior
    │   │   └── ShowcaseInterior
    │   └── Collision                         ground walls, furniture, porch supports
    ├── Level_1                              floor_level=1, floor_elevation=WallHeight
    │   ├── Visuals
    │   │   ├── RoofVisuals
    │   │   ├── FloorDeck                     four spans around open stairwell
    │   │   ├── ExteriorWalls
    │   │   ├── Furnishings
    │   │   └── Partitions
    │   └── Collision                         upper deck/walls/windows/fixtures/roof
    └── Stairs_0_to_1                         connects_floor_from=0, connects_floor_to=1
        ├── Visuals                          treads, risers, side handrails
        └── Collision                        smooth ramp + handrail shapes
```

Both level roots have Godot groups `building_floor_0` and `building_floor_1` for discovery, plus metadata `floor_level` and `floor_elevation` (local metres above building origin). Stair root uses metadata `lower_elevation`, `upper_elevation`, `connects_floor_from` and `connects_floor_to`, plus `building_stairs` group. These tags describe each floor's **geometry ownership**, not an automatic player floor-state detector.

`SetFloorOpacity(int floor, float opacity)` is a **manual visual-only hook** (0=transparent, 1=opaque) for future floor-aware camera logic. It changes only meshes under that floor's `Visuals`; collision under either `Collision` remains enabled. `ShowRoof`/`SetRoofVisible` remain independent of floor visual opacity. **Automatic floor detection/fading is not implemented in this geometry pass.**

Read [FLOOR_VISIBILITY_PLAN.md](FLOOR_VISIBILITY_PLAN.md) before building the camera integration. The existing `CAMERA/Occlusion/CameraOccluder.cs` still fades the **entire building**; a future floor controller must coordinate those fades rather than letting two independent transparency writers fight. Glass physics and line-of-sight classification are a separate consideration from floor cosmetic fade.

**Local validation required:** pull, compile C#, F6 `WORLD/World.tscn`, approach Building002 northwest of spawn, walk up/down stairs and around upstairs furniture in top-down and inherited FPS test. In the remote tree, inspect `Generated/Level_0`, `Level_1` and `Stairs_0_to_1`. Test camera occlusion separately; no automatic floor fade yet.

## Scene and silhouette

**Scene:** `res://WORLD/Content/Structures/Building002/Building002.tscn`

Approximate footprint **17m × 13m**, ground wall height **4m**, upper wall height **2.8m**, gable roof peak **~9.4m** (plus chimney), with a broad covered timber veranda, posts, handrails, weathered wood siding, framed windows, stove chimney and ranger signage.

**Ground-floor interior:** the 2m-wide front door is a real opening with no wall collider across the gap. There are two solid internal partition walls with 1.9–2.15m door gaps, forming a reception/common area, rear office and bunk/equipment room. Low-poly fittings: reception desk, wood stove, bunk and pillow, lockers and cabinet. Side/front window apertures have separate solid glass panes; the rear window glazing is decorative.

**Placement:** centre origin, base at `Y=0`, front entrance faces **+Z**. Place onto reasonably level Terrain3D, leaving the veranda approach clear. Decorative slab and veranda planks use existing terrain for floor collision; walls, furniture, window panes, porch support columns and main sloped roof have static colliders on physics layer 1.

## Source ownership

- `C/Building002Mesh.cs`: **C# mesh-construction script**, `[Tool]` editor-time preview after Godot .NET compilation; no idle processors.
- `Configuration/Building002Settings.cs`: settings resource class with footprint, roof rise and colours.
- `Configuration/Building002Master.tres`: inspector-editable defaults.
- `Artwork/`: future imported `.glb` or texture assets; none supplied.
- Shared primitive routines live in `../Shared/C/StructureGeometry.cs`.
- `Building002.tscn` also includes the existing `CAMERA/Occlusion/CameraOccluder.cs` scene child, just like Building001.

`ShowRoof=false` (or `SetRoofVisible(false)` at runtime) hides the roof and veranda canopy for testing the interior, while leaving their solid physics unchanged. The existing camera-fading component applies to the building as one object; no automatic player-indoor detection is included.

## Active placement

WorldController instances this prefab on saved terrain in both WORLD/World.tscn and DEBUG/TEST/FirstPerson/FirstPersonTest.tscn. Its offset is editable in WORLD/Configuration/WorldMaster.tres. Actual generated bounds plus approach margins exclude pines, including external porch/canopy geometry. Terrain is read-only and the parked legacy FPS world is unchanged. See WORLD/README.md for the shared layout.

Godot/.NET are unavailable here; compile, grounding, interior access, collider and camera checks remain for local validation.

## Enhanced detail showcase — additive pass

Building002 now generates extra low-poly C# detail **within its original footprint**. The existing shell, veranda, rooms, openings and base collision are retained.

- **Exterior:** horizontal timber weatherboard accents, subtle green weathering/moss geometry, porch trail noticeboard with paper lines, utility meter/conduit, two benches, small crate, doormat and **small potted plants** (no trees).
- **Interior:** ranger wall map with markings, desk paperwork and pencils, tabletop radio with antenna, office wall shelves and manuals, cupboard fascia, bunk blankets, bedside lantern and a wall clock.
- **Approach:** these are modeled geometry details rather than screen-space decals or textures; no new shaders, physics simulations, lights, AI or per-frame processing. Major furniture/porch post collision remains; lightweight dressing is mostly cosmetic, to avoid excessive collision shapes.
- **Layering:** `Generated/ShowcaseExterior` and `Generated/ShowcaseInterior` are grouped separately so a later pass can refine or replace the dressing without reconstructing the building shell. The entire enhancement lives in `C/Building002Mesh.cs` and can be used in both the top-down world and the inherited FPS preview.

This is a practical example of developing a prop in layers: **shell → usable rooms → furnishings → material/architectural dressing → small storytelling props**. It does not create an imported model or a complete loot/interaction system. Build and view locally to judge densities, placement and collision; authoring here did not include a Godot render test.

### Doorway visibility correction

The interior ranger-map prop is now mounted wholly on the solid front-wall section between the left window and the entrance; it no longer overlaps the open door. The added clapboard/weatherboard strips are split around the pedestrian doorway **at all heights below its header**, and around window openings, so no decorative horizontal beam cuts through an opening. These are purely visual changes—building footprint, solid walls, door collision and room layout are unchanged. Verify after a Godot scene reload/F6.
