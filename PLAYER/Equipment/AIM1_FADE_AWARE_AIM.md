# AIM1 — Fade-aware mouse targeting (implementation handoff)

**Status: Implemented in source; local Godot/.NET validation pending.**
**Branch:** `implement-td0-td1`. Scope: top-down mouse cursor aim only.

## Problem and expected behavior

CameraOcclusion fades individual walls, roofs, upper floors, and whole buildings visually. The existing top-down cursor ray uses the weapon HitMask, which still hits invisible/translucent building collisions. This points the weapon at a foreground faded wall rather than a zombie or object behind it. Bullets and physical movement **must** continue hitting that solid wall.

## Implemented approach

1. `CAMERA/Occlusion/CameraOccluder.cs` publishes a bool metadata flag on each owned visual mesh only when its *composed* visual opacity crosses the near-opaque threshold. This correctly includes section, floor/roof and exterior whole-building fades, but does not mistake authored transparent materials for camera fading.
2. `CAMERA/Occlusion/FadeAwareAim.cs` resolves the hit **shape owner** from Godot's ray `shape` index and looks up the specific paired visual through metadata set at construction time. Only known, currently faded visual/collider pairs can be passed through; otherwise mouse selection stops on the hit normally.
3. `WORLD/Content/Assets/Structures/Shared/C/StructureGeometry.cs` and `Building001/C/Building001Mesh.cs` link each generated box's collision shape with its associated visual. Many wall/roof/prop shapes can belong to one StaticBody3D: **never exclude the whole body**.
4. `PLAYER/Equipment/PlayerWeaponController.cs` calls the helper only for mouse aim. Each hit on a faded paired shape advances the origin slightly and re-queries; loop is bounded at 24 skipped hits and fails closed at the limit. Original physics-ray query origin is restored; weapon `TryFire`, muzzle/bullet collision and damage are untouched.
5. Future buildings using StructureGeometry inherit the behavior; custom/imported buildings must pair visual/collision shapes explicitly. Unpaired collision remains opaque to mouse rays. Both camera view and mouse motion keep their existing input-driven aim scheduling. No new process, global manager or periodic raycast.

## Validation checklist (owner/Copilot)

- `dotnet build war-machine.csproj` in the Godot .NET project; F6 `WORLD/World.tscn`.
- From top-down outdoors, stand behind a building so it enters whole-building fade; mouse over a zombie on the far side: weapon aim should track zombie; fired bullets should visibly stop at solid building collision.
- From inside a building, check an individually camera-faded wall, roof or hidden upper floor; target a zombie/object beyond it. A separate **opaque** wall or roof on the same shared collision body must still stop the cursor.
- Step into open space and let the same wall return to opaque; normal mouse aim collision should resume. Turn obstruction fading off and repeat.
- Check no mouse-ray passthrough on other opaque building walls, ground, car, tree trunk, props or unpaired colliders. Check top-down slot 1/2 aiming and muzzle obstruction as before.
- FPS inherited test scene: no changes to FPS aim or shooting. Confirm no error on spawn/unload/rebuild and unchanged zombie vision/movement.
- If deeply overlapping >24 faded collision shapes, the last checked shape conservatively blocks the cursor; this is intentional to cap worst-case aim rays.

## Performance

Normal aim: **one** existing ray, plus a constant shape-owner check for hits. Extra raycasts: **only** after a known faded hit; at most 24 additional rays per aim event. Aim events remain input/camera/player-driven, with existing one-shot physics processing; fade metadata writes occur only when entering/exiting faded state. No extra `_Process`, `_PhysicsProcess`, timers, materials, collision layers, or full-world scans are introduced.

## Files / ownership

- Aim adapter: `PLAYER/Equipment/PlayerWeaponController.cs`
- Cursor ray helper: `CAMERA/Occlusion/FadeAwareAim.cs`
- Fade state: `CAMERA/Occlusion/CameraOccluder.cs`
- Building shape pairing: `WORLD/Content/Assets/Structures/Shared/C/StructureGeometry.cs` and legacy `Building001/C/Building001Mesh.cs`
- Documentation: `PLAYER/Equipment/README.md`, `CAMERA/Occlusion/README.md`, `WORLD/Content/Assets/Structures/Shared/README.md`, `BUILDING_AUTHORING_README.md` and this file.

**Not changed:** gun projectile/ballistic logic, camera occlusion triggering/ranges, world placement, actual collider enabled/disabled state, enemy AI, FPS aim, movement, chunk streams. Godot build and rendered validation are **not yet claimed**.
