> **SOC0 collision enabled (2026-10-28):** Hedge001 now generates one static bounds box from its imported GLB, scaled with `SizeMultiplier`, even for its existing convenience-store site placements. This may alter walking routes at the parking area: check player/zombie paths and door/walkway clearances in-game. Fixed ornamental foliage is a solid blocker; procedural natural bushes are unaffected. Source implemented, runtime acceptance pending.

# Hedge001 — neatly clipped rectangular landscaping hedge (GLB)

**Status:** Original GLB art and canonical reusable visual scene authored. Godot import/appearance validation **pending**. This is a non-pickup **static landscaping object**, not a member of the procedural `VEGETATION/` system or the world chunk vegetation generators. No automatic procedural vegetation scatter, terrain clearing, collision, interaction or F1 menu entry. **Eight explicit scene instances are now used by ConvenienceStoreSite001** as decorative parking/walkway landscaping, using a site-specific size override; the canonical GLB and default master are unchanged.

## Contents and size

- [Artwork/Hedge001.glb](Artwork/Hedge001.glb) — real binary GLB 2.0, one baked foliage mesh, **three opaque olive/moss PBR material surfaces with brightness vertex variation**, and no image textures. The foliage has small faceted leaves/buds and slight surface variation, but the top and sides are visually squared-off as if regularly trimmed. A few thin darker woody stems emerge at the base.
- [Hedge001.tscn](Hedge001.tscn) — canonical asset scene: root `Hedge001` uses existing `PlaceableVisual`, `Model/Artwork` contains the imported GLB. No object-specific geometry-generation C#.
- [Configuration/HedgeMaster.tres](Configuration/HedgeMaster.tres) — existing shared `PlaceableSizeSettings` with `SizeMultiplier=Vector3(1,1,1)`. X width, Y height, Z depth, all **relative** to the GLB.
- Native geometry bounds (measured at generation): approximately **4.805 m wide × 2.138 m tall × 1.274 m deep**. Root is at ground Y=0; long dimension along X. No required front direction.
- **2,540 triangles, three material surfaces in one mesh**, 7,620 triangle vertices (flat-shaded), opaque double-sided materials. ~216 KB uncompressed GLB; no leaf alpha, skeletal animation, shader, lights or ticking scripts.

## Intended size relative to player

The current player capsule is 0.6m across and 1.8m tall. This hedge is about **8× player width**, **just taller than player height** and **~2× player capsule depth**. It should suit a row in front of the convenience store, parking-lot perimeter, a fenced garden or an ornamental entrance. It can be placed repeatedly as the same shared GLB; do not duplicate source files for different lengths.

To change the *default* size across all instances, edit `Configuration/HedgeMaster.tres`: e.g. `SizeMultiplier=Vector3(1.25,1,1)` makes a ~6m-wide hedge; `Vector3(1,0.9,1)` shortens its height. For individually sized copies, duplicate/override the settings resource on that scene instance so other copies retain the master. Scale affects only `Model`, not future collision.

## Visual-only boundary / planned game use

Hedge001 belongs in `WORLD/Content/Assets/Objects/Static/Landscaping/Hedges`, **not** `WORLD/Content/Vegetation/Bushes`, which owns procedural natural bushes. The ornamental hedge is an intentionally manually placed static object. **It currently has no physical collision**, despite the static folder name; the object authoring guide defers collisions. If this is later made solid, fit one simple appropriately resized box collider to the hedge's outer mass, and consider top-down building/foliage visibility only if explicitly requested. Do not modify terrain vegetation clearing or site placement as part of this authoring pass.

## Validate in Godot

Pull `implement-td0-td1` and let Godot import `Artwork/Hedge001.glb`. Open `Hedge001.tscn` directly in the editor and inspect from overhead and FPS angles; check flat top, muted multi-green leaves, grounding and absence of pink/missing materials. Adjust the `SizeMultiplier` and reload/reinstance. `dotnet build war-machine.csproj` can confirm shared C# helpers if installed. **No runtime Godot import, render, physics or perf profiling was performed remotely.**

**The F1 spawner is an explicit list; Hedge001 is not in it** because this request was to author a reusable asset, not to add a debug menu entry or place it into the current convenience-store scene.

## White-hedge screenshot fix (2026-10-28)

The GLB previously stored green leaf colours entirely in glTF `COLOR_0`, but the sole PBR material had a white base. The convenience-store screenshot showed white hedges, consistent with imported vertex colours not being multiplied into the displayed material. The **GLB itself** now provides an explicit evergreen `baseColorFactor` plus neutral per-vertex brightness variation. This works for all future Hedge001 instances, not only the convenience-store site, with no material override script at that point. The **subsequent muted-olive revision described below** replaces that formerly single-surface material with three subdued green PBR surfaces. Same original geometry, **2,540 triangles**, no bitmap textures or runtime generation.

The site height was also raised from the first-pass **~0.96m to ~2.41m**, exactly **2.5× taller**, through the site-only HedgeRowMaster `SizeMultiplier.Y=1.125` (previously 0.45). The canonical hedge-size master remains `Vector3(1,1,1)`. Location, length, depth, entrance opening and collision-free status are unchanged. Verify green/height after reimport and scene restart; remote Godot render validation pending.

## Muted natural olive foliage (reference-photo adjustment, 2026-10-28)

After the initial evergreen fix, the user supplied a close-up photograph of a **dark olive/moss clipped garden hedge** and reported that the GLB remained much too bright/unnatural. The original base tint `[0.30, 0.52, 0.29]` in **linear PBR values** was indeed too light/blue-green under bright ambient lighting.

**Latest GLB fix:** reallocate the identical 2,540 triangles among **three** materials instead of just recolouring the only material. Shadow foliage is deep olive, the main foliage is restrained moss green, and brighter leaves are muted olive/yellow-green. The underlying `COLOR_0` provides additional dappled light/dark variation, but **all three materials have explicit olive/green fallback values** if vertex colours aren't shown by Godot. This avoids a flat one-colour slab and neon-green appearance. Source GLB file and documentation are in `Artwork/`; no GLB clones, C# mesh generation, alpha transparency or new textures.

**Nothing else changed:** `HedgeMaster.tres` retains its original dimensions. ConvenienceStoreSite001's override remains `Vector3(1, 1.125, 0.43)` (~2.41m tall). Its 8 hedge positions, entrance path, parking and lighting are unchanged; collision is still unimplemented. Higher material surface count adds **two draws per visible instance**, a small trade-off for distinct foliage tones in this authored landscaping asset. For hundreds of distant hedges, evaluate a baked atlas/one-material LOD later, not in this patch.

**Local visual verification pending:** after Godot reimports `Artwork/Hedge001.glb`, compare from both FPS and overhead in daylight and at night with the user's reference photo. Check there are three distinct, muted-green material surfaces; don't assert photorealistic leaf geometry from this colour-only pass. Source GLB structure was validated remotely, actual Godot render was not.
