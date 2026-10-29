> **ZM-PERF2B:** Native lower-polygon mesh LOD is now source implemented for both rigs with unchanged pivots, material palette, mesh-node count and full near detail. Original/template-copy actors and corpses share geometry; no managed distance loop. Local compilation/render/profiling pending. [Settings and checks](../MESH_LOD_README.md).

> **ZD4 elbows:** Both procedural rigs now keep forearm/hand art beneath an identity `Elbow` pivot at shoulder-local Y=-0.27m before template batching. Original world placement, palette selection, shoulder gait and hit regions remain. Living elbows do not process; death physics alone articulates them. Check first/cached spawns, sleeve seams, severing and close FPS appearance locally. Previous mesh counts describe the earlier grouping.

> **ZM-PERF2A API fix:** primitive-type validation now calls `SurfaceGetPrimitiveType` on `ArrayMesh`, where Godot exposes it, rather than the base `Mesh`. Existing procedural primitive sources remain triangles. Source review passed; compilation/runtime still require local Godot validation.

> **ZM-PERF2A — combined same-pivot, same-material meshes (source implemented; local validation pending).** Standard zombie now uses shared single-surface geometry for compatible torso/shirt, face, arm and detail pieces. The first zombie builds and batches the immutable template; later zombies duplicate its cached pivot/mesh hierarchy without rebuilding mesh vertex arrays or material resources. Every named Head/arm/leg/Knee/LowerLegShape/Ankle joint is preserved. The 64px appearance palette still resolves by explicit `SkinBatch`, `FaceBatch`, `ShirtBatch`, `PantsBatch` category names. The earlier ZM-PERF2A pass did not include LOD; ZM-PERF2B is now source implemented as described above.

> **ZM-PERF1 — source implemented, awaiting in-engine FPS/quality measurements (2026-10-28):** Standard zombie reduced **42 → 29 visible mesh instances** by removing tiny accessory meshes and slightly extending the original hand/shoe shapes. All shoulders, knees, hit regions, corpse pivots, seeded material variation, collision and AI remain unchanged. No measured frame-time improvement claimed. See the latest verification notes below.

# Zombie procedural mesh

`ZombieMeshBuilder.cs` owns the **visual-only, low-poly zombie** generated in C#. The old all-box body is now made from shared rounded/tapered meshes: a slouched torso, narrower waist, head and jaw, uneven sleeves, exposed forearms, shaped trousers, articulated knee silhouettes and scuffed-looking shoes. Sunken eye sockets, dull eyes, a partly open mouth, ragged shirt details and restrained bruise/discolouration patches are intended to read as undead from the angled top-down camera.

A few muted combinations of skin, shirt and trousers are chosen per instance. Their low-resolution mottled textures are generated once and reused across zombies; shared primitive meshes/materials keep per-actor resource creation and idle work low. This is procedural art, not an imported model or a new rendering system. With large future zombie crowds, assess mesh-instance/draw-call counts and consider merged/LOD art if necessary.

**Animation and gameplay contract:** the existing `VisualRoot`, `LeftArm`, `RightArm`, `LeftLeg` and `RightLeg` pivots and forward -Z convention remain unchanged. New `LeftKnee`/`RightKnee` pivots receive a *small* alternating bend from the existing `ZombieController.Pose` shamble; no separate shoulder animation, timer or per-limb script. Collision remains the single zombie capsule, independent of visible pieces. Movement speeds, awareness, damage, terrain recovery and attack behaviour are unchanged.

`../Artwork/` remains reserved for future imported zombie models; a replacement should preserve/adapt the named rig pivots and character orientation.

**Brief checks for the owner:** `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn`, approach several zombies and inspect front/back/sides at typical zoom; verify face and shirt variation, no gaps between legs and hips, a restrained left/right knee bend while wandering/chasing, the familiar arms-out chase stance, no visual-collider interference, and disappearance upon death. No build or Godot runtime check was performed for this art pass.

## ZMAT0 shared appearance palette

`ZombieAppearance.cs` caches 28 low-resolution procedural textures once for **both** `ZombieMeshBuilder` and `Zombie_002/C/ZombieRunnerMeshBuilder`. It generates dirty, mottled skin/fabric with some small dried-blood areas and independently selects skin, face, shirt and trouser variants. `ZombieController.InitializeTarget` reassigns materials from the existing stable spawn seed; neither builder makes new textures per zombie, and existing gait/eye/wound props aren't touched.

The body-part names switched by `ZombieAppearance.Apply` are deliberately explicit; when adding new zombie artwork, add its names to the category switch or pass material references from the builder to avoid accidentally treating bruises or eyes as skin. No procedural shaders or dynamic decals are added. See [../README.md](../README.md) for resource and brief gameplay test notes.

## More human-like model pass

Shambler 001 now shares closed, cached ten-sided anatomical meshes with Zombie 002 in `ZombieHumanGeometry.cs`. Shaped shoulder-to-waist torso, tapered cheek/jaw/skull, slimmer forearms, palms with two tiny hanging fingers, angular knee joints and less balloon-like shoes replace many sphere silhouettes while preserving original six animation pivots, collision and height. Pale exaggerated eyes have been reduced to recessed grey slits and the former ball of hair has a thin angular profile. A mild left-leg bend and mismatched arm/step amplitudes make the shamble less symmetrical; no extra animation clock or game AI change.

Each profile has a complete capped hull, Godot-facing triangle winding and UVs for the existing shared spawn-seeded 64×64 skin/clothing variation; source review is not a substitute for local rendering. Check both FPS and top-down closeups for hollow faces, clipped materials, arm/knee gaps and toe clearance; verify F7/chase/health/terrain remain unchanged. Build locally; no .NET or Godot renderer was available for this change.


## ZM-PERF1: simplified visual parts (2026-10-28)

**Scope:** Visual mesh construction only. Normal shambler 001 creates **29 visible mesh instances instead of 42**. The removed pieces are: four standalone fingers, two pin-sized eyes, two raised arm bruises, two separate shoe toes, a fabric-tear prop, a temple-bruise prop and the separate hair puff. The hands and shoes were enlarged slightly so their silhouettes remain readable; the skull, sockets, jaw, nose, mouth, decayed clothing, waist, articulated arms/legs and all named pivots remain.

Geometry and material **resources stay shared**, and the existing `ZombieAppearance` 28 shared 64×64 skin/face/shirt/trouser textures remain unchanged. There are no new meshes baked per actor, scripts processing per part, shaders, physics queries, or extra `_Process` work. All `ZombieAnatomy` severable limb subtree membership and existing hidden stumps remain. Existing corpses inherit the reduced art without changing lifecycle. FPS and top-down use the same visuals.

**Local acceptance:** Build `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`. With the same 10–15-zombie crowd, camera, world state and render settings compare baseline vs ZM-PERF1: visible meshes/draw calls, triangles, frame time/FPS and the existing hide-visuals/shadow toggles. Inspect front/back and closer FPS face/hand/shoes, gait and limb amputation, corpse fall/pile and appearance variation. **No .NET/Godot build or FPS profiling was available in the authoring session**, so any performance improvement remains unverified. If draw calls fall but FPS stays similar, investigate ZM-PERF2 batches/LOD next rather than stripping core character anatomy.


## ZM-PERF2A — single-surface, per-joint geometry batching

`ZombieVisualBatcher.cs` builds each batch only on the **first source mesh construction for a species** and stores an immutable template tree (one for shambler, independently one for runner). It groups **direct meshes under the same animation pivot** by shared palette category or exact detail material identity. Groups of two or more pieces are merged into **one ArrayMesh with one triangle surface and one MaterialOverride**, baking each piece's position, rotation, normal and UV coordinates. Single pieces are left alone. This is not merely a Node3D parenting optimization: compatible surfaces really become a single draw-submittable surface.

Later instances copy the cached template's lightweight Node3D/mesh hierarchy, sharing the same geometry/material resource references. Their own stable world seed still calls `ZombieAppearance.Apply` and changes the same explicit material categories as before; detail groups (bruise, dark mouth, boots) retain their static materials. No per-frame geometry generation, shader updates, additional callbacks, or extra engine threads. Head, shoulders, hips, knees and other named joints remain identical and are cached by anatomy *after* batching. Severed-limb visibility and corpse transfer retain the original hierarchy.

**Validate locally:** `dotnet build war-machine.csproj`; open `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`. Compare a fixed zombie count/camera with ZM-PERF1: mesh instances/draw calls (actual renderer, not simple mesh-node count), FPS and frame time. Inspect faces/UV blood, sleeves and hands, front/back, normal vs runner, animation, injury/severing and corpse handoff. Confirm first and subsequent spawns both render identical shape with independent seeded clothing. This change was source reviewed only; **compilation and FPS improvement have not been verified**. See the current ZM-PERF2B handoff above for native distance detail.

## CM5-A shared upper-body attack pivot

`ZombieUpperBodyRig.Attach` runs **once per species** before the existing visual batching template is captured; the new `UpperBody` pivot sits at waist height and parents the pre-existing torso, head and arm pieces with world transforms preserved. The small waist/hip shapes move with the torso so its anatomy region still covers them; both legs remain under the original art root. New zombies reuse the cached batched mesh template with a single extra transform node, no per-frame mesh regeneration, no additional materials and no mesh-builder duplicate animation code. The controller's shared `ZombieAttack` is the only visual attack driver. Anatomy torso bounds now follow `UpperBody`, limb/head pivots stay named and independently tracked. Local render/anatomy/corpse checks pending.

Standard ZombieMeshBuilder first attempts the configured imported artwork via `../Artwork/ZombieImportedVisual.cs`; procedural generation/batching remains the fallback. See the Artwork README for compatibility and validation.
