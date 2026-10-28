# Doors — reusable non-pickup architectural objects

Door001 is a **physical**, non-pickup hinged object, not a general-purpose loose prop. Its leaf is a RigidBody3D constrained to a permanent static hinge point. See [Door001](Door001/README.md) for the current test implementation. DOOR2 handle pulling and IX3 cupboards remain future phases.

Door installation belongs to a building's existing **real aperture**. Do not make a rectangle of static collision across a doorway; reserve thresholds and the full swing arc before putting furniture nearby. Use the same prefab and physical simulation in top-down and FPS, with no per-door idle script.
