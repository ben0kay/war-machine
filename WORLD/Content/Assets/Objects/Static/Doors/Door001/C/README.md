# HingedDoor001.cs

One-time runtime construction of a Godot HingeJoint3D between shape-less static HingeAnchor and the collidable rigid Leaf. Adds the panel, shape, two cosmetic handle plates, two handle markers and explicit AIM1 collision-to-visual binding. Marks animated leaf art so cached building occlusion uses the fixed wall while including the moving panels in grouped opacity. No _Process, _PhysicsProcess, auto-closing, click interaction or player/controller changes. See [parent documentation](../README.md).
