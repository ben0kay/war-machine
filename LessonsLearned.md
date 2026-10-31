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
