# Deer master settings

`DeerMaster.tres` is a typed `DeerSettings : EntitySettings` authored profile with an embedded `HealthSettings`. Defaults: `TypeLabel = Deer`, `StartActive = false`, `MaxHealth = 120`, zero armour/resistances, brown coat, light belly/muzzle, dark hooves, brown antlers, `HasAntlers = false`.

`EntityCore` duplicates the species resource on actor Ready; `HealthComponent.Configure` also duplicates its health subresource. Health loss never mutates the authoring master. D-AI1 tuning includes wander radius/speed/wait, detection angle/range, flee distance/speed, movement/turn speeds and sight mask. The deer is deliberately inactive until world injection initializes its player target and terrain. Antlers default OFF for an ordinary deer; switch ON for small stag antlers. No zombie chasing is inherited.

World preview enable/offset are **world-owned** under `WORLD/Configuration/WorldMaster.tres`, not in the deer species resource.
