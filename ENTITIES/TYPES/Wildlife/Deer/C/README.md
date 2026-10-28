# Deer procedural artwork

`DeerMeshBuilder.cs` is a `[Tool]` one-shot visual builder on the inherited `VisualRoot`. Local **-Z is forward**. It builds a low-poly brown horizontal body with a pale underside, four separate long legs/dark hooves, raised neck, elongated muzzle, two ears/eyes, a white tail and optional eight short antler rods. Geometry uses shared primitive meshes per species and a few materials per instance. No physics collider per hoof/antler; `Deer.tscn` owns a single capsule. No animator, skeletal rig, physics update or procedural texture work yet.

Inspect both top-down and FPS silhouette; future artwork may replace this visual without changing `DeerController` or the shared health/collision interface.
