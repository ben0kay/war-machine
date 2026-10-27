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
