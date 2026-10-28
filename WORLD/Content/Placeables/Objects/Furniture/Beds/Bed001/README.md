# Bed001 — reusable single bed, Phase 1 material variants

**Status:** Visual-only reusable Godot C# asset; **awaiting local Godot/.NET compile and visual validation**. No physical collision, rigid body, pickup, bed/sleep interaction or automatic world placement.

## What exists

- `Bed001.tscn`: stable **Object** prefab with `C/BedMesh.cs` visual geometry, no new continuous processing.
- `Configuration/BedSettings.cs` + `BedMaster.tres`: reusable nominal **1.25m wide × 2.10m long** single-bed footprint, configurable common dimensions; 0m ground pivot, headboard at **-Z**, foot at **+Z**.
- Low-poly worn timber frame, four feet, open under-bed gap, headboard, mattress, light fitted sheet, two separate cream pillows, duvet, turn-over hem and small fold lines.
- **One mesh layout, four selected palettes** via the exported `BedMesh.BeddingVariant` enum: **SlateBlue** (default), **MossGreen**, **WarmSand**, **DustyRose**. Only duvet and folded trim change. All other geometry/materials remain identical.
- Shared Godot primitive box mesh and shared matte `StandardMaterial3D` resources. No per-variant duplicated model files, image textures or material cloning; instances choose existing colour materials.
- `Artwork/` is reserved for a later imported 3D model that could replace the generated art without changing this asset's identity.

## How to use

In Godot, instance `Bed001.tscn` under a building's floor `contents` visual subtree, then set the scene root's **BeddingVariant** enum to SlateBlue, MossGreen, WarmSand or DustyRose. `SetBeddingVariant(...)` is available for explicit runtime changes. In the F1 **Placeable Spawner**, choose one of the four Bed001 entries and spawn adjacent samples to compare them. This is debug visual placement, not permanent world furnishing.

The bed **has no collision yet**: actors pass through it in F1 preview and when manually instanced. In a later authorized gameplay phase, introduce a reusable fixed `StaticBody3D` and sensibly shaped frame/mattress colliders without enclosing the open under-bed area; then consider possible sleep/interaction as a separate feature.

## Performance and validation

Geometry is built once per scene instance (or deliberate palette change); the prefab turns off idle/physics processing immediately. Twenty-plus small mesh parts mean a modest draw-call cost per bed, appropriate for a few furnished rooms; if adding hundreds in view, consider batching material surfaces or a future authored GLB. No rigid physics, timers or per-frame palette updates.

**Local checks pending:** `dotnet build war-machine.csproj`; open `Bed001.tscn` in Godot; F6 `WORLD/World.tscn`, press F1 and spawn each bedding colour; check headboard, full silhouette, duvet coverage, scale, ground placement and no collision; verify palette switching affects only cloth and does not change mesh layout. Recheck F1 book and bookcase previews. Repeat in FPS debug scene if desired.
