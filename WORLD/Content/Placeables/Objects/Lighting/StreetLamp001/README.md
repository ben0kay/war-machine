# StreetLamp001 — future reusable streetlight prefab

**Status: scaffold only, not built, lit or world-placed.**

`StreetLamp001.tscn` is a valid empty Node3D, `C/StreetLampMesh.cs` and `Configuration/StreetLampSettings.cs` are empty C# placeholders, and `Configuration/StreetLampMaster.tres` is a generic Resource placeholder.

Future model: single lamppost with pole, base, arm/lamp head, authored collision and optional bulb emissive surface. Future lighting: localized shadow-conscious `OmniLight3D` or suitable `SpotLight3D` with a steady mode and an opt-in flickering variant. **Only one selected lamp should flicker** for the convenience-store site; the streetlamp prefab must also work at other locations. Keep shared lighting lifecycle/relevance under `ENVIRONMENT/Lighting/` and do not create a global time/sky system here.

No model, physics, light, flicker timer, signal, shadow, asset, materials, C# logic or world placement exists in this scaffold. Lighting work is part of PL2, not PL0A.
