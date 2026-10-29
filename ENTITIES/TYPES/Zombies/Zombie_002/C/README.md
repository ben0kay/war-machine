> **ZM-PERF1 — source implemented, awaiting in-engine FPS/quality measurements (2026-10-28):** Runner reduced **40 → 29 visible mesh instances**. Removed four tiny standalone fingers, two eyes, two raised elbow bruises, two toe meshes and one extra shirt rip. Preserved runner silhouette, eye sockets/brow, jaw/mouth, staining, articulation, seeded materials, hitboxes and corpse transfer.

# Zombie 002 procedural artwork

`ZombieRunnerMeshBuilder.cs` constructs only the Runner's low-poly visuals from shared static mesh/material resources. Keeps `VisualRoot`, `LeftArm`, `RightArm`, `LeftLeg`, `RightLeg`, `LeftKnee` and `RightKnee` compatible with shared `ZombieController` movement. The subtype's leaner physique, shorter asymmetric sleeves, darker ragged shirt and running shoes make it distinguishable from Zombie 001 at angled camera zoom. Future imported meshes belong in sibling `Artwork/`.

## Human anatomy detail pass

Both zombies now share `../../C/ZombieHumanGeometry.cs` for angular, properly capped/UV-mapped skull and torso contours, tapered limbs, slim hands, smaller eye slits and non-bubble feet. The Runner remains slimmer and faster, using the existing gait and sight behaviour; only its static lower-leg art has a very small uneven tilt. Same seed-driven ZombieAppearance variations, no materials/shaders/images regenerated per actor. Test FPS and top-down for silhouette, eyes and disconnected knees.


## ZM-PERF1 runner tests

One longer glove shape and one extended trainer shape replace tiny separate meshes. The runner keeps all its named shoulder, knee, lower-leg, ankle and head pivots. Check front/back/side at normal top-down zoom and in close FPS view, and confirm runner gait, limb severing and corpse death remain intact. Compare identical visible crowd before/after, drawing and FPS monitors. No new runtime processing or material allocation is added. **Source-only inspection; engine compile/visual benchmark pending.**
