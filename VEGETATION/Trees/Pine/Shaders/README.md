# Pine foliage shader ownership

- `LeafWind.gdshader`: existing procedurally generated pine needle wind and per-tree camera occlusion. It uses per-leaf UV.y attachment weights and a shared shader material for all generated variants.
- `ImportedPineFoliage.gdshader`: **PineTree001.glb forest comparison**: one shared material per foliage colour surface (3 total); uses native `INSTANCE_CUSTOM.r` stipple coverage fade to match the overhead camera-occlusion path. Subtle height-weighted world-space wind uses the same shared wind direction/strength/speed values. Bark remains original opaque PBR. It does not modify the imported GLB asset or F1 preview.

Both shaders are used in `PinePlacement`'s existing `MultiMesh` batches. No per-tree scene nodes or CPU-frame callbacks. Zero wind bypasses displacement. GLB wind is an approximation (height-driven canopy sway, not individual imported leaf tip weights) and has no bespoke distant imported mesh; validate in Godot before treating appearance/performance as production-ready.
