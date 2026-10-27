# Structures — Buildings and permanent world objects

World-local **stationary** structures are owned here, independently of entities/robots and the player. Reusable original low-poly prefabs:
- [Building001/](Building001/README.md): rural maintenance depot with solid walls, large shutter and a walk-in room. Already placed in the active world by `WorldController`.
- [Building002/](Building002/README.md): forest ranger lodge with veranda, stove, reception and bunk/office rooms, doors and real window openings.
- [Building003/](Building003/README.md): rural fuel/service station with a shop interior, garage passage, rear exit, fuel pumps and flat canopy.

Building002 and Building003 are **self-contained prefabs only**; they have **not** been instanced in `WORLD/World.tscn`. Their placement is for a later Codex job when the owner requests it.

The new [Shared/C/StructureGeometry.cs](Shared/C/StructureGeometry.cs) holds only simple reusable box, roof, gable and collider-building methods for Building002/003; Building001 remains unchanged.\n\nFollow the project convention: `C/` is **only procedural C# mesh construction**; `Artwork/` is reserved for imported models; `Configuration/` holds typed settings and master resources. Item scene stays at its own folder root and can later be instanced into `WORLD/World.tscn` or an appropriate placement system. No world placement is performed just by adding the prefab.
