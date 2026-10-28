# Hedge001 — neatly clipped rectangular landscaping hedge (GLB)

**Status:** Original GLB art and canonical reusable visual scene authored. Godot import/appearance validation **pending**. This is a non-pickup **static landscaping object**, not a member of the procedural `VEGETATION/` system or the world chunk vegetation generators. No automatic procedural vegetation scatter, terrain clearing, collision, interaction or F1 menu entry. **Eight explicit scene instances are now used by ConvenienceStoreSite001** as decorative parking/walkway landscaping, using a site-specific size override; the canonical GLB and default master are unchanged.

## Contents and size

- [Artwork/Hedge001.glb](Artwork/Hedge001.glb) — real binary GLB 2.0, one baked foliage mesh, one opaque vertex-coloured rough/PBR material and no image textures. The foliage has small faceted leaves/buds and slight surface variation, but the top and sides are visually squared-off as if regularly trimmed. A few thin darker woody stems emerge at the base.
- [Hedge001.tscn](Hedge001.tscn) — canonical asset scene: root `Hedge001` uses existing `PlaceableVisual`, `Model/Artwork` contains the imported GLB. No object-specific geometry-generation C#.
- [Configuration/HedgeMaster.tres](Configuration/HedgeMaster.tres) — existing shared `PlaceableSizeSettings` with `SizeMultiplier=Vector3(1,1,1)`. X width, Y height, Z depth, all **relative** to the GLB.
- Native geometry bounds (measured at generation): approximately **4.805 m wide × 2.138 m tall × 1.274 m deep**. Root is at ground Y=0; long dimension along X. No required front direction.
- **2,540 triangles, one mesh primitive/material surface**, 7,620 triangle vertices (flat-shaded), opaque double-sided material. ~214 KB uncompressed GLB; no leaf alpha, skeletal animation, shader, lights or ticking scripts.

## Intended size relative to player

The current player capsule is 0.6m across and 1.8m tall. This hedge is about **8× player width**, **just taller than player height** and **~2× player capsule depth**. It should suit a row in front of the convenience store, parking-lot perimeter, a fenced garden or an ornamental entrance. It can be placed repeatedly as the same shared GLB; do not duplicate source files for different lengths.

To change the *default* size across all instances, edit `Configuration/HedgeMaster.tres`: e.g. `SizeMultiplier=Vector3(1.25,1,1)` makes a ~6m-wide hedge; `Vector3(1,0.9,1)` shortens its height. For individually sized copies, duplicate/override the settings resource on that scene instance so other copies retain the master. Scale affects only `Model`, not future collision.

## Visual-only boundary / planned game use

Hedge001 belongs in `WORLD/Content/Assets/Objects/Static/Landscaping/Hedges`, **not** `WORLD/Content/Vegetation/Bushes`, which owns procedural natural bushes. The ornamental hedge is an intentionally manually placed static object. **It currently has no physical collision**, despite the static folder name; the object authoring guide defers collisions. If this is later made solid, fit one simple appropriately resized box collider to the hedge's outer mass, and consider top-down building/foliage visibility only if explicitly requested. Do not modify terrain vegetation clearing or site placement as part of this authoring pass.

## Validate in Godot

Pull `implement-td0-td1` and let Godot import `Artwork/Hedge001.glb`. Open `Hedge001.tscn` directly in the editor and inspect from overhead and FPS angles; check flat top, muted multi-green leaves, grounding and absence of pink/missing materials. Adjust the `SizeMultiplier` and reload/reinstance. `dotnet build war-machine.csproj` can confirm shared C# helpers if installed. **No runtime Godot import, render, physics or perf profiling was performed remotely.**

**The F1 spawner is an explicit list; Hedge001 is not in it** because this request was to author a reusable asset, not to add a debug menu entry or place it into the current convenience-store scene.
