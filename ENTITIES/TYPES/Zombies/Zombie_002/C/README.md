# Zombie 002 procedural artwork

`ZombieRunnerMeshBuilder.cs` constructs only the Runner's low-poly visuals from shared static mesh/material resources. Keeps `VisualRoot`, `LeftArm`, `RightArm`, `LeftLeg`, `RightLeg`, `LeftKnee` and `RightKnee` compatible with shared `ZombieController` movement. The subtype's leaner physique, shorter asymmetric sleeves, darker ragged shirt and running shoes make it distinguishable from Zombie 001 at angled camera zoom. Future imported meshes belong in sibling `Artwork/`.

## Human anatomy detail pass

Both zombies now share `../../C/ZombieHumanGeometry.cs` for angular, properly capped/UV-mapped skull and torso contours, tapered limbs, slim hands, smaller eye slits and non-bubble feet. The Runner remains slimmer and faster, using the existing gait and sight behaviour; only its static lower-leg art has a very small uneven tilt. Same seed-driven ZombieAppearance variations, no materials/shaders/images regenerated per actor. Test FPS and top-down for silhouette, eyes and disconnected knees.
