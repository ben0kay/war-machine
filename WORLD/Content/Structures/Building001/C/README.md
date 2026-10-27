# C — Procedural 3D mesh ONLY

`Building001Mesh.cs` procedurally assembles the entire Building001 model from reusable Godot primitives and two custom gable triangle meshes, with a single static collision body. `[Tool]` builds a preview in the editor after C# compilation; normal runtime builds once and becomes dormant.

**Owner convention:** `C/` is the temporary Godot C# **mesh-construction** folder. Place future gameplay, placement and interaction scripts outside `C/`. `../Artwork/` is reserved for actual imported models.
