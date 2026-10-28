# Ammunition and reloading — AM0–AM2

> **Status (2026-10-28): AM0 implemented in source, local Godot/.NET verification pending. AM1 implemented in source (awaiting local HUD validation); AM2 planned only.** Working branch: `implement-td0-td1`. This document describes the current foundation and later stages; do not implement AM1/AM2 without explicit authorization.

## Design decision: unlimited reserve, not an unlimited magazine

For debug testing, guns retain **finite magazines and real reload delays**. `DebugUnlimitedReserveAmmo=true` means reloading can refill the gun indefinitely **without consuming inventory ammunition**. It does not make firing itself unlimited. Later, with the boolean OFF, reloading draws from finite reserve inventory. No physical magazines, reload animation, ammunition pickup or HUD is introduced by AM0.

Rifle_001 starts at **30 / ∞**, with a **2.2-second** reload. MachineGun_001 starts at **100 / ∞**, with a **4.5-second** reload. Both have `AutoReloadWhenEmpty=true`, and manual **R** reload is available before emptying the magazine. These values are per-weapon `.tres` settings.

## AM0 — shared ammunition state and timed reload (implemented in source)

**Ownership:** `EQUIPMENT/Weapons/Shared/FirearmAmmo.cs` is a non-Node, per-instance state helper, owned by each `FirearmController`. It caches configured magazine capacity, current magazine rounds, initial finite reserve, the debug-unlimited-reserve flag and whether a reload is underway. It never allocates a manager, scene, or per-bullet object.

**Inspector fields:** `FirearmSettings` exports `MagazineCapacity`, `ReloadSeconds`, `AutoReloadWhenEmpty`, `DebugUnlimitedReserveAmmo`, `InitialReserveRounds` under one "Ammunition & reload (AM0)" group. `InitialReserveRounds=0` for both existing guns; it is ignored when unlimited debug reserve is ON. When debug reserve is OFF, only configured reserve rounds can be transferred, with no negative counts.

**One accepted shot:** Shared `FirearmController.TryFire` checks equip, cooldown, reload state and available magazine rounds before normal recoil/spread/cover/hitscan logic. It takes **exactly one round only after a valid accepted shot has already fired**. A shot that misses or hits terrain still consumes one round; rejected fire attempts during cooldown or reload consume nothing and generate no shot/recoil/feedback. Firing pipeline otherwise unchanged.

**Reload state:** One extra one-shot physics `Timer` (per firearm) handles `TryReload()`. A successful manual R, or automatic empty-magazine reload after the last accepted shot, immediately sets `IsReloading=true`, blocking firing even if the cooldown ends. Reloading while a magazine is partially filled **tops off** without discarding remaining rounds; finite reserve only spends the rounds actually loaded. `ReloadStarted`, `ReloadCompleted`, `ReloadCancelled`, `AmmoChanged` events and `MagazineRounds`, `MagazineCapacity`, `ReserveRounds`, `HasUnlimitedReserveAmmo`, `IsReloading`, `CanFire` properties are exposed for AM1 HUD and AM2 sounds/animations later. No audio or animation is fabricated now.

**Held automatic fire:** When reload finishes, the firearm may emit its existing `ReadyToFire` signal; the currently equipped top-down/FPS adapter may schedule another shot **only if the fire button is still held and the gun is automatic**. If the reload finishes before shot cooldown, the normal cooldown timer emits the request later. Releasing the button before completion does not auto-shoot. Empty-magazine attempts with auto reload enabled can start another reload; with auto reload disabled, R is required. If finite reserve is exhausted, the weapon simply stays empty and cannot reload.

**Weapon switching:** Existing `WeaponSlots` caches the actual firearm scenes, retaining magazine and finite reserve count across switches. A reload in progress is **cancelled when that gun is unequipped** (no rounds granted); on return the partially spent or empty magazine remains. Switching back does not recreate or refill the magazine. An empty gun will automatically try reloading on the next attempted shot if auto-reload is enabled. No in-background inactive-weapon reload is part of AM0.

**Input, focus, performance:** Physical **R** is handled by both existing input adapters after UI refusal (FPS also requires captured mouse). No new InputMap binding, input polling or per-frame ammo code. The timer sleeps when not reloading. ESC, input-block menus, focus loss and Q shove still cancel the *held trigger and recoil presentation*; they do not grant free rounds or bypass the reload timer on the still-equipped weapon. Scene teardown and unequip stop that gun's timer. Reload sounds, reload animations and a magazine readout are postponed.

**Suggested checks:** `dotnet build war-machine.csproj`. F6 `WORLD/World.tscn` and `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`; fire rifle 30 rounds and MG 100, ensure neither can fire while the 2.2s/4.5s timer is running, and held automatic fire resumes *only* when still held. Check R on partial/full magazines, no duplicate reloads, muzzle-against-cover shots, held fire, R during cooldown, swap during reload and back (count retained, no instant refill), GUI focus, Q, Escape, focus loss. For finite reserve test turn DebugUnlimitedReserveAmmo OFF and set InitialReserveRounds to e.g. 12; confirm reload tops off only by 12 and then cannot reload. Confirm no extra idle callbacks and no recoil/spread/damage change.

**Validation:** Source integration verified against current repository state. In-game and .NET compile checks have **not** been performed in this environment; leave AM0 status "implemented, awaiting acceptance" until tested locally.

## AM1 — ammunition HUD (implemented in source; awaiting local validation)

**Layout and scope:** `UI/GameplayHUD/Ammo/AmmoHud.tscn` + `AmmoHud.cs` add one compact 160×66px, noninteractive readout to the existing `GameplayHUD/Screen`, mounted by shared `WorldController.InitializeAmmoHud` after equipment and player-health widgets initialize. It is **to the left of the 472px bottom-centre hotbar**, and **to the right of the 244px bottom-left player body HUD** on desktop screens, sharing the hotbar's bottom margin. For narrow windows (left-of-hotbar space under 278px), it moves just **above the bar and right of the body panel**, avoiding overlap with both at typical window sizes; extremely narrow windows cannot physically keep all three side-by-side. Position changes only on viewport resize, not every frame. The appearance matches hotbar's subdued charcoal card.

**Readout:** Large `magazine / reserve` text (e.g. `30 / ∞` or `12 / 30`) with a compact AMMO label. When reloading, show amber `RELOADING`; partially depleted but not reloading, show `R RELOAD`; empty, `EMPTY`. Values come only from shared AM0 authoritative firearm state. No reload progress tween or bar: not asked for in this compact first pass. Labels and styles are created once, all UI Controls have MouseFilter.Ignore to avoid blocking shooting/camera/UI.

**Event binding:** `EQUIPMENT/Shared/WeaponSlots.cs` exposes a `SelectionChanged(FirearmController?)` event emitted after a successful slot selection; the new widget subscribes once, reads `slots.Current` at startup, unsubscribes from old weapon and subscribes to selected gun's `AmmoChanged`, `ReloadStarted`, `ReloadCompleted`, `ReloadCancelled`. Bind/unbind and viewport-size events explicitly disconnect on exit. No ammo polling, duplicate ammo state, changes to the recoil/fire/hit pipeline or new global manager.

**Both camera modes:** WorldController resolves `Equipment/WeaponSlots` for the primary top-down player. `FirstPersonWorld` overrides only the resolve path with `FirstPersonEquipment/WeaponSlots`, so the inherited FPS scene gets **the same one HUD card** without duplicating UI. Weapon switching refreshes the new selected weapon's actual remaining magazine immediately, including after a reload was interrupted during switch. No separate FPS ammo view.

**Local verification:** `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn` and F6 `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`. Confirm visually at 1920×1080, 1280×720 and narrower: the AMMO card is between the player body silhouette and hotbar (or safely above bar on a narrow view). Hold fire to watch 30→29 / ∞ and 100→99 / ∞, reload manually R and automatically on empty, check RELOADING state/duration and return to full, 1↔2 with partially spent magazines and cancelled reloads, finite debug reserve OFF test, backpack open, cursor hover/UI focus and FPS mouse capture. Verify one widget per inherited scene, no gunfire blocked by the card, and no idle ammo HUD callbacks.

**Validation status:** Static source/layout/event-path review only. C# compilation, Godot scene instantiation and visual window-size checks unavailable in this connector session. Leave **Awaiting Validation** until owner verifies. AM2 remains planned only.

## AM2 — actual ammunition and physical magazines (planned only)

Introduce ammo calibre/type, ammo stacks in the backpack, real reserve accounting, optionally detachable magazine items and chambered rounds, plus reloading animation, audio, interrupt handling and saving/loading magazine states. Inventory/weapon ownership supplies reserve stock while the shared firing controller remains authoritative. Choose detailed realism semantics later; never make the current test toggle part of permanent survival balance. AM2 is not part of this commit.

## Integration directions for future work

Read the root `README.md`, `CodePreferences.md`, this plan, `Weapons/Shared/README.md`, actual `FirearmController`, `FirearmAmmo`, `FirearmSettings`, `WeaponSlots` and both view adapters before editing. Preserve the physical FPS recoil/camera behavior, top-down recoil, independent ACC0 spread, pooled tracers, cover ray, sound events, impacts and damage. Follow one-authorized-task/one-commit policy on `implement-td0-td1`, never `main`.
