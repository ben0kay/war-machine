CaveSettings owns editable shape/location/light settings. WildernessCave builds one mesh passage, surrounding rock shell, static colliders and crystal lights. It uses the existing shared geometry helper and stone material, and never writes Terrain3D data.

- `CaveRouteBuilder.cs`: one-time seeded macro passage bends and grade-limited path samples.
- `WildernessCave.Fork.cs`: shared-port-preserving passage chambers at cached ring profiles.
