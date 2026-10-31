# Pine foliage shader ownership

- `LeafWind.gdshader`: existing procedurally generated pine needle wind and per-tree camera occlusion. It uses per-leaf UV.y attachment weights and a shared shader material for all generated variants.
- `ImportedPineFoliage.gdshader`: **PineTree001.glb forest comparison**: one shared material per foliage colour surface (3 total); uses native `INSTANCE_CUSTOM.r` stipple coverage fade to match the overhead camera-occlusion path. Subtle height-weighted world-space wind uses the same shared wind direction/strength/speed values. Bark remains original opaque PBR. It does not modify the imported GLB asset or F1 preview.

Both shaders are used in `PinePlacement`'s existing `MultiMesh` batches. No per-tree scene nodes or CPU-frame callbacks. Zero wind bypasses displacement. GLB wind is an approximation (height-driven canopy sway, not individual imported leaf tip weights) and has no bespoke distant imported mesh; validate in Godot before treating appearance/performance as production-ready.

## Stylized PineTree001 runtime adapter (PINE-ST1)

`StylizedPineFoliage.gdshader` is used only by the **world's seeded stylized-pine batches**. Loads source vertex colour tint parameters from `ASSETS/Vegetation/Trees/PineTree001/Materials/PineTree001Foliage.tres`, applies palette/luminance matching, keeps wind tied to existing weather sliders, and supports ordered top-down per-tree fade through `INSTANCE_CUSTOM.r` on **LOD1, LOD2 and LOD3**. Bark uses the pack's unchanged material; preview pack shaders are not edited. One shared shader material per forest, zero per-tree CPU callbacks.
