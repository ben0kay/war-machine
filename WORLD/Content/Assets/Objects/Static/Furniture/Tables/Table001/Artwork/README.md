# Table001 imported artwork

[Table001.glb](Table001.glb) is a genuine binary glTF 2.0 model: original stylized six-seat rectangular wooden dining table, **1.82m wide (X) × 0.76m high (Y) × 0.944m deep (Z)**. Ground plane at **Y=0** is the placement reference; four feet bottom surfaces start at Y=0.016m so they sit just above the authored ground plane.

**Geometry:** Five beveled tabletop planks with slight gaps and subtle alternating tones; reinforced underslab; four octagonal tapered legs; wooden side/end aprons; under-top centre brace; eight tiny muted brass pegs. No chairs, dishes, books or other separately placeable objects are baked in.

**Optimized material surface order:**
1. `TopPlanks` — main planks
2. `TopAlternate` — alternating planks
3. `WoodFrame` — legs/aprons/underslab
4. `Hardware` — muted metal pegs

GLB contains **532 triangles and 4 material groups** in one mesh, with no external bitmap textures, animation or physics. `../TableVisual.cs` applies shared material overrides on surfaces 0–2 only for WeatheredOak, DarkWalnut and FadedGrey. The fourth surface stays the same. Re-exporting must preserve this material-surface contract or update the adapter.

The model is deliberately independent of Godot runtime mesh building. Its size is adjusted using `../Configuration/TableMaster.tres`; no alternate GLBs are needed for wood-colour changes.
