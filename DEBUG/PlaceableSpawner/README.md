# F1 Placeable Spawner — GLB scene and size-master preview

Run `WORLD/World.tscn` (F6) or `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` (F6), press **F1**. Choose Bookcase001, Book001 or one of four Bed001 bedding palettes, then **Spawn selected model**, **Clear spawned**, or Close (F1).

- **Source:** Instantiates canonical `Bookcase001.tscn`, `Book001.tscn` and `Bed001.tscn`. Each is a visual wrapper around its imported GLB with its own `Configuration/<Asset>Master.tres` `SizeMultiplier`. Bed palettes override only the two bedding material surfaces of one GLB. Models are **not** built in C# at spawn.
- **Preview placement:** 3m ahead of the player along the horizontal camera direction, at player/terrain height. Bookcase and bed stand on their Y=0 pivot; Book001 hovers 0.95m. F1 distance/hover options remain in `Configuration/PlaceableSpawnMaster.tres`. Change the asset masters to resize each default preview, then respawn/reload to see the changed dimensions.
- **Lifetime:** Up to 24 instances under `WorldActorsRoot/PersistentPropsRoot/DebugSpawnedPlaceables`, removed by Clear or exiting the world. Nothing is saved to permanent structures, terrain or placement data.
- **No gameplay physics:** No collider, `StaticBody3D`, `RigidBody3D`, push/pickup/throw or inventory feature in these assets yet; actors can walk through the previews. IX implementation remains separate.
- Input only, no idle `_Process`/`_PhysicsProcess`. F1 releases FPS captured mouse; F1/Close restores it if captured, Esc closes without recapture. UI clicks do not fire guns.

## Local test / acceptance (pending)

1. Pull the development branch, import `Bed001.glb` in Godot .NET and run `dotnet build war-machine.csproj`.
2. F6 `WORLD/World.tscn` → F1. Spawn the original Bookcase001 and Book001 and four Bed001 colours. Check geometry appears, ground/hover placement and bed colours (same frame, different duvet/fold).
3. Edit `BedMaster.tres` to `SizeMultiplier = Vector3(1.2, 1, 1)`, reload/respawn and check bed is 20% wider. Repeat with book and bookcase masters; restore multipliers to 1. Individual scene override should be possible by copying a settings resource.
4. Check for GLB import/material-surface and missing-script warnings; ensure no stale `BedMesh` or `BedSettings` references, and visual props remain noncolliding. Test FPS F1 cursor/Close/Esc and preview clearing.

Remote source/GLB byte checks were performed; Godot/.NET build and in-game visual verification are deferred.
