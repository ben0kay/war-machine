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


## LL007 — Coarse detention bars and empty cell-door transoms (2026-10-31)

**Observed/requested:** After the PS1.7/PS1.8 basement extension, the user requested a second pass because jail cells appeared too clunky and under-detailed. Current source confirmed 12cm-square bars at ~50cm spacing, wide horizontal rails, basic full-height concrete partitions and only an unfilled doorway frame.

**Source correction awaiting FPS validation:** Replace coarse structural bar proportions with 7.2cm uprights and regular ≤38cm gaps, three thin crossrails and a short barred upper transom. Add small, static lower concrete wall panels, trims, ventilation grilles and corridor-visible bay markers. Stage a 90-degree open barred door leaf on the **inside of the cell, visual only**, leaving the original 1.9m-wide walkable aperture and 2.9m headroom unchanged. Keep native collision on fixed grilles but not visual-only detail; use shared material instances and no runtime ticking.

**Prevention:** For procedural jail/industrial rooms, vary material profiles and small architectural cues rather than solving lack of detail with thicker bars or multiple overlapping colliders. Verify actual passage size, spacing versus character capsule diameter, collision-bar agreement, flush divider joins and a functional distinction between an intentionally open mock gate and an interactive physical door. Confirm by local build and walking the corridor into every cell.

**Status:** Original clunky bar measurements verified from source; revised detail geometry committed; Godot rendering and physics validation pending.


## LL008 — Ranger lodge stair, furniture/window and ground wall-height clashes (2026-10-31)

**Observed:** Four owner screenshots from first-person War Machine play showed (1) the Building002 straight staircase beginning too close to the main front door, (2) the reception desk crowding that entrance, (3) a stove/cabinet-like furnishing visually obstructing a window and (4) internal ground-floor wall tops stopping well below the upper-floor deck. PoliceStation001 had previously established minimum stair-landing, opening and wall-top rules, but the lodge did not yet implement them.

**Verified source causes:** The default lodge has a +Z front wall at Z=6.5 and its old first stair at Z=4.55 (**1.95m clear**, below the author's ≥2m threshold). Reception desk and its tabletop details were at Z=4.35. The stove was beside the west-side window bay (side window local Z=-1.8..1.0, stove at Z=0.15), and an equipment locker was near the front-right window. Lower partitions terminated at a fixed **Y=2.95m**, while the 4m upper deck's underside is **Y=3.76m**. The 0.81m open seam is not intentional.

**Source fix awaiting local validation:** Shift the entire straight stair flight and its matching upper-floor opening rearwards to a 3.45m front apron (default start Z=3.05, end Z=-2.45) while preserving 22 treads, existing smooth slope collision and handrail construction. Move the reception desk and its papers/radio farther from the door. Relocate stove/firebox to a solid west-front wall position, equipment locker/leaf to the right partition, and office cabinet/fascia away from glazing. Extend each lower interior partition to **h - 0.24 - 0.02** (default 3.74m), preserving genuine doorway headers and exterior walls. Add one-time local-space `StructureClearance` reservations protecting approaches and window bays, and make deliberate solid furniture placement reject violations of those reservations.

**Prevention:** Check the *actual* 3D entrance-to-first-tread flat walking area including open door leaf and rail posts, not just nominal doorway width. When moving stairs, update both the upper deck's hole and its guards from the same geometry parameters. Reserve window sightlines early enough to catch nearby stoves and lockers. Move every furniture visual/detail and matching collider together. Derive interior wall endpoints from the next collidable slab underside with a small gap; never alter the exterior facade to fix an interior seam. Validate non-default building sizes for clearance rather than silently accepting a layout that cannot physically fit. One initial build and a manual FPS stair/window/ceiling walkthrough are still required.

**Status:** Source-verified defects, correction source committed; local Godot compile, traversal and visual check pending.


## LL008 — DTP1 dirt preview invisible around spawn due to oversized protection mask (2026-10-31)

**Observed:** Owner ran the one-shot DTP1 preview and then FPS, but saw no exposed dirt while exploring near spawn. The preview scene exited automatically; that behaviour occurs after both successful authoring and an exception and is not itself proof of success.

**Confirmed source flaw:** `DirtPatchSettings` originally excluded everything inside a **305×360m axis-aligned rectangle** relative to VillageOriginXZ (X[-150,+155], Z[-250,+110]). WorldMaster's default SpawnXZ is (512,550), which is inside that rectangle. All soil candidate locations inside were rejected before painting. Additionally, the original FPS world logs only a brief fallback message, so an absent/stale preview might go unnoticed.

**Source correction (awaiting Godot visual confirmation):** Replace the coarse mask with individual site/building/road/patrol pads in `DirtPatchProtectedAreas`, add three near-spawn witness patches, output explicit `DTP1 V2 SUCCESS` after the published preview passes its manifest check, and print `DTP1 V2: verified dirt preview ACTIVE` in FPS when the preview is selected. Use a **new V2 directory** rather than deleting or silently overwriting the user's original DTP1 copy. Original native gameplay terrain, saved heightmaps, manual-painted control pixels and foliage logic remain untouched.

**Prevent recurrence:** Whenever an environmental generator is expected to be testable from spawn, compare its placement constraints numerically with the player spawn and the nearest authored infrastructure. Use bounding masks that cover actual objects/roads rather than arbitrarily large no-content rectangles; add a local witness/test case and a distinguishable success/failure log. A CLI/editor scene exiting does not establish generated content, correct loading, or rendering.

**Status:** the oversized exclusion is proven from source; revised material/preview rendering and in-game dirt visibility await user test.


## LL009 — Lodge short interior wall, coplanar porch deck, roof gap, and non-colliding furniture (2026-10-31)

**Observed:** Owner screenshots of Building002 after BL2.1 show a ground-floor partition stopping short at a front entrance joint, shimmering surfaces near the threshold, a triangular upper-level wall/roof mismatch plus unwanted chimney exposure inside, and benches facing the lodge instead of outdoors while the porch fence lacks collision. Owner additionally requested seat/back collision.

**Confirmed code causes:** The exterior wall is 0.23m thick but lower partitions ended 0.25m before the outer centre plane, **13.5cm short of the inner face**. The decorative interior floor and veranda both finished at Y=0.12 and overlapped 14.5cm in Z. Upstairs roof and divider used independent flat-height formulas instead of matching the sloped roof; exterior gables were coupled to the roof visibility owner, and the chimney base extended below the peak region. Bench backs were at +Z beyond their seats, and both bench seats/backs and porch rail/balusters passed a null collider to the mesh helper.

**Code correction (awaiting runtime verification):** Complete the partitions to the shell inner face with 5mm perpendicular overlap; shorten veranda/seams to start outside the facade and span a 16cm transition using one small raised threshold. Extend the upper front/rear wall tops to the roof underside, add one upper-bedroom partition gable and move existing exterior gable visuals into wall ownership without duplicate overlapping meshes. Move the chimney onto the ridge, embedded just into the ridge cap so no chimney passes through the roof into the interior. Move both bench backs toward the lodge so they face outward and add static collision for bench seats/backrests, fence rails and balusters.

**Prevention:** Derive meeting surfaces from actual cross-sections (wall thickness/2, deck extents and slope envelope); never place full interior/exterior finished floor slabs across the same height at a doorway. Roof wall infills must be aligned to real pitch and built **only once** in the appropriate visibility group. Treat intended physical porch railings and furniture as solid or deliberately mark them as cosmetic; check collisions with the player's capsule. Confirm locally by C# build and walking, viewing the portal seams and upstairs roof from both sides.

**Status:** source repair committed / C# build and owner FPS acceptance pending.
