# Fixed reusable light fixtures

Physical lighting fixtures live in `Placeables/Objects/Lighting/` because they are fixed, non-pickup-able world objects. Their **models** are independent of the environments in which they appear; future real-light effects and flicker behaviours should reuse `ENVIRONMENT/Lighting/` when appropriate.

[StreetLamp001](StreetLamp001/README.md) is now a standalone pole/LED prefab with two native Godot lights: a downward spotlight and soft omni fill. Its geometry and lighting are implemented; no site/world placement or flicker controller exists yet.
