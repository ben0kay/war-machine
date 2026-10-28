# PineTree001 — comparison GLB for procedural pine trees

**Status:** GLB visual asset + reusable `.tscn` + size master created; **awaiting local Godot import and appearance validation**. Classified under **Static/Vegetation/Trees**, meaning a non-liftable authored world object. Folder classification does **not** add collision.

## What to expect

This is an original imported **tall evergreen pine** model, with an uneven crown of dark/medium/lighter needle bunches around a visible brown tapered trunk and staggered woody branch tiers. It is intended to compare **authored GLB geometry** against the game's existing procedural pines, not to replace those pines yet.

- Artwork: [PineTree001.glb](Artwork/PineTree001.glb).
- Native bounds: **~7.97m wide × 14.37m tall × 8.10m deep**; root at ground Y≈0, +Y up; no required 'front' direction.
- Geometry: **7,679 triangles, five opaque PBR material surfaces**, no bitmap textures, 60 branch stems/171 leaf tufts baked into one imported mesh; no per-leaf nodes or runtime generation.
- Scene: [PineTree001.tscn](PineTree001.tscn), a shared `PlaceableVisual` wrapper. 
- Config: [Configuration/PineTreeMaster.tres](Configuration/PineTreeMaster.tres) with shared `SizeMultiplier = Vector3(1, 1, 1)`. Adjust X width, Y height and Z depth as **relative scale**; respawn/reload after editing. For proportional trees use all three equal.

## How to view without replacing existing forest

Pull development branch, import the GLB in Godot .NET, run **F6 `WORLD/World.tscn`**, press **F1**, select **OBJECT · PineTree001 · Imported GLB** and spawn. The preview is placed **at least 12m ahead** to accommodate its ~8m canopy diameter; it can appear alongside the existing world pines.

Previewed tree is visual-only and noncollidable. This has **no effect** on `VEGETATION/Trees/Pine/PinePlacement.cs`, existing `PineMeshBuilder`, `PineMaster.tres`, tree terrain placement, active trunk collisions, wind, LOD, fade or density.

## Before adopting it across the whole forest

The current active pines use seeded `MultiMesh` batches, shader wind, low-detail far LOD, view fading and chunk-level trunk physics. **None of those mechanics is wired to this GLB yet**. This 7.7K-triangle model is for side-by-side art inspection; exporting a low/medium LOD, checking needle silhouette in top-down/FPS, attaching compatible foliage wind/fade shading and testing instanced draw costs would be required before world-scale replacement. Avoid creating individual Node3D trees in forest placement.

**Local validation pending:** `dotnet build war-machine.csproj`, Godot GLB import, open `PineTree001.tscn`, F6 World + F1 preview; check normals, trunk, branches, green colours, height, terrain grounding, no collision and current procedural pines unchanged. GLB header/mesh/surface/index structure was validated while authoring, not tested inside Godot.

## WS1 distance policy (2026-10-28)

F1 instances under PersistentPropsRoot now automatically receive the shared conservative fog-aware native visibility range, like other GLB/procedural static objects. Imported mesh LOD remains enabled by normal Godot import defaults; inspect actual generated LODs locally. This does not replace the active forest, add wind/collision to this visual-only prefab or create manual far-tree artwork. All earlier forest-replacement constraints still apply. See WORLD/Rendering/README.md for coverage and validation limits.

**F1 dynamic preview (2026-10-28):** This canonical scene has `metadata/f1_preview_min_distance = 12.0`, preserving the old 12m minimum preview spacing for wide pine crowns. It is automatically listed from its canonical wrapper; no special-case PineTree001 branch in F1. No changes to procedural forest trees.
