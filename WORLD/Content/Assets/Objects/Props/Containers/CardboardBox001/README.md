# CardboardBox001 — closed small kraft cardboard box

**Class:** Prop; intended for IX1 movable/carry/drop/throw experiments, but currently visual only.
**Canonical scene:** [CardboardBox001.tscn](CardboardBox001.tscn). F1 auto-discovers this scene under Props.
**Artwork:** [CardboardBoxVisual.cs](CardboardBoxVisual.cs) creates four simple BoxMesh nodes once (body, top tape, two short flap-end lines). No GLB or external textures. All default instances share three opaque rough materials. Approx. **48 triangles, 4 mesh surfaces/nodes**, no animation or idle process.
**Native size:** 0.42m X width × 0.26m Y height × 0.30m Z depth; +Y up, floor/rest pivot at Y=0; symmetric front/back (+Z/-Z).
**Settings:** [CardboardBoxMaster.tres](Configuration/CardboardBoxMaster.tres) uses shared `PlaceableSizeSettings`: SizeMultiplier and ShadowMode (0 Inherit, 1 Disabled, 2 Enabled). The shared `PlaceableVisual` owns sizing and runtime shadow policy. No independent asset manager or per-frame callbacks.
**Collision:** SOC0 **None / CollisionMode=0**. No static collision or physics body; player/zombies currently pass through. This is deliberate: IX1 must add a correctly sized primitive box collider to its future RigidBody3D/carry mechanism, rather than stack static and dynamic colliders. No claims of pickup/throw functionality yet.
**Limitations:** The top is not physically folded, no interior/open variant or texture wear. Four box meshes create four surfaces (not a single draw call); sufficient for a first-pass F1 visual preview, revisit for high-volume clutter.
**Validation:** Pending local Godot C# compile and F1 visual check. Verify appearance, scale, shadows Disabled/Enabled and absence of static collider before connecting IX1.
