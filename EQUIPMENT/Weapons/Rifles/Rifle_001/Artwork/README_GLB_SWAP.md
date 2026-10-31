# Rifle_001 — grounded tactical GLB visual (first-pass)

The GLB is now wired into the repository's `Rifle_001.tscn` on the `implement-td0-td1` branch. The original ZIP installation instructions below are retained for manual or rollback use. It replaces only procedural generated Visual art with a low-poly **actual GLB** inspired by the owner's first reference screenshot, **without the holographic sight**. More practical styling than sci-fi: brushed/dark matte metal, muted olive polymer, rail segments, compact front and rear iron sights, receiver seams, vented handguard, stock/grip/magazine.

## Install

1. Back up or commit your original `EQUIPMENT/Weapons/Rifles/Rifle_001/Rifle_001.tscn`.
2. If you pulled the current GitHub branch, the GLB and corrected `Rifle_001.tscn` are **already included** — do not extract the old ZIP over this scene. If installing from an offline archive instead, preserve the entire `EQUIPMENT/` folder structure and include both the model and scene file.
3. Let Godot import `Artwork/Rifle_001_ReferenceStyle.glb`. Build C# if Godot prompts you and run the FPS scene with F6: `WORLD/FirstPerson/FirstPersonWorld.tscn`.
4. Inspect hip-fire and MMB ADS framing, sight height, recoil visual kick and cyan aim line. The first-pass model was generated offline and **has not been test-run inside Godot**. It may need a visual-offset / optic alignment tweak.

## Scene contract preserved

`Rifle_001` still owns the same `FirearmController.cs`, `Settings` resource (`RifleMaster.tres`) and exact original `Muzzle` marker at `(0,0.055,-0.81)`. `Visual` remains an ordinary `Node3D`, with the GLB instance under it. `FirearmVisualRecoil` still animates that `Visual` node; whole-root ADS and sway still work, as do slots, ammo, hit rays, spread, stamina costs, audio and camera recoil. No gameplay script, `RifleMaster.tres`, movement values or firing stat is changed by this package.

This is a *design first pass*, not a replica or a functional real-world mechanical design. No holographic optic, laser module or glowing side strips. The bright cyan line seen when aiming in the game is the pre-existing FPS debug aim ray, not part of the model.

## Technical specs

- Source orientation: +Y up / **-Z forward**, Godot/metre-scale coordinate space.
- Approximate extents: X=-0.153..0.153m, Y=-0.334..0.228m, Z=-0.809..0.417m.
- ~9,984 triangles across **nine material groups**. PBR solids; no external images or remote dependencies.
- Optional Python authoring recipe in `Artwork/Sources/build_rifle_reference.py` (uses `trimesh` and `numpy`); for visual re-authoring, imported GLB can also be opened in Blender.
- `C/RifleMeshBuilder.cs` is deliberately left untouched in your project so you can return to the older procedural appearance. This pack's scene no longer runs that C# visual builder.

## Rollback

Restore the previous `Rifle_001.tscn` (from Git or your backup). That scene uses `RifleMeshBuilder.cs` on `Visual`. You may keep or remove the new `Artwork/Rifle_001_ReferenceStyle.glb` independently; reverting the scene is enough to restore the original look.

## Validation status

The GLB file was exported and read back successfully in the authoring environment. Its internal material groups, triangle count, and bounds were checked. **Godot import, render fidelity, FPS iron-sight alignment and runtime code compatibility have not been verified.**
