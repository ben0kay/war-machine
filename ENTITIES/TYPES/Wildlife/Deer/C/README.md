# Deer procedural artwork

`DeerMeshBuilder.cs` is a `[Tool]` one-shot visual builder on the inherited `VisualRoot`. Local **-Z is forward**. It builds a low-poly brown horizontal body with a pale underside, four separate long legs/dark hooves, raised neck, elongated muzzle, two ears/eyes, a white tail and optional eight short antler rods. Geometry uses shared primitive meshes per species and a few materials per instance. No physics collider per hoof/antler; `Deer.tscn` owns a single capsule. No animator, skeletal rig, physics update or procedural texture work yet.

Inspect both top-down and FPS silhouette; future artwork may replace this visual without changing `DeerController` or the shared health/collision interface.

## Proportion update (2026-10-28)

The low-poly art is now deer-proportioned rather than a cylindrical cartoon: shorter shallower body, higher shoulders, slimmer tapered forward neck, smaller face and muzzle, narrower legs and hooves, slightly larger flatter ears, tiny tail. A less saturated brown coat and no-antlers default make an ordinary doe-like deer; small stag antlers can still be enabled. Names of the four existing leg pivots remain unchanged, so AI gait keeps working. Same one-time procedural build, no additional animated scripts or physics.
