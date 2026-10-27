# Building002 — Forest ranger lodge

**Status:** Reusable procedural scene, now placed by WorldController in both the active top-down world and inherited FPS test. Source complete; local build/render/physics validation pending.

## Scene and silhouette

**Scene:** `res://WORLD/Content/Structures/Building002/Building002.tscn`

Approximate footprint **17m × 13m**, wall height **4m**, gable roof peak **6.6m** (plus chimney), with a broad covered timber veranda, posts, handrails, weathered wood siding, framed windows, stove chimney and ranger signage.

**Actual interior:** the 2m-wide front door is a real opening with no wall collider across the gap. There are two solid internal partition walls with 1.9–2.15m door gaps, forming a reception/common area, rear office and bunk/equipment room. Low-poly fittings: reception desk, wood stove, bunk and pillow, lockers and cabinet. Side/front window apertures have separate solid glass panes; the rear window glazing is decorative.

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
