# StreetLamp001 — physical reusable street-lamp fixture

**Status:** independent fixture and reusable lighting component wired; **Godot/.NET runtime validation pending**. It is now instanced four times in the reusable convenience-store site, and that site is placed in the active village.

The physical lamp stays at `WORLD/Content/Assets/Objects/Static/Lighting/StreetLamp001/`; all real local lighting code, light nodes and lighting configuration now live under [ENVIRONMENT/Lighting/Fixtures/StreetLamp](../../../../../../../ENVIRONMENT/Lighting/Fixtures/StreetLamp/README.md).

## Physical model only

- `StreetLamp001.tscn` has `StreetLampMesh.cs` on its root and an **instanced** `../ENVIRONMENT/Lighting/Fixtures/StreetLamp/StreetLampLighting.tscn` beneath a child named `Lighting`.
- `C/StreetLampMesh.cs` generates a tapered metal pole (~7.0m), collision base/post, projecting arm (~1.1m), lamp head and small LED panel. The model has no native `Light3D` nodes of its own.
- `Configuration/StreetLampSettings.cs` and `StreetLampMaster.tres` now control **only physical geometry and metal colours**: pole height, arm reach, pole paint and housing paint. Previous `SpotEnergy`, `FillEnergy`, `LightsEnabled` etc. have been moved to the lighting master, not deleted.
- The lamp's `_Ready` positions the reusable lighting child at the actual LED head coordinates and binds its per-instance LED material so the light component can adjust emission. It does not copy the global lighting master or run per-frame.
- Public `SetLampEnabled(bool enabled)` **still exists** on `StreetLampMesh` and forwards to its reusable lighting child. Site/night control code will not need a different lamp API.

## Reusable lighting

The child `StreetLampLighting.tscn` has one down-facing `SpotLight3D` (warm, 4.5 energy, 20m range, 63° half-cone) and one shadowless ambient `OmniLight3D` (0.75 energy, 12m range). The spotlight's shadows are off by default. All spot/fill settings, glow and fog contribution live in `../ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres`. It can also be instanced under another physical luminaire. It does **not** create a second `WorldEnvironment`.

The `[Tool]` geometry and lighting components each configure once on `_Ready`, then disable `_Process` / `_PhysicsProcess`. Turning the lamp OFF removes the two active GPU lights and disables LED glow. ON still incurs up to **two native GPU lights per lamp**; shadows on multiple lamps are expensive, so keep the default OFF unless specifically selected for testing. There is no timer, flicker, dynamic radius activation, or time-of-day hook until later PL2.

## Local test

After pull and C# compile, open `StreetLamp001.tscn`. Confirm the child `Lighting` resolves as a subscene, its `Downlight` and `AmbientFill` appear and the emitter is at the head (not ground), with a downward beam. In a temporary dark test scene, compare the same coverage/colour with the previous lamp, then edit **`../ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres`** rather than the world geometry master. Toggle `SetLampEnabled(false/true)`; OFF should remove both real light and LED emission. Confirm the pole/collision shape stays unchanged. Fog response requires Forward+ configured. These checks have **not been run in Godot**.

See [PL site implementation plan](../../../../Sites/ConvenienceStoreSite001/IMPLEMENTATION_PLAN.md) for the pending assembly/world integration.

### Height and coverage adjustment — 2026-10-28

The shared physical fixture default is now **7.0m high** (previously 5.8m). All four convenience-store site lamps inherit that default automatically. The real light settings remain in `ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres`, not inside this physical geometry prefab. Spotlight intensity/range/cone are now 4.5 / 20m / 63° half-angle; dispersed ambient fill is 0.75 / 12m. This produces a noticeably broader, brighter lit area with shadows still OFF. Head position uses pole height directly, so light and pole move together. Reopen/reinstance the prefab in Godot after pulling and rebuilding to refresh `[Tool]` art and lighting.
