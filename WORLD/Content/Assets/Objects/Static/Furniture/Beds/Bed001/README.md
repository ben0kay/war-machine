> **SOC0 collision enabled (2026-10-28):** Bed001 now creates one static auto-bounds BoxShape3D during runtime instancing, scaled with `SizeMultiplier`. F1 bed palette previews are solid; bed/under-bed volume is filled by the first-pass bounding box. A future interactive or under-bed clearance pass can use CustomBoxes; no rigid-body sleeping or inventory system was added. Godot validation pending.

# Bed001 — imported GLB with four bedding variations

**Status:** GLB scene conversion complete in source; **local Godot import, C# build and visual validation pending**. No physical collision, pickup, bed/sleep interaction or permanent world placement.

**Files:** [Bed001.tscn](Bed001.tscn) wraps [Artwork/Bed001.glb](Artwork/Bed001.glb) inside `Model/Artwork`. The old `C/BedMesh.cs` and `Configuration/BedSettings.cs` geometry-generation approach has been removed. The scene-root script [BedVisual.cs](BedVisual.cs) only applies **shared size settings and per-instance bedding material changes**, not geometry generation.

**Size:** Native GLB approx. **1.32m W × 1.183m H × 2.125m L**, Y=0 ground pivot, head -Z. Edit [Configuration/BedMaster.tres](Configuration/BedMaster.tres) `SizeMultiplier = Vector3(1, 1, 1)`; X adjusts width, Y height, Z length. For example `Vector3(1.1, 1, 1)` is 10% wider. All default instances and F1 previews share this master; individual overrides may duplicate the resource.

**BeddingVariant:** SlateBlue, MossGreen, WarmSand, DustyRose; all share the **same GLB**, frame and pillows. `BedVisual.SetBeddingVariant(...)` changes only the two duvet/fold material surfaces (indices 5 and 6). Geometry, imported meshes and original material resources are not cloned or modified.

**Performance:** 288 triangles in seven material groups, no per-frame work, no physics bodies. Slightly more draw calls than one material but far fewer geometry nodes than the old code-generated version. Asset fits reusable furnishing of houses, cabins, lodge and motel rooms. Future collision needs separately designed frame/bed shapes and careful handling of nonuniform scaling.

**Local check:** `dotnet build war-machine.csproj`; import GLB in Godot, open `Bed001.tscn` and verify 7 material surfaces and correct form. F6 `WORLD/World.tscn` → F1 → spawn all 4 palettes side by side, verify only bedding differs. Adjust scale master and reload/respawn to inspect. Test old Bookcase001/Book001 previews still work. No runtime 3D validation was run remotely.
