> **ZM-PERF2B:** Native lower-polygon mesh LOD is now source implemented for both rigs with unchanged pivots, material palette, mesh-node count and full near detail. Original/template-copy actors and corpses share geometry; no managed distance loop. Local compilation/render/profiling pending. [Settings and checks](../../MESH_LOD_README.md).

> **ZD4 elbows:** Both procedural rigs now keep forearm/hand art beneath an identity `Elbow` pivot at shoulder-local Y=-0.27m before template batching. Original world placement, palette selection, shoulder gait and hit regions remain. Living elbows do not process; death physics alone articulates them. Check first/cached spawns, sleeve seams, severing and close FPS appearance locally. Previous mesh counts describe the earlier grouping.

> **ZM-PERF2A — source implemented; local validation pending.** Runner geometry uses its own one-time cached batched template: compatible pieces on the same pivot and palette/material are merged to one material surface, with original runner proportions, seeded blood/clothing variation, joints, anatomy and corpse visuals preserved. Does not add LOD or affect runner AI.

> **ZM-PERF1 — source implemented, awaiting in-engine FPS/quality measurements (2026-10-28):** Runner reduced **40 → 29 visible mesh instances**. Removed four tiny standalone fingers, two eyes, two raised elbow bruises, two toe meshes and one extra shirt rip. Preserved runner silhouette, eye sockets/brow, jaw/mouth, staining, articulation, seeded materials, hitboxes and corpse transfer.

# Zombie 002 procedural artwork

`ZombieRunnerMeshBuilder.cs` constructs only the Runner's low-poly visuals from shared static mesh/material resources. Keeps `VisualRoot`, `LeftArm`, `RightArm`, `LeftLeg`, `RightLeg`, `LeftKnee` and `RightKnee` compatible with shared `ZombieController` movement. The subtype's leaner physique, shorter asymmetric sleeves, darker ragged shirt and running shoes make it distinguishable from Zombie 001 at angled camera zoom. Future imported meshes belong in sibling `Artwork/`.

## Human anatomy detail pass

Both zombies now share `../../C/ZombieHumanGeometry.cs` for angular, properly capped/UV-mapped skull and torso contours, tapered limbs, slim hands, smaller eye slits and non-bubble feet. The Runner remains slimmer and faster, using the existing gait and sight behaviour; only its static lower-leg art has a very small uneven tilt. Same seed-driven ZombieAppearance variations, no materials/shaders/images regenerated per actor. Test FPS and top-down for silhouette, eyes and disconnected knees.


## ZM-PERF1 runner tests

One longer glove shape and one extended trainer shape replace tiny separate meshes. The runner keeps all its named shoulder, knee, lower-leg, ankle and head pivots. Check front/back/side at normal top-down zoom and in close FPS view, and confirm runner gait, limb severing and corpse death remain intact. Compare identical visible crowd before/after, drawing and FPS monitors. No new runtime processing or material allocation is added. **Source-only inspection; engine compile/visual benchmark pending.**


## ZM-PERF2A runner acceptance

Runner has a separate cached geometry template from standard zombies but shares the `ZombieVisualBatcher` and the same `ZombieAppearance.PaletteForPart` mapping. Subsequent runners duplicate the template, preserve all pivot names and receive their own spawn-seeded palette. Please check both the **first** spawned runner and additional runners, the two different glove/sleeve proportions, static cheek/bruise and dark mouth, head tilt, animation and limb severing. Profile draw calls/FPS with the same camera as ZM-PERF1; no verified FPS gain is claimed.
