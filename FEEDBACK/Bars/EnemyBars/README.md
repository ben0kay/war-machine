# NPC health overlay — injured enemies visible to the camera

**Implemented in source; Godot compilation and runtime validation pending.**

`HealthBarOverlay.cs` observes shared `HealthComponent` events and pools screen-space health
bars for injured, living **NPCs** only (standard zombies, runners and other actors). Health
changes remain event-driven; the bar itself is hidden at full HP and on death.

## Visibility rules

A wounded actor's bar appears only when its actor is visible in the tree, it is in
front of the current camera, its bar anchor is inside the viewport, and it is
within `HealthBarMaxDistance` (**50 metres** by default). When
`HealthBarOcclusionEnabled` is true, a physics ray from the **camera** to the
actor's upper torso must also be clear: nearby hills, collidable buildings and walls
block it. We probe the torso rather than the bar's floating position so bars don't
show through a hill just because the bar would be above the hill.

The ray uses `HealthBarOcclusionMask = 17` (world layer 1 plus tree-trunk layer 5),
matching the existing zombie sight blocker mask. The player's own collider is excluded.
Purely decorative geometry without collision will not occlude a health bar; check the
collider and mask when an expected wall doesn't block it.

## Low-cost updates

- `ProjectionUpdatesPerSecond = 20` by default (**one update every 0.05 seconds**).
- Screen projection and optional physics occlusion use the same 20 Hz cadence, driven
  by the shared overlay's `_PhysicsProcess` (not a callback on every zombie).
- Only **wounded, on-screen, in-range** actors are ray-tested, bounded by
  `MaxVisibleBars` (48 default) per update.
- No injured actors means the overlay disables its physics callbacks entirely.
- Health values update immediately on `HealthChanged` and are not polled at 20 Hz.
- No new timers, ray casts per zombie per frame, or new NPC gameplay code.

## Configure

`FEEDBACK/Configuration/FeedbackMaster.tres`:

- `ProjectionUpdatesPerSecond = 20`
- `HealthBarMaxDistance = 50`
- `HealthBarOcclusionEnabled = true`
- `HealthBarOcclusionMask = 17`

The existing bar dimensions, anchor, pool capacity and separate player silhouette
settings are unchanged.

## Acceptance checks (local Godot test)

Pull `implement-td0-td1`, build the .NET solution, and run
`WORLD/FirstPerson/FirstPersonWorld.tscn` (F6). Damage a zombie and verify its bar
appears unobstructed, disappears behind a hill/building, disappears past 50m, and
reappears as it comes into view. Repeat while moving around cover; check the second
world view and other injured NPC types. Collision-less foliage does not count as a wall.

This folder owns NPC presentation only. The player body-condition silhouette lives in
`FEEDBACK/Bars/PlayerStatus`; do not restore a numeric player HP bar here.
