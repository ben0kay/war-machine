# Props — player-liftable physical assets

Props can **eventually** be picked up into a floating carry position in front of the character, moved, dropped and thrown with mass/reach restrictions. They don't automatically go to inventory or need per-object animations.

The first mesh design is [Book001](Books/Book001/README.md). The book cannot yet be lifted, thrown or collide: `RigidBody3D`, grab controls and physics prefab remain future IX1–IX2 tasks. A future bucket or broom can reuse the same shared `INTERACTION/` logic. See [Placeables foundation](../README.md).

## Authoring future Prop GLBs

Use the same canonical [OBJECT_AUTHORING_GUIDE.md](../OBJECT_AUTHORING_GUIDE.md) for new art, size masters, naming, variants and F1 previews. **Choose the SOC0 mode deliberately:** future physically held/tippable objects should normally keep `CollisionMode = 0` (None) until IX1/IX2 adds the **appropriate RigidBody3D and primitive collision shapes**. SOC0 AutoBox/CustomBoxes generate fixed `StaticBody3D` obstacles, **not** pushable physics objects. A fixed decorative wheelie bin could instead be a Static object with AutoBox. Inventory `ItemDefinition`/stacks and full physical item identity are separate gameplay systems.
