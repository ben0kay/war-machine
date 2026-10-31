# STG1 imported grass runtime shader

`StylizedGrass.gdshader` is used by chunk-batched copies of the **uploaded** ForestGrass001 material; the original `ASSETS/.../forest_grass_001_wind.gdshader` and texture atlases are unchanged. Opaque two-sided geometric blades, texture atlas + vertex colours, light wind in the vertex shader, and the real Godot project-wide `wm_foliage_tint` and `wm_foliage_tint_strength` uniforms. No particle shader, alpha card transparency or per-clump uniform writes.

Shared tint uses luminance-preserving blending, modulated by a grass-local `palette_response`, so setting strength=0 returns authored atlas/mesh colour. The stylized pine and DF2 canopy foliage have their own materials/shaders using the same global palette. Global values are declared in `project.godot` and editable in Godot's Project Settings → Shader Globals.
