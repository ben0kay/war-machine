# PineTree001 — comparison GLB for procedural pine trees

**Status:** GLB visual asset + reusable `.tscn` + size master created; **awaiting local Godot import and appearance validation**. Classified under **Static/Vegetation/Trees**, meaning a non-liftable authored world object. Folder classification does **not** add collision.

## What to expect

This is an original imported **tall evergreen pine** model, with an uneven crown of dark/medium/lighter needle bunches around a visible brown tapered trunk and staggered woody branch tiers. It is intended to compare **authored GLB geometry** against the game's existing procedural pines, not to replace those pines yet.

- Artwork: [PineTree001.glb](Artwork/PineTree001.glb).
- Native bounds: **~7.97m wide × 14.37m tall × 8.10m deep**; root at ground Y≈0, +Y up; no required 'front' direction.
- Geometry: **5,969 triangles, five opaque PBR material surfaces**, no bitmap textures, 60 branch stems/171 leaf tufts baked into one imported mesh; no per-leaf nodes or runtime generation.
- Scene: [PineTree001.tscn](PineTree001.tscn), a shared `PlaceableVisual` wrapper. 
- Config: [Configuration/PineTreeMaster.tres](Configuration/PineTreeMaster.tres) with shared `SizeMultiplier = Vector3(1, 1, 1)`. Adjust X width, Y height and Z depth as **relative scale**; respawn/reload after editing. For proportional trees use all three equal.

## How to view without replacing existing forest

Pull development branch, import the GLB in Godot .NET, run **F6 `WORLD/World.tscn`**, press **F1**, select **OBJECT · PineTree001 · Imported GLB** and spawn. The preview is placed **at least 12m ahead** to accommodate its ~8m canopy diameter; it can appear alongside the existing world pines.

Previewed tree is visual-only and noncollidable. This has **no effect** on `VEGETATION/Trees/Pine/PinePlacement.cs`, existing `PineMeshBuilder`, `PineMaster.tres`, tree terrain placement, active trunk collisions, wind, LOD, fade or density.

## Before adopting it across the whole forest

The current active pines use seeded `MultiMesh` batches, shader wind, low-detail far LOD, view fading and chunk-level trunk physics. **None of those mechanics is wired to this GLB yet**. This ~6K-triangle model is for side-by-side art inspection; exporting a low/medium LOD, checking needle silhouette in top-down/FPS, attaching compatible foliage wind/fade shading and testing instanced draw costs would be required before world-scale replacement. Avoid creating individual Node3D trees in forest placement.

**Local validation pending:** `dotnet build war-machine.csproj`, Godot GLB import, open `PineTree001.tscn`, F6 World + F1 preview; check normals, trunk, branches, green colours, height, terrain grounding, no collision and current procedural pines unchanged. GLB header/mesh/surface/index structure was validated while authoring, not tested inside Godot.

## WS1 distance policy (2026-10-28)

F1 instances under PersistentPropsRoot now automatically receive the shared conservative fog-aware native visibility range, like other GLB/procedural static objects. Imported mesh LOD remains enabled by normal Godot import defaults; inspect actual generated LODs locally. This does not replace the active forest, add wind/collision to this visual-only prefab or create manual far-tree artwork. All earlier forest-replacement constraints still apply. See WORLD/Rendering/README.md for coverage and validation limits.

**F1 dynamic preview (2026-10-28):** This canonical scene has `metadata/f1_preview_min_distance = 12.0`, preserving the old 12m minimum preview spacing for wide pine crowns. It is automatically listed from its canonical wrapper; no special-case PineTree001 branch in F1. No changes to procedural forest trees.

## WORLD GLB forest comparison (2026-10-28)

The world pine spawner can **now** use the actual PineTree001 GLB as its source geometry; this is an **opt-in-by-master/default-on visual comparison**, not permanent deletion of procedural trees. `VEGETATION/Trees/Pine/Configuration/PineMaster.tres` has `UsePineGlb=true` and references `Artwork/PineTree001.glb`. `PinePlacement` extracts the combined mesh once and passes it through its existing **seeded Terrain3D placement, MultiMesh chunk streamer, per-tree scale/yaw and exclusions**. It does not instance this whole catalogue scene on every tree, and the F1 scene/master remain independently usable.

Imported foliage gets 3 shared shader materials for the existing opaque camera fade and subtle height-based sway. Bark remains original PBR. Current chunk trunk collision is preserved, using the GLB's base radius. Render distance, chunk lifecycle, world section culling and startup clearing are unchanged. **No dedicated far GLB/LOD authored yet**; 5,969 triangles and 5 material surfaces per tree mean this is a **performance comparison**, not yet a production forest optimization. To switch instantly in code/config without modifying art, set `UsePineGlb=false` and reload the world to restore procedural pines.

**Pending acceptance:** Godot import, C# compilation, 2 camera-mode visual check, top-down fading, spacing/placement and FPS comparison; nothing has been runtime tested in this remote environment.
 
## PINE-OPT1 — Foliage geometry optimization (2026-10-28)

The canonical `Artwork/PineTree001.glb` was reduced from **7,679 → 5,969 triangles (-22.3%)**, by reducing every one of the 171 needle clusters from five cross-section rings to four. No clusters, trunk geometry or branches were removed. The original pointed tips, five material groups, imported GLB root, dimensions, colours and `PineTreeMaster` size control are preserved; file size fell from 603,168 to 470,472 bytes.

**Forest compatibility:** `PinePlacement` already loads this path and GPU-instances the entire mesh. No changes to its five-surface shader material assignment, seeded world spawning, collision, wind, occlusion fade, `UsePineGlb` toggle or tree placement. There is **no new far-art LOD**. This reduces geometry, not material draw calls, and performance gains must be measured in Godot.

The original higher-detail file remains available in previous Git history. Confirm visually in F1 and in-world after the GLB reimports.
