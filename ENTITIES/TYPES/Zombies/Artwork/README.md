# Zombie imported artwork — current organic zombie

**Source integrated; local Godot compilation, import and gameplay validation pending.**

The current `ZombieFirstPass.glb` is the **Organic Rebuild V3**: the textured,
segmented zombie with shoulder overlap and hip/upper-thigh repairs.
The file is self-contained, with embedded textures. The uploaded V3 is
approximately **40,915 triangles**, **11 mesh groups**, and **23 glTF nodes**.
The original low-poly C# procedural zombie is **not deleted**.

## Select the model or restore the old one

Standard zombies read `ImportedArtworkEnabled` from
`../Configuration/ZombieMaster.tres` (currently **true**):

- **true** — try `ZombieFirstPass.glb` first. When import or rig validation fails,
  fall back to the cached C# procedural mesh.
- **false** — always use the original C# procedural mesh from
  `../C/ZombieMeshBuilder.cs` and its shared helpers.

After changing the setting, restart the running scene so existing zombie instances
are reconstructed. `Zombie_002` runners keep their separate procedural art;
this switch does not replace runner visuals.

The loader now searches the imported scene tree recursively instead of assuming
the model is at a single wrapper depth. It tolerates importer-generated numeric
suffixes on the required pivot names and normalizes **only the instance's**
validated joints to the names expected by gameplay.

## Compatibility contract

```text
VisualRoot
├─ UpperBody (waist pivot)
│  ├─ Hips
│  ├─ Head
│  ├─ LeftArm
│  │  └─ Elbow
│  └─ RightArm
│     └─ Elbow
├─ LeftLeg
│  └─ Knee
└─ RightLeg
   └─ Knee
```

These pivots remain under the existing standard-zombie animation, damage,
dismemberment and corpse system. The code transfers the three validated imported
rig roots to `VisualRoot` and discards import-only wrapper nodes. It does not
replace the AI, collision, attacks, hit regions, physics or corpse logic.
No skeletal skinning is used; rigid limb posing can still reveal overlapping seams
at extreme animations.

Imported mesh names deliberately differ from procedural palette names, preserving
the authored textures. Imported art does not yet receive procedural colour variation.
Godot may generate mesh LODs on import; `ZM-PERF2B` runtime-generated
procedural LOD meshes do not replace the imported artwork's surfaces.

## Diagnosing an unchanged zombie

1. Pull `implement-td0-td1`, allow Godot to finish importing the GLB, and rebuild
   the C# solution.
2. Run `WORLD/FirstPerson/FirstPersonWorld.tscn` and check Godot's **Output**.
3. **Success:** `[ZombieArtwork] ZombieFirstPass.glb active ...` appears once.
4. **Fallback:** `[ZombieArtwork]` warning states if the GLB could not load, or
   prints the actual imported node paths if the expected rig was not found.
5. If the setting is false, only procedural visuals are intentionally constructed.
   The cached procedural template remains available even when the GLB is present.

Test nearby standing/walking poses, chase attacks, both arm and leg removals,
limping/crawling, death/corpses and the runner. Review performance with a comparable
crowd: V3 is substantially heavier than the earlier 4,832-triangle first-pass GLB.

**Legacy source warning:** `Source/build_zombie.py` generates the *old* 4,832-triangle
first-pass art and would overwrite the current V3 GLB if run unchanged. It is
retained as historical source/fallback reference, **not** the builder for V3.
The editable V3 model/texture package was produced separately.

## Verification status

The uploaded V3 glTF hierarchy has the named gameplay pivots; imported wrapper
structure, Godot engine behaviour, C# compilation and the living zombie's visual
and dismemberment states still require validation in the owner's Godot editor.
No change to the procedural C# builders or the runner was made in this loader fix.
