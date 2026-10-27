# Structures — Buildings and permanent world objects

World-local **stationary** structures are owned here, independently of entities/robots and the player. First reusable proof: [Building001/](Building001/README.md), a substantial low-poly abandoned maintenance depot with roof, doors, windows and static collision.

Follow the project convention: `C/` is **only procedural C# mesh construction**; `Artwork/` is reserved for imported models; `Configuration/` holds typed settings and master resources. Item scene stays at its own folder root and can later be instanced into `WORLD/World.tscn` or an appropriate placement system. No world placement is performed just by adding the prefab.
