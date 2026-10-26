# World vegetation preview

Trees live in [Trees/](Trees/). This is a small preview layered over the existing GPU grass from `res://VEGETATION/Grass/`; it doesn't replace that system.

The tree placement reads Terrain3D's **saved** heights directly and never changes the region or its legacy instancer data. Trees are streamed as chunked Godot MultiMeshes when the explorer crosses chunk boundaries, with zero per-frame C# tree processing while idle. This placement bridge is deliberately world-local until a later dedicated vegetation phase decides how to unify it.
