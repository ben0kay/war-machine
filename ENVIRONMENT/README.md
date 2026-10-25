# ENVIRONMENT — Shared atmosphere

`Sky/WildernessSky.tscn` adapts only Sky3D's environment and four relevant child nodes from its reference scene. No city, reference camera, demo UI, reflection probe or competing static sky is included. Sky3D owns sun, moon, sky, clouds and its timer.

`Configuration/EnvironmentMaster.tres` / `EnvironmentConfig.cs` own start hour, day length, accelerated preview, update interval, initial pause and shared grass wind. `Sky/SkyController.cs` applies these once after initialization. **P** pauses/resumes solar time; **F8** toggles a one-minute day. Pausing solar time intentionally leaves clouds and grass moving. Editor time is disabled to keep a stable authoring preview.

No custom per-frame sky loop exists. Sky3D updates time on its 0.1-second timer while running; pausing stops that timer. Sky3D cloud callbacks and shared GPU grass animation are intentional active visual work. Grass and clouds currently use separate shader motion models; coherent broader weather/wind belongs to WE4/WE6. No weather, forest or streaming framework is introduced. **Current SkyDome settings include `fog_density = 0.01`; the on-screen fog appearance has not yet received owner visual acceptance.**

Sky3D 2.1 is MIT licensed; retain `addons/sky_3d/LICENSE.txt` and its third-party texture license files. The supplied sky scene references those original assets. See `WORLD/WE0_WE1_VALIDATION.md` for tested versus pending checks.

## Planned atmosphere direction (not implemented)

**Persistent, eerie mist is the default world mood**, not a rare rain/storm effect. Use Sky3D's existing atmospheric distance fog as the global baseline, with cold neutral/blue-grey depth: foreground terrain and foliage remain readable, intermediate forest fades progressively, and distant hills or future large machines become silhouettes before revealing their detail. Avoid uniform opaque fog or using short draw distances disguised by haze.

- **WE2:** Confirm the actual visual baseline in `testworld.tscn`, tune existing Sky3D fog to be clearly misty most of the time, and expose a few useful data-driven settings (density, falloff/start/end and optional comparison control) in the existing `EnvironmentMaster.tres` / `EnvironmentConfig.cs` rather than an independent manager.
- **WE3–WE4:** Design distant terrain and later tree/grass LOD transitions against both default mist and temporarily clearer air; a fog transition cannot hide broken streaming.
- **WE5:** Refine day/time-responsive mist and, if worthwhile, add low-lying valley/cave fog pockets with built-in volumetric FogVolume3D after visual/performance checks. Sky3D remains the only sky/light/WorldEnvironment owner.
- **WE6:** Weather can temporarily deepen or lighten the haze, but returning to normal restores the misty baseline.

Keep static settings event-driven and dormant, avoid duplicated competing global fog effects or unnecessary per-frame C# updates, and profile expensive volumetrics before enabling them broadly. **No robots or enemies are authorized by this visual direction.** See [environment implementation plan](../WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md) for future phase scopes and pass criteria.
