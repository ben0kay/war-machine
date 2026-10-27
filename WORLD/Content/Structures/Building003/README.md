# Building003 — Abandoned roadside fuel/service station

**Status:** Reusable procedural scene, now placed by WorldController in both the active top-down world and inherited FPS test. Source complete; local build/render/physics validation pending.

## Scene and silhouette

**Scene:** `res://WORLD/Content/Structures/Building003/Building003.tscn`

Main shop/service hall: about **22m × 12m**, **4.4m** walls, flat industrial roof, parapet and air conditioner. A tall **fuel island canopy** reaches forward of the main footprint, supported by four columns. Two detailed gas pumps, hoses/nozzles, service lane, price sign and weathered shop sign give a recognizable Earth-like roadside silhouette.

**Actual interior:** accessible front customer door **2.1m wide** and a separate open **rear service exit**. A large service shutter is deliberately closed/collidable, but an internal passage through the dividing wall connects the store and garage. The shop contains two shelving aisles, a counter/cash register and refrigerated cabinet; the garage has a workbench/tools. Shopfront and side window holes are physically open in the wall, with separate colliding panes. Main roof and pump canopy can both be hidden visually from the angled camera.

**Collision:** true static colliders on layer 1 for outside walls, glass, shutter, partition, fixed shelves/counter, workbench, canopy posts, pump bodies and roadside sign. Thin decorative floor and forecourt overlays remain non-colliding, with Terrain3D handling walkable ground. **Place on level ground with clearance forward of the +Z entrance**: the pump canopy extends about 19m from the building centre; do not intersect trees or robot waypoints.

## Source ownership

- `C/Building003Mesh.cs`: one-shot `[Tool]` C# generator, no per-frame logic.
- `Configuration/Building003Settings.cs`, `Configuration/Building003Master.tres`: adjustable size and muted colours.
- `Artwork/`: optional future imported models.
- `../Shared/C/StructureGeometry.cs`: shared Godot primitive mesh/collision construction.
- `Building003.tscn`: scene root with `ShowRoof` and existing `CameraOccluder` child.

`SetRoofVisible(false)` hides the flat roof and pump canopy for an interior view, but leaves collision in place. This prefab is a **visual/physics test**, not a fuel/refueling system. No dynamic lights, NPCs, loot, doors opening, interaction, stock simulation, or save logic.

## Active placement

WorldController instances this prefab on saved terrain in both WORLD/World.tscn and DEBUG/TEST/FirstPerson/FirstPersonTest.tscn. Its offset is editable in WORLD/Configuration/WorldMaster.tres. Actual generated bounds plus approach margins exclude pines, including external porch/canopy geometry. Terrain is read-only and the parked legacy FPS world is unchanged. See WORLD/README.md for the shared layout.

Godot/.NET are unavailable here; compile, grounding, interior access, collider and camera checks remain for local validation.
