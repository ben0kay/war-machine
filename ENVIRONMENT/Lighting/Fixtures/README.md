# Reusable static and local light fixtures

All real light nodes, their configuration, light on/off state and future flicker/night control belong under `ENVIRONMENT/Lighting`. Physical world props are separate and may instance these lighting scenes as children at the point of emission.

- [StreetLamp](StreetLamp/README.md) — reusable steady spotlight plus softer omni fill and optional LED-panel emission; physical post remains `WORLD/Content/Placeables/Objects/Lighting/StreetLamp001/`.

This is not a competing global `WorldEnvironment` or a new per-frame lighting manager. Effects such as `Effects/MuzzleFlash` and future campfire lights still have their own activation lifecycles.
