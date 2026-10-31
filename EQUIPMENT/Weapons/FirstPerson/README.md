> **IX0:** The FPS adapter now forwards focused LMB to shared interaction and cancels queued/held attacks on focus entry. Capture/look stay owned by the camera; slots remain available and a fresh trigger press is required after release. See [interaction contract](../../../INTERACTION/README.md). Source implemented; local validation pending.

> **CM3 bat source implementation (2026-10-28), awaiting local validation:** [Slot 3 / LMB horizontal multi-target bat](../../Melee/BaseballBat_001/README.md). Damage 11, cap 3, mild melee flinch, ELD-aware region damage, SeverPower=0; Q remains separate. This supersedes older firearm-only/two-slot/CM3-Planned descriptions. No knife, inventory or outgoing zombie attacks.

> **Current ownership:** Production FPS adapter/profile in `EQUIPMENT/Weapons/FirstPerson`. [View composition and settings](../../../WORLD/FirstPerson/README.md). Older test-folder statements below are historical; current paths and behaviour are defined by the promoted source.

# EQUIPMENT

`FirstPersonWeapon.cs` supplies centre-screen aim and trigger requests to the configured shared firearm. Queries run only for shots; shared EQUIPMENT owns damage, cooldown, muzzle obstruction and effects.

## Two test weapon slots

Shared EQUIPMENT/Shared/WeaponSlots owns two explicit scenes from EquipmentMaster: DefaultWeaponScene (1, Rifle_001) and SecondaryWeaponScene (2, MachineGun_001). Both overhead PlayerWeaponController and FPS FirstPersonWeapon use the same slots, with their own hand/camera mounts and existing aim adapters. Press top-row **1** or **2** after GUI refusal; FPS requires mouse capture. Rifle starts active. Weapon stats/automatic fire/spread/pooled tracers stay in the shared firearm implementation.

Switching cancels the trigger/pending shot, unsubscribes the old cooldown callback, stops old VFX/audio via Unequip, hides and disables the old gun, equips the new one and updates the adapter's hit mask. Click again to fire. Scenes instantiate lazily once per slot (maximum two); returning reuses the instance. No frame loop, inventory, reload/ammo, pickups or new weapon-specific firing code. Current test switching resets the unequipped gun's cooldown through existing Unequip semantics. Unload explicitly frees camera/hand-mounted weapons and releases subscriptions. Missing/invalid replacement leaves the old gun selected.

## Per-weapon FPS framing

The FPS mount starts at `FirstPersonMaster.WeaponOffset`, then adds the equipped
`FirearmSettings.FirstPersonOffset` and applies `FirstPersonScale` to that firearm
root. These adjustments are applied **only when selecting a weapon in the FPS
adapter**, not in `WeaponSlots` or the top-down player. The weapon root includes
its `Muzzle` marker, so the shot ray and cover obstruction check stay aligned
with the resized visual. Default values (zero offset, scale 1) preserve rifle
framing. The machine gun is positioned lower, slightly farther forward/right and
scaled to 78% to keep its stock/handle clear of the centre of the screen.
Adjust `MachineGunMaster.tres` if further tuning is desired.

Local validation pending: Godot/.NET unavailable here. Build, F6 World/FPS, 1/2 repeatedly, shoot/hold then switch, release/focus loss, GUI slider clicks and scene unload. Confirm only selected mesh draws, only selected gun fires, MG tracers/spread work, and both mounts/muzzles look right.

## RC2 — real FPS recoil (2026-10-28)

`FirstPersonWeapon.cs` now uses the existing recoil-aware `TryFire(aim, origin, _camera.GlobalBasis.Y)`, so sustained automatic shots deviate **upward in real 3D space**, not by simply overlaying a screen-space streak. Random `ShotSpread` runs independently after the current recoil angle; the hit ray and tracer share the result. Mouse/look direction and centre crosshair are not forcibly rotated; mouse-look down can compensate.

The reusable `EQUIPMENT/Weapons/Shared/FirearmVisualRecoil.cs` animates each selected weapon's **Visual only** (short backward +Z kick / muzzle-up local X pitch), borrowing the authored VisualRecoil settings from its master. The actual root/Muzzle marker remains unchanged; this is why recoil is not applied twice. On the accepted-shot event, the FPS adapter wakes only while the kick settles; no visual idle loop. Slot changes, successful Q shove, Escape, loss of mouse capture, backpack input block, lost focus and scene unload cancel and restore the authored visual transform. The third-person top-down adapter shares the same helper through its arm/shoulder wrapper.

**No camera kick was added** in RC2; the camera is the actual firing-reference view and another rotation would alter gameplay aim twice. Build with `dotnet build war-machine.csproj` and F6 the inherited FirstPersonTest scene. Verify a stationary crosshair with upward-climbing MG hit rays, short burst control, rifle shot kick, model return, near-wall muzzle obstruction, swap/Q/Escape/backpack/ALT-TAB and FPS mouse look. Run one top-down F6 regression for the shared visual helper. **Source implemented; local Godot/.NET verification outstanding.**

## FPS physical camera climb (RC2 refinement, 2026-10-28)

The original RC2 separately offset the final bullet ray while leaving the crosshair fixed. The FPS-only refinement now **tilts the actual first-person camera upward** while the gun fires continuously; its centre aim/crosshair and the actual hitscan follow this new 3D direction. `FirstPersonCamera` keeps mouse pitch and recoil pitch separate, so pulling the mouse downward directly compensates during a burst. **Do not also pass the three-argument recoil offset when FPS fires**: `FirstPersonWeapon` now uses `TryFire(aim, origin)`, preventing double recoil. `ShotSpread.Apply` remains separate.

`FirearmVisualRecoil.SetSustainedRise` provides extra FPS-only gun-model rise above the temporary shot kick. `FirearmSettings.FpsCameraRecoilFollowDegreesPerSecond` and `FpsVisualClimbMultiplier` tune the feel per gun: MG starts 10°/s and 0.7×, rifle 30°/s and 0.55×. Model motion never alters the real Muzzle or collision. Camera and model recoil cancel on swap, shove, backpack, Escape, mouse uncapture, lost focus and scene exit, and the FPS visual/camera driver sleeps when settled. Top-down RC1 camera is unchanged.

**QA:** `dotnet build war-machine.csproj`, F6 FirstPersonTest, test long MG burst, downward mouse correction, aim returning on release, switch/Q/Escape/focus/pitch boundaries and cover; brief WORLD F6 RC1 comparison. Source implemented, local validation pending.

## FPS one-way camera recoil revision (2026-10-28)

**Current behavior supersedes the earlier automatic pitch-recovery description.** Firing either weapon tilts the **actual FPS camera upward** via per-accepted-shot positive increments, never down as the trigger is released. The player must move the mouse to re-aim; inverted-Y FPS controls mean moving the mouse up looks down by default. The real muzzle/hitscan use this camera aim, plus independent base spread. There is **no additive recoil angle a second time**.

`FirearmSettings.FpsMaxCameraRecoilBurstDegrees` is FPS-only: **machine gun 25°**, rifle **3°**. Accepted shots count against the continuous-burst budget until an idle gap `FpsRecoilBurstResetSeconds` (MG 0.35s, rifle 0.45s), which resets only the budget—not the camera. Thus the slow rifle continues kicking once per shot. `FpsCameraRecoilFollowDegreesPerSecond` still smooths positive impulses (MG 10°/s, rifle 30°/s). The weapon's model kick and extra cosmetic muzzle rise may recover automatically; this **does not lower the camera**. All equipment/focus/UI cancellations clear pending impulses and gun animation but retain the achieved FPS look angle. Top-down RC1 remains unchanged. Local Godot/.NET validation pending; see the final section of [recoil plan](../RECOIL_IMPLEMENTATION_PLAN.md).

## AM0 FPS magazine/reload (2026-10-28)

Press physical **R** while mouse is captured (and no UI input has focus). The selected shared firearm refills after its configured reload timer; firing is blocked until then. Rifle 30 rounds / 2.2s, MG 100 rounds / 4.5s. Debug flag supplies unlimited reserve only, not unlimited magazine. On an empty magazine, auto reload begins and held automatic fire can resume after completion. Gun count survives 1/2 switches; interrupted reloads do not finish on unequipped guns. No additional FPS-specific ammo logic, recoil change, animation or HUD. See [ammo plan](../AMMO_IMPLEMENTATION_PLAN.md). Local testing pending.

## AM1 shared ammunition HUD (2026-10-28)

The inherited FPS world displays the **same** AM1 `GameplayHUD/Ammo/AmmoHud` as top-down. `FirstPersonWorld.ResolveWeaponSlots` points the shared WorldController's one-time HUD binding at `FirstPersonEquipment/WeaponSlots`. It shows the currently selected gun's magazine / reserve, reloading status, and follows 1/2 weapon swaps without polling. Mouse capture and aim are unaffected because all Controls ignore mouse input. Inspect [AMMO_IMPLEMENTATION_PLAN.md](../AMMO_IMPLEMENTATION_PLAN.md) for layout/validation. No FPS-specific ammo counts or new firing logic.


## ADS0 — held MMB aim-down-sights (2026-10-31)

**Source implemented; local build/visual validation pending.** Hold MMB with a captured FPS mouse while a firearm is selected to move its entire root toward authored iron-sight alignment, with smooth FOV and pose interpolation. Releasing reverses the motion. Rifle ADS animation multiplier is **1.1×**, movement multiplier **0.70×**, FOV **65°**; MachineGun (future SMG) is **0.9×**, **0.85×**, **70°**. Base animation is **0.20s** in `FirstPersonMaster.tres`; a multiplier of 1.0× means that full duration. Each weapon's `FpsAdsOffset`, optional pitch, animation multiplier, movement multiplier and ADS FOV live in its existing master. Current aim offsets are preliminary, requiring owner sight-alignment checks.

ADS only applies in FPS, does not change bullet spread or recoil strength, and leaves the shared damage/obstruction pipeline intact. Existing `Visual`-only recoil layers over the root/muzzle ADS pose. Camera pitch from accepted shots still requires mouse compensation. Animation shares the existing temporary recoil render callback, sleeping when settled; FPS movement penalty wakes shared player physics only on MMB state changes. Interaction focus, Q shove, Escape, lost focus, GUI blocking, switching and unload reset ADS. Slot-3 bat cannot ADS.

See [ADS implementation plan](../ADS_IMPLEMENTATION_PLAN.md) for ADS0 acceptance and unapproved ADS1 carried-weapon weight / ADS2 polish phases.
