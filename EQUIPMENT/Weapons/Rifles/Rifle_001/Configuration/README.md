# Configuration — Rifle_001

When the rifle is implemented, create an actual editable **`RifleMaster.tres`** using a typed `RifleSettings.cs` Resource class stored in `../C/`.

Suggested inspector groups: **Weapon** (range, cooldown, damage), **Aiming** (optional turn speed/spread), **Visuals** (procedural mesh scale, colours, muzzle flash/tracer duration). Keep settings modest; duplicate the master at runtime before editing values so test-only changes never mutate authored defaults.

**No fake/empty master .tres** is committed during folder scaffolding; Codex creates valid resources when the rifle code is authorized.
