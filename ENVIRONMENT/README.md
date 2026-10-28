# ENVIRONMENT — current shared prefab

**Planned interior effect exclusions:** [INTERIOR_EXCLUSION_IMPLEMENTATION_PLAN.md](INTERIOR_EXCLUSION_IMPLEMENTATION_PLAN.md) — IE0–IE4: shared building interior/shelter volumes, local ground-haze exclusion in both views and **first-person-only rain hiding**. Top-down rain remains unchanged, including perspective overhead. Plan only.


Day/night fog, readable shared ambient lighting, FPS sky visibility and adjustable sun/moon volumetric rays are implemented, **awaiting local validation**. See the [day/night guide](SHARED_ENVIRONMENT_README.md#day--night-lighting-and-rays--awaiting-local-validation). Use Environment → Lighting / atmosphere in either shared debug overlay. The historical fixed-fog/WE5 notes below predate this implementation.

Use [Environment.tscn](Environment.tscn) for the active top-down and FPS test views. See [shared environment guide](SHARED_ENVIRONMENT_README.md) for scene ownership, shared defaults, initialization and testing. Both views now share ground haze as well as Sky3D/weather. Existing wilderness APIs remain supported. Current FPS controls are one left-hand expandable panel; F8/F9 instructions below are historical.

# ENVIRONMENT — Shared atmosphere

`Sky/WildernessSky.tscn` adapts only Sky3D's environment and four relevant child nodes from its reference scene. No city, reference camera, demo UI, reflection probe or competing static sky is included. Sky3D owns sun, moon, sky, clouds and its timer.

`Configuration/EnvironmentMaster.tres` / `EnvironmentConfig.cs` own start hour, day length, accelerated preview, update interval, initial pause shared grass wind, and Sky3D mist. `Sky/SkyController.cs` applies these once after initialization. **P** pauses/resumes solar time; **F8** toggles a one-minute day. Pausing solar time intentionally leaves clouds and grass moving. Editor time is disabled to keep a stable authoring preview.

No custom per-frame sky loop exists. Sky3D updates time on its 0.1-second timer while running; pausing stops that timer. Sky3D cloud callbacks and shared GPU grass animation are intentional active visual work. Vegetation consumes the master wind settings in its GPU ribbon simulation; clouds retain Sky3D's own motion model. Coherent broader weather/wind belongs to WE4/WE6. No weather, forest or streaming framework is introduced. **Current baseline: FogDensity=0.025, FogStart=0m, FogEnd=200m, FogFalloff=3, enabled.** These are applied to the existing SkyDome once by `SkyController.ApplyMist()` and serialized into WildernessSky for a matching editor preview. The fog shader reaches its distance weight at 200m while exponential density/falloff still determine final opacity. Software Vulkan captures were inspected; target-PC/user acceptance remains pending.

Sky3D 2.1 is MIT licensed; retain `addons/sky_3d/LICENSE.txt` and its third-party texture license files. The supplied sky scene references those original assets. See `WORLD/WE0_WE1_VALIDATION.md` for tested versus pending checks.

## Atmosphere direction and remaining refinement

**Persistent, eerie mist is the default world mood**, not a rare rain/storm effect. Use Sky3D's existing atmospheric distance fog as the global baseline, with cold neutral/blue-grey depth: foreground terrain and foliage remain readable, intermediate forest fades progressively, and distant hills or future large machines become silhouettes before revealing their detail. Avoid uniform opaque fog or using short draw distances disguised by haze.

- **Current authorized refinement:** Data-driven close mist is implemented through EnvironmentMaster and the existing SkyController. FogEnabled/density/start/end/falloff apply at launch or explicit ApplyMist; no per-frame fog writer or competing WorldEnvironment was added.
- **WE2:** Further visual tuning against terrain/landforms and target hardware remains planned; this isolated grass/mist job does not implement the rest of WE2.
- **WE3–WE4:** Design distant terrain and later tree/grass LOD transitions against both default mist and temporarily clearer air; a fog transition cannot hide broken streaming.
- **WE5:** Refine day/time-responsive mist and, if worthwhile, add low-lying valley/cave fog pockets with built-in volumetric FogVolume3D after visual/performance checks. Sky3D remains the only sky/light/WorldEnvironment owner.
- **WE6:** Weather can temporarily deepen or lighten the haze, but returning to normal restores the misty baseline.

Keep static settings event-driven and dormant, avoid duplicated competing global fog effects or unnecessary per-frame C# updates, and profile expensive volumetrics before enabling them broadly. **No robots or enemies are authorized by this visual direction.** See [environment implementation plan](../WORLD/ENVIRONMENT_IMPLEMENTATION_PLAN.md) for future phase scopes and pass criteria.

## Active top-down visibility fog

Fog/PlayerDistanceFog provides the separately authorized player-centred edge fog, controlled by GameplayEnvironmentMaster and the world tuning panel. Clear radius 24m, full radius 48m; FPS omits it. This lightweight depth overlay is separate from parked Sky3D mist and volumetric ground haze. See Fog/README.md for activation/idle cost, transparent-depth limitations and pending local checks.

## Active ground haze 2.0

The top-down world now reuses Weather/GroundHaze with an explicit player/terrain/camera initialization overload and the same Sky3D Environment. GameplayEnvironmentMaster enables the 30m local wispy layer; WorldTuning exposes its controls under Environment → Ground haze. One cached volume and terrain-height texture; GPU noise, movement/tile-only updates, dormant OFF/zero. Legacy explorer/cave initialization is retained; FPS comparison opts out. See [Weather/README.md](Weather/README.md) for defaults, cost, limitations and local checks. Implemented; build/shader/visual validation pending locally.

## Planned reusable local lighting

`[Lighting/README.md](Lighting/README.md)` introduces a scaffold for local point lights separate from Sky3D's sun/moon, environment fog and audio. `Lighting/Configuration/LightingMaster.tres` holds authored **future** muzzle-light defaults; `Lighting/IMPLEMENTATION_PLAN.md` specifies LI0 (brief, shadowless rifle muzzle illumination using existing FirearmShotEffects), with persistent campfire flicker deliberately deferred as LI1. **No local lights or gameplay hooks are implemented by the scaffold.**

## Scheduled streetlamps (2026-10-28)

`GameEnvironment` now owns one `StreetLampSchedule` attached to the authoritative `SkyController`; no second clock, per-lamp polling or scene scans per time tick. Change **global on/off hours** in `Lighting/Fixtures/StreetLamp/Configuration/StreetLampScheduleMaster.tres` (17:15 ON / 06:30 OFF by default). Visual shafts are controlled independently by `StreetLampLightingMaster.tres` under the same folder; no changes to Sky3D's actual sun/moon timing. Default gameplay time is paused at 09:00, so lamps should start off until time is advanced or adjusted. Source implemented; local tests pending.

**Streetlamp tweak (2026-10-28):** World-wide Sky3D light schedule now turns lamps on at **17:15** and off at **06:30**, with one shared setting in `Lighting/Fixtures/StreetLamp/Configuration/StreetLampScheduleMaster.tres`. StreetLamp001's light cone fits to its actual pole height + 0.85m below-base overlap, hiding the floating termination against terrain. Local visual testing pending.
