# StreetLamp001 artwork — physical geometry

Procedural art comprises one tapered pole, rigid base, short arm, metal luminaire and per-instance LED panel. No GLB or texture. The LED panel starts dark and is controlled through a material binding to the instanced lighting component from `ENVIRONMENT/Lighting/Fixtures/StreetLamp/`. Replacing the artwork should preserve the lamp-head anchor; `StreetLampMesh` still owns collision, while the reusable light component independently owns the two `Light3D` nodes.
