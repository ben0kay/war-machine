# Bed001.glb — original imported bed model

[Bed001.glb](Bed001.glb) is the canonical geometry for Bed001: **288 triangles, 7 material surfaces**, one model with wooden frame, mattress, sheet, pillows and coloured duvet/fold. The old procedural geometry from `C/BedMesh.cs` was converted to a static GLB and removed from runtime.

**Native bounds:** ~1.32m wide, 1.183m tall, 2.125m deep. Floor pivot Y=0, headboard at -Z, foot at +Z. Models are low-poly, matte and texture-free.

The glTF mesh primitives are intentionally ordered: 0=Wood, 1=WoodTrim, 2=Mattress, 3=Sheet, 4=Pillow, **5=BeddingMain**, **6=BeddingTrim**. `../BedVisual.cs` changes only surfaces 5/6 using per-instance material overrides to offer four bedding palettes without another GLB. If the GLB is re-exported, preserve this order or update its adapter.

Resize all default Bed001 scenes through `../Configuration/BedMaster.tres`: `SizeMultiplier = Vector3(X, Y, Z)`. The GLB source geometry stays untouched. See [Bed001 README](../README.md).
