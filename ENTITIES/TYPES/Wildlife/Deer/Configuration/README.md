# Deer master settings

`DeerMaster.tres` is a typed `DeerSettings : EntitySettings` authored profile with an embedded `HealthSettings`. Defaults: `TypeLabel = Deer`, `StartActive = true`, `MaxHealth = 120`, zero armour/resistances, brown coat, light belly/muzzle, dark hooves, brown antlers, `HasAntlers = true`.

`EntityCore` duplicates the species resource on actor Ready; `HealthComponent.Configure` also duplicates its health subresource. Health loss never mutates the authoring master. Future deer AI controls should be added in separate phases; do not borrow zombie chase settings.

World preview enable/offset are **world-owned** under `WORLD/Configuration/WorldMaster.tres`, not in the deer species resource.
