# Gameplay HUD

In-game player interface modules live here. The [Hotbar](Hotbar/README.md) is a purely visual nine-slot strip. HUD elements should observe future inventory/equipment systems; they must not become those systems' source of truth. Add future status widgets independently, rather than inside the hotbar.
