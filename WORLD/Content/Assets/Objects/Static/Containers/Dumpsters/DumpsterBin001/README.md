# DumpsterBin001 — giant dark-green industrial dumpster

**War Machine original static environment asset, source authored 2026-10-28; Godot import and gameplay testing pending.** **3.48m wide × 2.55m tall × 1.91m deep** including external details — roughly **5× the width and 2.5× the height** of the small WheelieBin001. Industrial dumpster, not a scaled-up household wheelie bin.

## Visual design

- Heavy, gently tapered dark **forest-green metal chassis** with folded top rim, vertical side/front reinforcing ribs, inset front panel, front/lower bumper brace, metal skids and black forklift-lift pockets/frames. Small worn metal edge highlights, no graffiti or written labels. **No wheels**.
- **Two separate matte-black steel lids**, with raised moulded ribs, front grab lips and a seam down the middle. `LeftLidHinge` and `RightLidHinge` are named GLB transform pivots at the rear, ready for a *future* interaction pass. Current lids are closed and purely visual. Both lid pivots reference **shared geometry** but retain independent transform nodes. No lid open input/physics or simulated contents.
- [`Artwork/DumpsterBin001.glb`](Artwork/DumpsterBin001.glb) is an original self-contained binary glTF 2.0: **1,140 unique mesh triangles (1,356 drawn across both lids), 9 shared mesh resources / 11 mesh nodes, 8 opaque PBR materials, ~69 KiB**. No texture files, animation callbacks or external resource dependencies. The green body and black lids reuse WheelieBin001 base colour values. Details of mesh groups, material names and pivots: [Artwork README](Artwork/README.md).

## Godot integration

- [`DumpsterBin001.tscn`](DumpsterBin001.tscn) is the canonical `DumpsterBin001 → Model → Artwork` prefab using shared `PlaceableVisual.cs`, **no per-asset C#**, no mesh regeneration. A standard relative-size [`Configuration/DumpsterBinMaster.tres`](Configuration/DumpsterBinMaster.tres) supplies `SizeMultiplier = Vector3(1,1,1)`. Scale X width / Y height / Z depth; duplicate/override this resource for a differently sized instance and reinstance for collision updates.
- **SOC0: `CollisionMode = 1 AutoBox`**, because this is a massive, fixed closed dumpster. The existing `StaticObjectCollision.cs` creates **one** `StaticBody3D` / `BoxShape3D` on world physics layer 1 at runtime, derived from the entire scaled imported bounds. This intentionally blocks walking into the closed steel container or through its decorative forklift niches. No `RigidBody3D`, gravity, wheels or dynamic lid colliders.
- The automatic **F1 catalogue** will discover it as **OBJECT · Containers / Dumpsters → DumpsterBin001**, with a scene-specific **7m minimum preview distance** to avoid spawning its large envelope on top of the player (usual menu default is 3m). No F1 code changes or manual registration.
- The GLB is **not added** to any house/store/site or procedural scatter; instance the canonical prefab manually when you later authorize placement. No edits to terrain, structures, player or zombies.

## Validation

After pulling `implement-td0-td1`, let Godot import the GLB; open the prefab and inspect both black lids, panel thickness, rear hinge line, opaque green paint, floor skids and mesh normals from all sides. Run `dotnet build war-machine.csproj` where available; play `WORLD/World.tscn` / `WORLD/FirstPerson/FirstPersonWorld.tscn`, press F1 and spawn **Containers / Dumpsters / DumpsterBin001**. Verify the 7m preview offset, 5×-wide visual scale relative to WheelieBin001, foot contact, FPS/top-down appearance, solid AutoBox blocking, weapon ray obstruction and clearing the temporary preview. Resize an instance through `SizeMultiplier` and reinstance. Godot in-engine testing has **not** been performed here.

**Future only if requested:** operable lids with updated lid collision, waste loot, movable dumpster or destructibility. Any moving / interactive part needs a separate physics design; the present static AutoBox does not magically adapt to lid rotation.
