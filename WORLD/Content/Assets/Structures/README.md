# Structures — Buildings and permanent world objects

World-local **stationary** structures are owned here, independently of entities/robots and the player. Reusable original low-poly prefabs:
- [Building001/](Building001/README.md): rural maintenance depot with solid walls, large shutter and a walk-in room. Already placed in the active world by `WorldController`.
- [Building002/](Building002/README.md): forest ranger lodge with veranda, stove, reception and bunk/office rooms, doors and real window openings.
- [Building003/](Building003/README.md): rural fuel/service station with a shop interior, garage passage, rear exit, fuel pumps and flat canopy.
- [Building004/](Building004/README.md): two-storey field office with flat parapet roof, balcony, crosswise internal stair and true upper-floor opening; uses the same authoring contract as Building002.

- [PoliceStation001/](PoliceStation001/README.md): standalone **48 × 64m, two-storey police-station shell**, with [Tool] editor floor-preview selector, stairwell/collision, actual windows/openings and shared structure metadata; **not placed in the active world** (PS0 source implemented, awaiting local validation).

**Independent reusable prefab:** [ConvenienceStore001/](ConvenienceStore001/README.md) is now a complete store-only procedural shell with big unglazed windows, shelving, till counter, stockroom and back door. Shared structure visibility is wired, and the prefab is composed into the convenience-store site in the active village.

All four buildings are now placed once by WorldController for both WORLD/World.tscn and the inherited WORLD/FirstPerson test. Offsets live in WorldMaster; generated bounds reserve tree clearings and approaches. See WORLD/README.md for the layout.

The new [Shared/C/StructureGeometry.cs](Shared/C/StructureGeometry.cs) holds only simple reusable box, roof, gable and collider-building methods for Building002/003; Building001 remains unchanged.

Follow the project convention: `C/` is **only procedural C# mesh construction**; `Artwork/` is reserved for imported models; `Configuration/` holds typed settings and master resources. Item scene stays at its own folder root and can later be instanced into `WORLD/World.tscn` or an appropriate placement system. No world placement is performed just by adding the prefab.

## New building authoring

**Both building guides are required:** [BUILDING_DESIGN_GUIDE.md](BUILDING_DESIGN_GUIDE.md) **first** (floorplan, safe stairs, windows, seamless floors, wall junctions and PoliceStation001 failure register), then [BUILDING_AUTHORING_README.md](BUILDING_AUTHORING_README.md) (C#/Godot scenes, preview, collision, haze footprints, top-down fade, FPS presentation, targeting, streaming and performance). Use ConvenienceStore001 for the single-level example and Building002/004 for multi-level references. A new prefab is never automatically placed in the active world.
