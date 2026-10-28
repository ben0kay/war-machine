# Shared GLB placeable visuals and size masters

Every catalogued GLB placeable has its own `Configuration/<Asset>Master.tres` with `SizeMultiplier = Vector3(1, 1, 1)`. X controls model width, Y height/thickness and Z depth/length as a **relative multiplier**, not as a size in metres. `Vector3(1.2, 1, 1)` is 20% wider; `Vector3(1, 0.85, 1)` is 15% lower. The originals stay at their authored dimensions when all multipliers are 1.

`PlaceableVisual.cs` is a lightweight scene-root adapter that applies the size master **once** to the `Model` container. The GLB remains at `Model/Artwork`; no geometry or shared material resource is rebuilt. Changing a master and respawning/reloading affects **all instances using it**, including F1 previews. To give just one individual item a different size, duplicate/override the settings resource on that specific scene instance.

Bed001 uses `BedVisual.cs`, which derives from the shared scale adapter and separately recolours its two bedding material surfaces. No C# procedural bed geometry remains.

**Physics caveat:** These three scenes have no collision yet. If physics bodies are added later, plan appropriately sized colliders; nonuniform physics scaling may require explicitly rebuilt collision rather than reusing the visual-only transform.

**Performance:** These components do their work in `_Ready` (or intentional explicit palette changes), call `SetProcess(false)` and `SetPhysicsProcess(false)`, and do not create timers, new GLB meshes, awake physics bodies or per-object polling.
