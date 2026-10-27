# Zombie procedural mesh

`ZombieMeshBuilder.cs` owns the **visual-only, low-poly zombie** generated in C#. The old all-box body is now made from shared rounded/tapered meshes: a slouched torso, narrower waist, head and jaw, uneven sleeves, exposed forearms, shaped trousers, articulated knee silhouettes and scuffed-looking shoes. Sunken eye sockets, dull eyes, a partly open mouth, ragged shirt details and restrained bruise/discolouration patches are intended to read as undead from the angled top-down camera.

A few muted combinations of skin, shirt and trousers are chosen per instance. Their low-resolution mottled textures are generated once and reused across zombies; shared primitive meshes/materials keep per-actor resource creation and idle work low. This is procedural art, not an imported model or a new rendering system. With large future zombie crowds, assess mesh-instance/draw-call counts and consider merged/LOD art if necessary.

**Animation and gameplay contract:** the existing `VisualRoot`, `LeftArm`, `RightArm`, `LeftLeg` and `RightLeg` pivots and forward -Z convention remain unchanged. New `LeftKnee`/`RightKnee` pivots receive a *small* alternating bend from the existing `ZombieController.Pose` shamble; no separate shoulder animation, timer or per-limb script. Collision remains the single zombie capsule, independent of visible pieces. Movement speeds, awareness, damage, terrain recovery and attack behaviour are unchanged.

`../Artwork/` remains reserved for future imported zombie models; a replacement should preserve/adapt the named rig pivots and character orientation.

**Brief checks for the owner:** `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn`, approach several zombies and inspect front/back/sides at typical zoom; verify face and shirt variation, no gaps between legs and hips, a restrained left/right knee bend while wandering/chasing, the familiar arms-out chase stance, no visual-collider interference, and disappearance upon death. No build or Godot runtime check was performed for this art pass.
