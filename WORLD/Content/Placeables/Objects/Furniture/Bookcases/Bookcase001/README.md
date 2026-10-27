# Bookcase001 — weathered wooden bookcase

**Model complete; game implementation pending.** Original self-contained [Artwork/Bookcase001.glb](Artwork/Bookcase001.glb). No `.tscn`, collider or world placement.

**Design:** old walnut-toned timber; detailed chamfered upright frame, wood backing planks with seams, protruding cornice, six separate empty shelf boards with edging, four short feet and tiny brass accents. 7 embedded low-roughness? No: mostly *matte/high-roughness* PBR colour materials, **1,484 triangles**, no external textures. Bounds approximately **1.215m wide, 1.93m tall, 0.415m deep**. Ground pivot Y=0, shelves face +Z.

**Owner policy:** fixed **Object**, **cannot lift, push, interact or break**. Future books are separate physics Props; do not bake Book001 into this bookcase.

**Later gameplay prefab:** import GLB under `Bookcase001.tscn` with `StaticBody3D`; build thin colliders for every shelf and separate narrow uprights/back. **Never one large collision box filling empty shelf compartments**, or a thrown Book001 could not enter the shelf. Use this one canonical prefab when furnishing any building; ensure floor `contents` role and structural fading stay visual-only. Original GLB can be replaced later without changing interaction APIs.

See the [Placeables library](../../../../README.md).
