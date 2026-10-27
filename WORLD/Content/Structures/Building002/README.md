# Building002 — Forest ranger lodge

**Status:** Complete standalone procedural scene source; not yet placed in the active world. `[Tool]` generates temporary low-poly art from Godot C#, and the building has native 3D collision. This prefab is distinct from the earlier industrial depot.

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

## Handoff to Codex

This commit **does not** instance Building002 in `WORLD/World.tscn`. When authorized, load this PackedScene, choose a non-overlapping, fairly flat spot, sample terrain height once for its root transform, reserve enough clearance for the veranda and keep it away from the robot path and zombie starts. Avoid writing native terrain or modifying the parked FPS scene.

Build the Godot C# project, open this scene for the mesh preview and perform brief walk-through/collision and roof-toggle checks. No Godot/.NET runtime was available when authoring, so visuals/build/physics have **not** been validated locally.
