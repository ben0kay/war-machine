# Placeables catalogue (future AI furnishing reference)

**Only the following models/prefabs currently exist.** Units are metres; +Y is up. **Model only** means no gameplay collision, scripts, or world placement.

| Asset | Class | Canonical file | Size and status |
| --- | --- | --- | --- |
| Bookcase001 | **Object** | [Bookcase001.glb](Objects/Furniture/Bookcases/Bookcase001/Artwork/Bookcase001.glb) | ~1.215m W × 1.93m H × 0.415m D; Y=0 ground pivot; front +Z. **Mesh only**, 1,484 tris, 7 colour/PBR materials. Fixed/non-breakable/noninteractive design. |
| Book001 | **Prop** | [Book001.glb](Props/Books/Book001/Artwork/Book001.glb) | ~0.245m W × 0.050m H × 0.318m D; lying flat, bottom Y=0, spine -X. **Mesh only**, 420 tris, 8 colour/PBR materials. Designed for future physics. |
| Car001 | **Object (legacy path)** | [Car001.tscn](../Objects/Cars/Car001/Car001.tscn) | Working static car under old path; no migration performed. |

For future prefab-integrated assets, record capability, static/rigid collision shapes, mass, local axes/transform, interaction policy, placement clearance and variation settings. Dense decorative clutter should not be awake rigid physics by default.

**Visual inspection:** Both current mesh-only designs are available through the F1 debug preview menu (see `DEBUG/PlaceableSpawner/README.md`). Previewing adds no collider, interaction or reusable gameplay scene.
