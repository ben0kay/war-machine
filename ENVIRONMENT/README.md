# ENVIRONMENT — Shared atmosphere

`Sky/WildernessSky.tscn` adapts only Sky3D's environment and four relevant child nodes from its reference scene. No city, reference camera, demo UI, reflection probe or competing static sky is included. Sky3D owns sun, moon, sky, clouds and its timer.

`Configuration/EnvironmentMaster.tres` / `EnvironmentConfig.cs` own start hour, day length, accelerated preview, update interval, initial pause and shared grass wind. `Sky/SkyController.cs` applies these once after initialization. **P** pauses/resumes solar time; **F8** toggles a one-minute day. Pausing solar time intentionally leaves clouds and grass moving. Editor time is disabled to keep a stable authoring preview.

No custom per-frame sky loop exists. Sky3D updates time on its 0.1-second timer while running; pausing stops that timer. Sky3D cloud callbacks and shared GPU grass animation are intentional active visual work. Grass and clouds currently use separate shader motion models; coherent broader weather/wind belongs to WE4/WE6. No weather, forest or streaming framework is introduced.

Sky3D 2.1 is MIT licensed; retain `addons/sky_3d/LICENSE.txt` and its third-party texture license files. The supplied sky scene references those original assets. See `WORLD/WE0_WE1_VALIDATION.md` for tested versus pending checks.
