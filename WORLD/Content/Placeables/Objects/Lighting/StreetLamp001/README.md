# StreetLamp001 — physical reusable street-lamp fixture

**Status:** independent fixture and reusable lighting component wired; **Godot/.NET runtime validation pending**. It is now instanced four times in the reusable convenience-store site, but that site is not placed in the active world.

The physical lamp stays at `WORLD/Content/Placeables/Objects/Lighting/StreetLamp001/`; all real local lighting code, light nodes and lighting configuration now live under [ENVIRONMENT/Lighting/Fixtures/StreetLamp](../../../../../ENVIRONMENT/Lighting/Fixtures/StreetLamp/README.md).

## Physical model only

- `StreetLamp001.tscn` has `StreetLampMesh.cs` on its root and an **instanced** `ENVIRONMENT/Lighting/Fixtures/StreetLamp/StreetLampLighting.tscn` beneath a child named `Lighting`.
- `C/StreetLampMesh.cs` generates a tapered metal pole (~5.8m), collision base/post, projecting arm (~1.1m), lamp head and small LED panel. The model has no native `Light3D` nodes of its own.
- `Configuration/StreetLampSettings.cs` and `StreetLampMaster.tres` now control **only physical geometry and metal colours**: pole height, arm reach, pole paint and housing paint. Previous `SpotEnergy`, `FillEnergy`, `LightsEnabled` etc. have been moved to the lighting master, not deleted.
- The lamp's `_Ready` positions the reusable lighting child at the actual LED head coordinates and binds its per-instance LED material so the light component can adjust emission. It does not copy the global lighting master or run per-frame.
- Public `SetLampEnabled(bool enabled)` **still exists** on `StreetLampMesh` and forwards to its reusable lighting child. Site/night control code will not need a different lamp API.

## Reusable lighting

The child `StreetLampLighting.tscn` has one down-facing `SpotLight3D` (warm, 2.5 energy, 13m range, 51° half-cone) and one shadowless ambient `OmniLight3D` (0.35 energy, 8m range). The spotlight's shadows are off by default. All spot/fill settings, glow and fog contribution live in `ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres`. It can also be instanced under another physical luminaire. It does **not** create a second `WorldEnvironment`.

The `[Tool]` geometry and lighting components each configure once on `_Ready`, then disable `_Process` / `_PhysicsProcess`. Turning the lamp OFF removes the two active GPU lights and disables LED glow. ON still incurs up to **two native GPU lights per lamp**; shadows on multiple lamps are expensive, so keep the default OFF unless specifically selected for testing. There is no timer, flicker, dynamic radius activation, or time-of-day hook until later PL2.

## Local test

After pull and C# compile, open `StreetLamp001.tscn`. Confirm the child `Lighting` resolves as a subscene, its `Downlight` and `AmbientFill` appear and the emitter is at the head (not ground), with a downward beam. In a temporary dark test scene, compare the same coverage/colour with the previous lamp, then edit **`ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres`** rather than the world geometry master. Toggle `SetLampEnabled(false/true)`; OFF should remove both real light and LED emission. Confirm the pole/collision shape stays unchanged. Fog response requires Forward+ configured. These checks have **not been run in Godot**.

See [PL site implementation plan](../../../../Sites/ConvenienceStoreSite001/IMPLEMENTATION_PLAN.md) for the pending assembly/world integration.
