# Zombie imported artwork — first pass

Implemented, awaiting local Godot import, compilation and visual/gameplay validation.

`ZombieFirstPass.glb` is original, generated segmented character artwork: tapered torso/limbs,
shaped jaw/nose/ears/brows, fingers, boots, scalp and an embedded 768×512 opaque grime/fabric/face atlas.
It is a first-pass stylized asset, not a finished sculpt or a smoothly skinned humanoid.

Standard zombies use it by default. Set `ImportedArtworkEnabled = false` in
`../Configuration/ZombieMaster.tres` to restore procedural art. Runners retain their own procedural model.
The two appearances can coexist; there is no additional runtime animation callback.

## Compatibility contract

The GLB supplies `UpperBody` at (0,.85,0), its `Head`, `LeftArm/Elbow`, `RightArm/Elbow`,
and root `LeftLeg/Knee`, `RightLeg/Knee`. Shoulder/hip/knee/head positions match the current rig.
`UpperBody/Hips` remains independently movable for the corpse pelvis.
`ZombieImportedVisual` loads the PackedScene once and moves its rig children into the existing visual root.
Meshes and the atlas are shared resources; transforms remain per actor.

C# gait, attack, flinch, injury poses, deferred anatomy boxes, limb hiding/stumps and articulated corpse
handoff continue to drive these same nodes. No AnimationPlayer clips or Skeleton3D are supplied.
This retains existing movement, including its limitations: joints rotate rigid segments, they do not
bend continuous skin. A future skinned GLB needs a node-to-bone pose adapter and skeleton-aware
sever/corpse integration; arbitrary downloaded humanoids are not drop-in compatible.

Imported mesh names intentionally do not use the procedural palette categories, so the existing
appearance pass leaves the authored atlas intact. Imported colour variation is deferred.
The GLB has 11 mesh surfaces and 4,832 triangles. Godot's importer can generate LOD; the procedural
ZM-PERF2B buffers do not apply to this asset. No measured frame-rate improvement is claimed.

## Authoring and validation

Rebuild deterministically with Python 3 and Pillow:

```sh
python ENTITIES/TYPES/Zombies/Artwork/Source/build_zombie.py
```

The source is self-contained, with no downloaded assets or external textures. Reimport the GLB in Godot,
then `dotnet build war-machine.csproj`. Open the FPS world and inspect nearby faces/clothing, walking,
pursuit, horizontal attacks, flinches, both leg injuries, limb removal, deaths and settled corpse piles.
Check secondary view, crowd cost and toggling back to procedural art. No terrain rebake is needed.

Source validation covers binary structure, triangle indices, bounds, pivot paths, deterministic generation
and whitespace. Compilation, Godot import and runtime checks are deferred because Godot/.NET are unavailable.
