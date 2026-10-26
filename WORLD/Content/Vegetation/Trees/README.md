# Procedural conifers

TestWorld now uses seeded tall pines: straight tapered trunks, downward-angled branch tiers and overlapping narrow needle fans. Four shared near/far mesh variants use Terrain3D height/slope sampling and chunk MultiMeshes. ImportedMeshes still replaces generated artwork.

TreeMaster owns height range/multiplier, trunk taper, tier count, length falloff, foliage density/budget, conical width, colour variation, placement density, distances and subtle GPU wind. Old oak-only shape properties were removed. No per-tree nodes or processing were added. Chunk movement and height-map changes remain the placement triggers; idle placement is dormant.

F9 opens the environment panel. Wind updates shared materials immediately. Shape and placement sliders require Apply/Rebuild vegetation; this rebuilds only nearby tree batches and the grass particle pool, never terrain. Zero placement density frees tree batches. Runtime settings are copies of master resources and are discarded on exit.

Implemented, awaiting user visual/performance validation. Brief C# compilation and headless scene launch only; no new rendered comparison or GPU profiling. Existing Validation notes describe the previous oak implementation.

## Foliage cost / culling revision

Replaced the 3,600 six-triangle thin needle fans with 900 closed six-triangle elongated tufts, adding vertical scatter and configurable length/thickness. Near default: 5,920 triangles (864 tufts plus trunk/branches), previously 22,336. Far default: 1,120 triangles (6 tiers, 4 branches, 6 tufts/branch), previously 4,624; distant branches now simplify too. These are geometry counts, not measured frame-time improvements.

Placement batches are now 48m instead of 96m; native range culling remains per batch, not per tree. Near/far switch is 60m; outer range stays 240m, measured from batch centers. A tree at the batch edge can remain visible beyond that distance. Opaque hard LOD transitions replace Self fades that could put foliage into an expensive transparency path and overlap both levels. A visible LOD pop is the tradeoff. Closed tufts use backface culling. Wind avoids per-vertex matrix inversion (placement uses uniform scale). No per-tree CPU callbacks were added.

F9 exposes tuft length/thickness, LOD/render distances and a tree-shadow toggle (Apply/Rebuild). Shadows remain enabled near, disabled far; turning them off provides a useful local comparison. Original terrain and grass untouched. Brief compile/launch only; visual acceptance and FPS comparison deferred to user.

## Distant silhouette correction

Far meshes now retain every branch tier and all six radial branches. Only tuft count/detail reduces; coarser, wider foliage preserves canopy coverage. Branch-local seeds keep branch positions identical across LODs. Default distant mesh is 1,600 triangles (144 tufts plus full branch skeleton), versus 5,920 near. This deliberately spends a little more distant geometry to retain the pine outline. Native LOD still chooses by batch centre, so two trees can differ in detail despite their individual distances; 48m batches limit, but do not eliminate, that approximation. No claim of per-tree LOD. User visual/FPS validation pending.

Default HeightMultiplier is now 1.5, also reflected by TreeMaster and the F9 initial reset value. Placement density/range and mesh budgets are unchanged.

## Thickened trunks

Default trunk geometry now uses `TrunkRadius × TrunkThicknessMultiplier × seeded thickness variation`: base radius remains 0.42m, multiplier **2**, variation **0.85–1.25**. Radius before existing whole-tree instance scale is therefore 0.714–1.05m. Only the main trunk/taper uses this new factor; branch radius, foliage spread, height, forest positions and other random streams are preserved.

Thickness is seeded per shared art variant (four variants by default), not a unique material/mesh per placed tree. Near/far meshes use the identical independent thickness seed so LOD changes do not alter trunk width. Existing whole-tree size variation remains. F9 Pine trees exposes thickness multiplier/min/max as staged controls: press Apply / Rebuild vegetation. Imported meshes are unaffected. No per-frame work or extra tree nodes were added. Source/binding checks passed; rendered proportions still need local acceptance.

## Solid bark correction

Owner observed all trunks looking like cutaways from outside. Wood triangles had outward calculated normals but the wrong submitted front-face winding for Godot culling. Wood now submits clockwise front faces while retaining outward lighting normals. Both ends of each tapered trunk/branch are capped. Leaf generation/materials are unchanged; no double-sided material workaround or extra per-tree nodes. The same generator produces near/far meshes, so both LODs receive the correction.

Brief numerical checks cover closed taper edge counts and outward side/top/bottom normals. No Godot/.NET here: rendered culling, branch ends and LOD transition still need owner confirmation after rebuild/restart.
