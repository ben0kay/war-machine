# Lessons Learned — War Machine

**Purpose:** A concise, growing record of concrete War Machine failures, their causes, confirmed fixes, and prevention checks. Read with [AGENTS.md](AGENTS.md), [CodePreferences.md](CodePreferences.md), and the root [README.md](README.md) **before coding**. Prefer the actual current-branch source when older notes disagree. Add future lessons only when they are supported by a reproducible failure, source inspection, or a validated correction.

## LL001 — Godot C# `[Tool]` preview exported a non-tool custom Resource (2026-10-31)

**Symptom:** After pulling the DF2.8 multiple-forest-zone integration, the Godot editor emitted repeated errors when loading the War Machine project:

```text
System.InvalidCastException: Unable to cast object of type 'Godot.Resource' to 'CanopyTreeSettings'.
at CanopyTreeVisual.SetGodotClassPropertyValue(...)
```

The FPS test also failed to start reliably during this incident. The repeated cast exception was specifically traced to **editor-time canopy preview resource assignment**; the screenshot alone did not establish that this exception was the direct cause of every game-process exit.

**Root cause:** `VEGETATION/Trees/CanopyTree/CanopyTreeVisual.cs` is marked `[Tool]` and declares `[Export] public CanopyTreeSettings Settings { get; set; }`. Its referenced custom resource script, `VEGETATION/Trees/CanopyTree/CanopyTreeSettings.cs`, originally had `[GlobalClass]` but **not `[Tool]`**. In this editor-loading context, Godot instantiated/deserialized the resource as a plain `Godot.Resource`, which could not be assigned to the strongly typed exported property. Both `CanopyTreeMaster.tres` and `CanopyTreeLeafierMaster.tres` already pointed to the right source script; rewriting tree art or forest settings was unnecessary.

**Fix applied:** Add `[Tool]` to the custom resource class while keeping `[GlobalClass]`; use separate attributes, as in the working source:

```csharp
[Tool]
[GlobalClass]
public partial class CanopyTreeSettings : Resource
{
    // Existing exported fields unchanged.
}
```

Changed script: `VEGETATION/Trees/CanopyTree/CanopyTreeSettings.cs`. The canopy preview scenes, .tres values, meshes, forest tree placement, tint, LOD, chunk logic, terrain and collision were **not** changed for this fix. Corrective commit: [6b2cdb4](https://github.com/ben0kay/war-machine/commit/6b2cdb45616a73a1cf7e48b8cbb1879fda9d2252).

**Observed validation:** After pulling and rebuilding/reopening Godot, the user reported successful startup. The game output showed **three approved forest zones**, with 425, 369 and 387 seeded DF2 trees respectively, 1,181 trees total. This confirms the project proceeded through all three forest initialization paths; it does not by itself measure FPS or guarantee absence of unrelated warnings.

**Prevention checklist:**

- When writing an editor-executed C# `[Tool]` node that exports a *custom typed C# Resource*, check that the referenced resource class is also marked `[Tool]` when it must be constructed/inspected by the editor.
- Keep `[GlobalClass]` for inspector-recognized custom resources where appropriate, and use separate `[Tool]` and `[GlobalClass]` declarations.
- Confirm the `.tres` `[ext_resource type="Script"]` path points to the actual C# resource script. A correct path is necessary but does not replace editor-time type compatibility.
- After pulling changes involving C# scripts or resource classes, **build the Godot .NET solution and reopen/reload the editor** before diagnosing new resource cast errors as world-generation failures. Review the **first substantive error and its stack trace**, not only downstream repeats.
- Distinguish **editor-time resource loading**, **C# compilation**, **scene initialization**, and **runtime game-process crashes**. If startup still fails after the resource errors are fixed, capture the new first runtime exception and isolate recently enabled zones without deleting or regenerating saved terrain.
- Avoid changing an entire forest or disabling features to work around a clearly identified editor resource type mismatch. Confirm the smallest root-cause fix first.

**Scope note:** Do not indiscriminately add `[Tool]` to every C# resource. This lesson applies to resources involved in editor-executed tool scripts; inspect the consumer and actual failure.

 
## LL002 — Terrain3D terrain coplanar with building floors and hole overshoots exterior entrance (2026-10-31)

**Observed:** In the isolated `DEBUG/Scenes/BuildingTerrainTest.tscn` test, screenshots showed concrete floors alternating with noisy terrain patches along the ground-floor corridor, and a visible terrain-to-foundation gap at the front of PoliceStation001. The owner separately confirmed that the stairs into the basement looked correct.

**Confirmed causes (source + geometry):**

1. The station's own collidable ground-floor slab has finished elevation **Y=0**, as does the first Terrain3D test's flat 256×256m heightmap. The initial test removed terrain **only under the 48×32m basement**, leaving coplanar terrain across the other 48×32m of the station. Two visible surfaces share the same depth, producing **Z-fighting** (irregular patches/flicker as the viewpoint moves); no texture/material fault is required.
2. The original test put hole bits on **inclusive exterior-edge vertices** (world X=104..152, Z=128..160). Terrain3D discards any triangle touching a hole-marked vertex, so the actual hole extends beyond the marked edge by a terrain grid cell, including just outside the front facade. The building ground slab is also slightly inset (~0.12m) relative to its exterior shell, making an open seam at door thresholds visible.

**Corrective source change (awaiting local visual/physics validation):** For this test, remove terrain from **under the entire collidable station footprint**, not just the basement. With station centre (128,128) and 48×64m exterior dimensions, mark hole vertices **X=105..151 / Z=97..159**, resulting in missing quads approximately under X=104..152 / Z=96..160. Exterior terrain remains at Y=0; the building slab is the **sole interior floor surface**. Place two *site-owned* front/rear entrance thresholds across the narrow terrain-to-inset-slab seam, with their tops deliberately **0.04m above** the overlapping approach surfaces so they do not themselves Z-fight. The police station generator, stair geometry/collision and world terrain are untouched.

**Persistence trap and preservation:** Terrain3D's native `.res` region files are persistent independent of the scene and one-time generator. Merely changing a script's cutout bounds will **not** repair a region already saved with the old hole. To protect any manually sculpted test data, the corrected test uses a **new `DEBUG/Scenes/TerrainData/V2/`** save directory and refuses overwriting any existing saved region. V1 remains untouched; V2 initializes on its first F6 run. Do not copy old data to the new directory.

**Prevention contract for future buildings with or without basements:**

- Declare **building shell bounds**, **actual owned floor/deck bounds**, **basement excavation bounds** and **Terrain3D vertex spacing** in one site-coordinate frame (accounting for building translation/rotation). Compare against *actual* authored geometry rather than assuming a basement and building cover the same area.
- **Never leave two opaque horizontal surfaces at the same elevation.** If a building supplies a collidable finished ground-floor slab, either remove terrain across the covered footprint or deliberately maintain adequate vertical separation; do not blindly shrink the basement hole and leave terrain at the same Y as an uncut interior floor.
- Terrain3D hole masks are **vertex-based**, not exact analytical rectangles. At every boundary, account for triangle discard and grid-cell expansion; an endpoint included on an exterior edge can remove geometry outside the wall. Test boundary samples and inspect doorways from *both sides*, not just whether the basement centre is hollow.
- Independently check **terrain mesh**, **native terrain collision**, **building slab/collision**, **exterior level approaches/door thresholds**, and the **basement**. A basement stair test passing does not verify the rest of the structure/terrain interface.
- Fill small **doorway transitions with intentional site architecture** (collidable landing/threshold with noncoplanar surface), not renderer depth bias or a duplicate giant floor slab. Explicitly verify no new Z-fighting under the added threshold.
- Protect existing Terrain3D saved regions: use a distinct test data directory or an explicit versioned, backed-up migration. Never silently regenerate or overwrite user terrain on every F6 run. Prefer one-time seeding with cleanly logged validation errors.
- Perform a **ground-floor corridor sweep + entrance eye-height seam inspection + basement walk + exterior perimeter walk** after any terrain/building alignment change. Verify collision separately (e.g. Debug → Visible Collision Shapes). C# source checks alone are not visual or runtime acceptance.

**Validation status:** owner screenshot confirms the original overlap/gap and that stairs appear good; the V2 fix has been committed in source only and awaits Godot compilation, rendering and physics checks.


## LL003 — Uniform concrete values concealed stairs and ceiling boundaries (2026-10-31)

**Observed:** Owner-supplied first-person screenshots of PoliceStation001 (particularly its basement U-stair) showed dark, hard-to-see step depth and a nearly monochrome junction of floor, wall and ceiling. The stair geometry itself was reported walkable. This is a separate issue from LL002's coplanar Terrain3D/building floor flicker.

**Confirmed source cause:** `PoliceStationMesh.Build()` originally used the same `PoliceStationSettings.Concrete` colour for all finished ground and upper decks and passed it to `BuildBasement` for both basement walls and floor; the initial stair-tread value was the same colour as that concrete. Each `StructureGeometry.Box` uses one material for all six faces, so the slab underside (ceiling) inherited the floor appearance. Limited indirect/stairwell light makes already-close values appear flatter.

**Correction in source (visual/runtime acceptance pending):** Add separate exported `FloorSurface`, `BasementFloor`, `CeilingSurface` and muted dark-blue `StairEdgeStrips` materials, keeping higher-value walls and contrasting tread/riser colours. Add noncolliding ceiling underside visual panels offset away from structural slabs and split exactly around both U-stair shafts, assigned to their **owning upper floor slab / ground slab / roof visual role**. Give each of the two upstairs and two basement flights a 6cm blue visual nosing strip per tread, offset above the tread top; keep all existing step mesh dimensions, ramp/railing collision, guardrails and landing geometry unchanged. No extra lights or runtime processing are introduced in this pass.

**Prevent recurrence:** Before authoring or approving any stairs or basement, inspect the material **value hierarchy** from standing and descending FPS camera height. Flat procedural primitives need intentional surface separation, not only a uniformly believable architectural colour. Visual underside overlays must never cover a real stair opening, share a coplanar surface, create duplicate colliders or break floor/roof preview/fading ownership. Nosing is a surface cue, not a physics step or obstacle. Keep colour choices editable in the building master resource. Validate in local Godot—source-level review alone cannot prove visual legibility or lighting.

**Validation status:** original screenshot and material-reuse cause confirmed; new visual pass source-implemented and awaits local Godot C# build, camera/stair walkthrough and Z-fighting inspection.


## LL004 — C# stair edge material shadowed a rail-coordinate local (2026-10-31)

**Observed:** The Godot C# build after PoliceStation001 PS1.6 failed in `PoliceStationMesh.cs` with **CS0136**: `A local or parameter named 'edge' cannot be declared in this scope because that name is used in an enclosing local scope`. The screenshot also showed a separate nullable-reference warning in the debug player, which is not caused by this compiler error and is not addressed here.

**Cause:** PS1.6 introduced a `Material edge` parameter for the new blue stair-nosing strips in `StairFlight(...)`. The pre-existing rail construction in the same method used `foreach (float edge in ...)`, redeclaring `edge` within the parameter's scope. This is a C# compile-time identifier collision, not a mesh/collision or Terrain3D failure.

**Fix:** Rename only the rail coordinate loop variable and its two geometric uses to `railX`. Keep the `Material edge` parameter, the stair-nosing visual, all four stair flights, rail geometry/collision and settings unchanged.

**Prevention:** Before adding a new method parameter or local, scan its **complete method scope** for same-named variables (including `foreach` and nested scopes). Prefer descriptive identifiers such as `edgeMaterial` and `railX` rather than generic `edge`. Review the final diff and run **one initial Godot .NET build** whenever practical; balanced braces/static source checks are not proof of compilation.

**Status:** Fix committed in source, actual Godot rebuild pending local user verification.

## LL005 — Floating stair rails and daylight between thin step meshes (2026-10-31)

**Observed:** After PS1.6, the owner supplied an FPS screenshot showing handrails/balusters visibly suspended beside PoliceStation001's U-stair and gaps through thin stair treads, despite successful walking.

**Confirmed source cause:** `StairFlight()` generated each noncolliding tread as a 0.07m-thick plank in a **3.2m-wide** flight, but placed the railing centre lines **±1.76m** from the centre—outside the tread edges **±1.6m**. Posts used a fixed 0.85m height and a slope-calculated centre; their bottoms therefore did not reliably contact any physical-looking step, especially descending. Risers were placed near the far edge of their respective steps, rather than at the boundary to the preceding elevation, so the flight looked open-sided. The one smooth incline collider supported movement but did **not** create a visible stair backing.

**Source correction pending owner validation:** Shift rail centres inwards to **±1.52m**, add continuous visual side stringers and a sloped underside pan, thicken treads to **0.16m**, position each riser at its entering edge and give each rail its own posts with bases derived from the exact chosen tread/landing height and tops from the matching sloped handrail. Retain the original four smooth walking ramp colliders, level changes, U-turns, blue nosing, deck shaft cutouts and collision semantics. Basement detention rooms introduced in the same PS1.7 phase must also maintain genuine open cell entrances and connected wall ends.

**Prevent recurrence:** Every new FPS stair prefab must be inspected **from underneath and both sides**, not only top-down or by walking on it. Check tread width versus guard X offsets, real riser continuity on rising and descending flights, step-to-landing joints, baluster anchors, collision versus visual boundary and existence of a physically plausible support/soffit. Do not introduce dozens of individual step colliders for a visual gap; keep smooth ramps as the walking proxy when intended. See `BUILDING_DESIGN_GUIDE.md` PS1.7 for the reusable contract.

**Status:** photographed original defect and source geometry confirmed; corrective source is implemented, but Godot build and FPS rendering/physics acceptance require the user's local test.

## LL006 — Side stringers concealed banisters and turn landing was unsupported (2026-10-31)

**Observed:** PoliceStation001 screenshots after PS1.7 showed a railing that looked half-solid, a floating basement U-turn landing and some projecting ends/columns.

**Confirmed source causes:** The recently introduced 0.50m-deep `SideStringer` boxes blocked the sightlines through the lower rail posts. Rail post positions were not exactly uniform. Each stair turn landing was hardcoded 8.6m wide although two 3.2m lanes at X=11 and X=15.3 span **7.5m**, creating roughly 0.55m overhang at each outer end. The basement mid-landing had its own slab collider but no visual columns beneath it.

**Source correction (local visual acceptance pending):** Keep one **20cm** noncolliding underside under the treads, remove the thick side stringers, use nine balusters evenly spaced along each flight, compute both turn widths from the real flight bounds, and support the basement platform with two full-height static corner columns from basement floor to the landing underside. Preserve the stair ramp, treads, risers, 6cm blue nosing, cell block, Terrain3D and all above-ground architecture.

**Prevention:** Check an authored stair in first person from side and underside; verify visible, uniformly spaced balusters, rail/pillar joins, no obstructive half-height panels, platform ends aligned with real stairs, and non-floating structural supports. Read the building design guide's PS1.8 notes. Source-only commits require local Godot validation.
