# Book001 — cloth-bound old hardback

**Model complete; game implementation pending.** Original self-contained [Artwork/Book001.glb](Artwork/Book001.glb). No `.tscn`, `RigidBody3D`, collider, item definition or world placement yet.

**Design:** muted teal cloth hardback with darker rounded-looking spine trim, inset cream page block and fine page-layer ridges, modest gold-coloured edge details and geometric cover accent. No copyrighted printed text, no external textures. **420 triangles, 8 simple PBR materials**. Approximate bounds **0.245m wide × 0.050m thick × 0.318m long**. Default lies flat on XZ, lower face at Y=0, spine toward -X.

**Owner policy:** a **Prop** intended to be held in front of the player using shared physics grab, dropped and thrown; not automatically inventoried or hand-equipped. Later wrap imported mesh in a single `RigidBody3D` with simple box collision, suitable physics-centre alignment, low mass/friction and sleep. After Bookcase001 gets separate shelf colliders, the thrown book may rest on a shelf. This is IX1/IX2 work, *not* already implemented.

See the [Placeables library](../../../README.md).
