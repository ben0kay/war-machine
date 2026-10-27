# Rifle_001 — temporary usable low-poly rifle

**Implemented, awaiting local validation.** Always equipped in the active top-down
preview, selected by `EQUIPMENT/Configuration/EquipmentMaster.tres`.

- `Rifle_001.tscn`: shared FirearmController root, replaceable Visual and Muzzle.
- `C/RifleMeshBuilder.cs`: original temporary procedural rifle geometry only.
- `Configuration/RifleMaster.tres`: shared FirearmSettings resource, edited per rifle.
- `Artwork/`: eventual imported model/texture destination.

## Defaults and controls

| Setting | Value |
| --- | --- |
| Shot interval | 0.65 seconds (about 92 rounds/minute) |
| Damage | 25 |
| Range | 90 metres |
| Trigger | Click once or hold left mouse for slow repeating fire |
| Hit mask | 5: world layer 1 + zombie layer 3; player layer 2 excluded |
| Feedback | 0.07s reused flash/tracer, optional sound slot (empty by default) |

Mouse aims toward a physics surface using the active camera in either projection.
Empty space falls back to a hand-height plane. The player faces the cursor while
walking/crouching, with holding pose/socket settings in TopDownTestMaster.tres.
Shots use the muzzle, not the camera, and check shoulder-to-barrel obstruction.
Zombies have editable 50 health: two default-rifle hits remove one. Dead actors
stop their brain/movement and leave the scene; no ragdolls, drops or respawns.
Robot/scenery can block a shot but have no damage response added in this pass.

UI clicks don't shoot. Releasing the mouse, focus loss or hovering the tuning UI
cancels held fire. No ammo, reloads, inventory, switching or ballistic projectiles.
Existing camera/arrow movement/crouch/environment controls remain.

## Reuse / replacement

For another firearm, make another scene using the shared controller and settings,
copy its stats resource, supply its visual and Muzzle marker, then select that
scene in EquipmentMaster. No player or firing-code rewrite is required.
For imported art, replace only Visual, align the grip origin and muzzle facing -Z.
Weapon pose is player-rig configuration, not a rifle-specific animation driver.

## Performance / validation handoff

Unequipped weapons have no polling. Actor/camera/mouse changes queue an aim update
in physics; settled aim sleeps. Holding fire waits on the weapon cooldown timer.
A shot uses a shoulder obstruction ray and, when unobstructed, one hit ray.
Hit reactions use an interface, not world scans. Two feedback meshes are reused
and stop processing after expiry; optional audio only plays per shot.

Source/resource paths, wiring and whitespace checked. Godot/.NET unavailable in
this environment, so **compilation and gameplay remain unverified**. Locally:

1. `dotnet build war-machine.csproj`, then open in your Godot .NET editor.
2. F6 `WORLD/TopDownWorldTest/topdownWorldTest.tscn`; use existing preview terrain.
3. Check gun/arms standing, walking, crouched, turning and aiming uphill/downhill.
4. Click/hold: slow regular shots, correct muzzle/tracer and 2-hit zombie removal.
5. Verify terrain/cover blocks hits, another zombie intercepts the shot, robot is unharmed.
6. Click tuning sliders and alt-tab while firing: no unwanted/stuck shots.
7. Switch orthographic/perspective, zoom/pitch, move with a stationary cursor.
8. Disable EquipmentMaster or unload: no stuck subscriptions/audio/effects.

No terrain rebake or parked FPS scene changes are required.
