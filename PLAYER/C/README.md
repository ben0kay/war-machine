# Procedural player mesh

`PlayerMeshBuilder.cs` builds a lightweight, stylized **soldier-like** human for the angled 3D world. The original box-only placeholder has been replaced with rounded/tapered chest and limbs, a readable helmet/face, utility vest and straps, compact backpack, cargo pockets, knee pads, gloves and boots. Small deterministic green/brown camouflage fabric textures are generated once in C# and reused; no external texture/model assets or per-frame mesh work.

The knee pivots now visibly flex on the forward swing (alternating left/right) and the ankles add restrained toe/heel roll while preserving level-looking boots. A small rounded joint hides the thigh/shin seam. Tune `KneeBendDegrees` and `FootRollDegrees` in `PLAYER/Configuration/PlayerMaster.tres`; these affect walking only, with reduced influence during crouching.

A new `ShoulderGirdle` pivot and a pair of `Elbow` pivots give the soldier a more natural walk: the upper torso counter-twists, shoulders roll subtly and elbows flex while arms swing opposite the legs. Original arm/leg rest positions, names and the `WeaponMount` socket are preserved. These visual motions are strongest while moving unarmed and heavily damped in the rifle pose. Tweak `TorsoTwistDegrees`, `TorsoRollDegrees`, `ShoulderSwayDegrees` and `ElbowFlexDegrees` in `PlayerMaster.tres`.

**Original animation contract is preserved:** `VisualRoot`, `UpperBody`, `LeftArm`, `RightArm`, `LeftLeg`, `RightLeg`, `Knee`, `Ankle`, and the existing `WeaponMount` remain available for `PlayerController`'s movement, crouching and rifle poses. The player root capsule retains all collisions; the visible mesh has none. Camera, controls, aim, health and enemy logic were not modified.

The palette and pieces are near the top of `PlayerMeshBuilder.cs`. Geometry is intended to read well at gameplay zoom, not to be a high-resolution FPS model. `../Artwork/` remains reserved for a future imported replacement that preserves pivots and sockets.

**Brief local checks:** Run `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn`: inspect front/back at play zoom, walking, running, right-Ctrl crouch, mouse aim, rifle hold/fire. Check that one knee bends during each leg's forward swing, that boots settle at rest, and that the crouch pose still folds cleanly. Also check shoulder counter-sway and elbow articulation while walking without a weapon, and ensure rifle aim remains steady when moving with the weapon equipped. Check that the vest/pack do not obscure weapon alignment, and that legs and arms do not detach. No visual runtime verification has been performed here.

ShoveHandsBuilder.cs builds two shared-mesh glove visuals once per FPS firearm for the CM2-D side-on shove. The pose helper hides them on completion/cancel; this builder has no processing or combat logic.

## Prone rig pose (2026-10-28)

The shared player keeps its existing generated human rig, arms/knees/ankles and `WeaponMount` nodes. `PlayerController.Pose` now bends the upper body forward toward local -Z, lowers the torso/pelvis, extends arms and legs into a basic crawl pose, and uses a short alternating active-only gait while prone. The FPS test hides this third-person rig but inherits collider/eye-height movement. This is first-pass positional rigging rather than a dedicated ground-hugging skeletal prone animation. No geometry is rebuilt on a stance change, no per-frame work is added while fully idle. Review prone clipping from overhead and weapon socket alignment in Godot.
