# F1 Placeable Spawner — visual catalogue inspection only

Run `WORLD/World.tscn` (F6) or `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` (F6) and press **F1**. The isolated, right-side menu has **OBJECT Bookcase001**, **PROP Book001**, **four OBJECT Bed001 bedding colour variants**, **Spawn selected model**, **Clear spawned**, and **Close (F1)**.

- The menu uses the **actual imported GLB artwork and procedural Bed001 scene** under `WORLD/Content/Placeables/`; it does not create or modify gameplay prefabs. Preview instances live in `WorldActorsRoot/PersistentPropsRoot/DebugSpawnedPlaceables`, and disappear on scene exit or when Clear spawned is clicked. Max **24** live previews.
- Spawned models appear **3m ahead along the horizontal active-camera view direction**, at player standing elevation or sampled Terrain3D surface height (whichever is higher). The **bookcase and Bed001 stand on the ground** using its Y=0 base pivot. The small **book hovers 0.95m** above that height so its artwork can be inspected. Alter distances in `Configuration/PlaceableSpawnMaster.tres`.
- **No physics/collision:** the GLBs are visual Node3D instances, not StaticBody3D or RigidBody3D. They cannot fall, push, settle, be hit, or be picked up. The player and enemies can pass through them. Real collision/physics are later IX1/IX2 tasks.
- The debug code is self-contained under `DEBUG/PlaceableSpawner/`; world composition merely loads and initializes it for both camera modes. Its full-screen GUI backdrop prevents UI clicks from also firing a gun/recapturing FPS look. F1 releases captured FPS mouse while the menu is open; F1/Close restores capture if it was captured previously, while **Esc** closes the menu without recapturing (click outside to recapture).
- Everything is input-driven: no `_Process` or `_PhysicsProcess` on the menu or generated visual meshes. GLBs/scenes load only when spawned; terrain height is sampled only on a click. No changes to saved terrain, live building geometry or IX phases.

## Local test / acceptance (pending)

1. In Godot .NET import the GLB files and build Bed001's C# class and run `dotnet build war-machine.csproj`.
2. F6 `WORLD/World.tscn`; press F1, spawn the bookcase and book, then each of the four Bed001 variants side-by-side. Verify identical bed geometry with different duvet/trim colours, correct grounding, visible models, and walk-through noncollision. Clear, reopen and spawn again. They should remain fixed even when bumped.
3. F6 FPS scene; verify F1 releases mouse, buttons operate without firing, F1 or Close recaptures appropriately and Esc leaves mouse released.
4. Confirm no errors loading imported models, no extra idle callbacks, no terrain rebake or runtime saved changes.

Only GitHub source/scene review was performed here. Godot compilation, GLB import and procedural Bed001 render and FPS mouse behavior require local validation. This is **not** implementation/acceptance of IX0–IX2.
