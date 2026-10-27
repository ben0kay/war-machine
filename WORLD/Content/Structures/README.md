# Structures — Buildings and permanent world objects

World-local **stationary** structures are owned here, independently of entities/robots and the player. Reusable original low-poly prefabs:
- [Building001/](Building001/README.md): rural maintenance depot with solid walls, large shutter and a walk-in room. Already placed in the active world by `WorldController`.
- [Building002/](Building002/README.md): forest ranger lodge with veranda, stove, reception and bunk/office rooms, doors and real window openings.
- [Building003/](Building003/README.md): rural fuel/service station with a shop interior, garage passage, rear exit, fuel pumps and flat canopy.

All three buildings are now placed once by WorldController for both WORLD/World.tscn and the inherited DEBUG/TEST/FirstPerson test. Offsets live in WorldMaster; generated bounds reserve tree clearings and approaches. See WORLD/README.md for the layout.

The new [Shared/C/StructureGeometry.cs](Shared/C/StructureGeometry.cs) holds only simple reusable box, roof, gable and collider-building methods for Building002/003; Building001 remains unchanged.

Follow the project convention: `C/` is **only procedural C# mesh construction**; `Artwork/` is reserved for imported models; `Configuration/` holds typed settings and master resources. Item scene stays at its own folder root and can later be instanced into `WORLD/World.tscn` or an appropriate placement system. No world placement is performed just by adding the prefab.
