# Book001 — cloth-bound old hardback

**Original imported GLB retained unchanged.** [Book001.tscn](Book001.tscn) wraps [Artwork/Book001.glb](Artwork/Book001.glb) with an independent size master [Configuration/BookMaster.tres](Configuration/BookMaster.tres). **No physics/interactions yet; local validation pending.**

The imported book remains a muted-teal hardcover with page ridges and small gold details: **420 triangles**, eight PBR materials, approx. **0.245m W × 0.050m H × 0.318m L**. It lies flat at Y=0 with spine -X. No external texture files.

**Size tuning:** `SizeMultiplier = Vector3(1, 1, 1)`: X width, Y thickness and Z length. You could make a smaller paperback-like variation by e.g. `Vector3(0.85, 0.8, 0.85)`. This setting applies to all default Book001 instances and F1 previews on reload; duplicate a settings resource for an individual scene override. Imported model stays identical.

**Policy:** Future movable **Prop**, intended for shared carry/drop/throw; does not automatically enter inventory. The visual wrapper currently has no `RigidBody3D`, collision, gravity or pickup. Later physics should use a suitable simple box collider and mass that match chosen size; the visual-only scale adapter does not add these features.
