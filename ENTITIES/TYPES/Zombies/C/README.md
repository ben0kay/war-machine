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
