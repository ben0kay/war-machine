# Procedural conifers

TestWorld now uses seeded tall pines: straight tapered trunks, downward-angled branch tiers and overlapping narrow needle fans. Four shared near/far mesh variants use Terrain3D height/slope sampling and chunk MultiMeshes. ImportedMeshes still replaces generated artwork.

TreeMaster owns height range/multiplier, trunk taper, tier count, length falloff, foliage density/budget, conical width, colour variation, placement density, distances and subtle GPU wind. Old oak-only shape properties were removed. No per-tree nodes or processing were added. Chunk movement and height-map changes remain the placement triggers; idle placement is dormant.

F9 opens the environment panel. Wind updates shared materials immediately. Shape and placement sliders require Apply/Rebuild vegetation; this rebuilds only nearby tree batches and the grass particle pool, never terrain. Zero placement density frees tree batches. Runtime settings are copies of master resources and are discarded on exit.

Implemented, awaiting user visual/performance validation. Brief C# compilation and headless scene launch only; no new rendered comparison or GPU profiling. Existing Validation notes describe the previous oak implementation.
