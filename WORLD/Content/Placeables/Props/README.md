# Props — player-liftable physical assets

Props can **eventually** be picked up into a floating carry position in front of the character, moved, dropped and thrown with mass/reach restrictions. They don't automatically go to inventory or need per-object animations.

The first mesh design is [Book001](Books/Book001/README.md). The book cannot yet be lifted, thrown or collide: `RigidBody3D`, grab controls and physics prefab remain future IX1–IX2 tasks. A future bucket or broom can reuse the same shared `INTERACTION/` logic. See [Placeables foundation](../README.md).
