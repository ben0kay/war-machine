# Lighting — reusable local illumination

> **Status: scaffold only (2026-10-27).** This folder documents future local point-light effects and contains one Inspector-editable `LightingSettings` master. **No OmniLight3D, LightEffects runtime, fire, weapon hook or lighting-manager behaviour has been implemented.** Settings are not read by gameplay yet.

## Purpose and boundaries

War Machine is an angled top-down 3D survival game, with a first-person debug scene. Local sources should make nearby terrain, characters and vegetation respond visibly to **real light** at night, independently from the existing shader-only muzzle flash and distant fog. Do not replace or compete with Sky3D's sun, moon or WorldEnvironment. Keep shader muzzle glow/short tracers and local real illumination distinct.

## Folder map

```text
ENVIRONMENT/Lighting/
  README.md
  IMPLEMENTATION_PLAN.md
  LightingSettings.cs
  Configuration/
    LightingMaster.tres            # authoring defaults, not yet consumed
    README.md
  Shared/
    README.md                     # future reusable light behaviours and budgets
  Effects/
    MuzzleFlash/
      README.md                   # next: transient firearm light
  Sources/
    Campfire/
      README.md                   # future: persistent flickering firelight
```

**Recommended ownership:** generic lifecycle/relevance helpers belong under `Shared/`; the weapon retains authority over when a shot occurs and can request a small, optional muzzle light; an eventual campfire entity/prop owns whether it is burning. Do **not** put firearm cooldowns/damage logic into environment lighting. If a common effect is reusable later, implement it once rather than giving each prop a competing update system.

## Data and expected behaviour

`Configuration/LightingMaster.tres` contains future global lighting enable, a suggested maximum of four simultaneous **transient** lights, and muzzle-light colour/energy/range/duration. These are **design defaults, not active runtime limits**. A future feature should duplicate the master for runtime editing; weapon-specific overrides (e.g. suppressor, machine gun) can live with that firearm's settings. Do not add campfire values to this master until the campfire implementation is authorized; campfire may need a separate reusable scene/resource.

- **Transient / muzzle flash:** brief `OmniLight3D`, no dynamic shadows, lit only on accepted firing. Reuse cached node/trigger lifetime; completely disable/stop after ~0.07s, including unequip/focus loss. Audio and AI sound perception remain separate.
- **Persistent / campfire (future):** warm localized `OmniLight3D`, gentle irregular flicker while actively burning and near the player, optional expensive shadows only when relevant, stopped/off when extinguished or unloaded. Fire visuals, fuel, heat, damage and sound are separate mechanics.
- **Future examples:** handheld torches, streetlights, house lamps, robots, generator panels, vehicle lights and electrical sparks. Share a small budget/relevance policy rather than permanently illuminating distant unloaded chunks.

## Performance requirements

Avoid per-frame C# loops when a light is not active. Reuse nodes, materials and timers; transient effects should only process for their very short lifetime. No runtime resource scans or per-shot allocations. Avoid real-time shadow rendering for gun flashes. Limit simultaneously shadowed persistent point lights separately when needed. Visible attenuation, colour and fog contribution should be checked in Godot Forward+, particularly at night and with Ground Haze 2.0 enabled.

**Next authorized work is not yet specified here:** use [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) as a Codex handoff, with **LI0 muzzle illumination first**, and campfire explicitly deferred. No gameplay, rendering or audio paths are changed by this scaffold.
