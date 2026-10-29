# INTERACTION — shared physical-world interactions

**IX0 source implemented (2026-10-29); awaiting local compilation, physics/input/visual acceptance.** IX1–IX8 remain planned. [Full phase plan](IMPLEMENTATION_PLAN.md). FPS is the primary view; top-down is supported over the same world and target state.

Hold **E** to focus interactions, aim at the nearby **IX0 TEST SWITCH**, then **LMB** to toggle its orange/green indicator. Release E to leave. FPS requires the existing mouse capture; E does not change capture or camera look. Ordinary firing resumes with a fresh trigger press. Existing slot switching remains available while focused; attacks/reload are reserved for normal weapon mode. GUI gets first refusal, and focus loss, Escape, menus, death and unload cancel pending interaction.

## Current owners

- PlayerInteraction is one player-local focus/input owner and one stopped-at-idle physics timer.
- Shared/IInteractable is the target-owned action/permission contract; Shared/InteractionTargeting resolves a collider through at most eight ancestors.
- ViewAdapters chooses top-down cursor projection or FPS centre-screen projection. Shared reach, cover, permissions and actions do not fork by view.
- Configuration/InteractionMaster.tres owns enable, focus key, 2.5m reach, 0.15s mode-only target interval and test-switch spawn.
- UI/GameplayHUD/Interaction is one mouse-through event-driven prompt below screen centre.
- DEBUG/Interaction owns the synthetic noncombat fixture and standalone targeting smoke. World composition tries four supported/clear nearby locations and skips safely if none fits. Disable SpawnTestSwitch after acceptance; no asset catalogue/persistent item is created.

## Physical safety and input

Selection is a cached camera query, then a mandatory direct torso-to-hit-point reach/cover ray. Camera segments are bounded by distance to the player plus reach (and camera far clip). A visible distant object is not usable. World/trunk layers 1/5 remain blockers even if removed from the optional QueryMask. Default mask 543 includes layers 1–5 and reserved interaction layer 10; self is excluded, camera-fade layer 8 and corpse pile layer 9 are ignored.

Top-down camera selection may skip up to four *known faded* building shapes through the existing FadeAwareAim helper (at most five camera rays). Actual reach never uses fade rules. Solid walls, doors and floors still block it. FPS uses one camera ray; physical reach adds at most one ray. Every click reselects and checks target permissions in its physics callback; no stale prompt authorizes a use.

Weapon adapters explicitly forward focused LMB events, independent of scene input order. Focus events cancel held/queued fire, shove/bat and feedback; physics guards reject queued attacks too. Release does not restore the old held trigger. Slots keep their current state and can still switch. No firearm damage, ammo, recoil, stamina, movement or camera calculation is replaced.

## Performance and limits

E press starts a one-shot physics timer; it refreshes at 0.15s only while interaction focus is held, including stationary focus so moving blockers/targets cannot leave stale selection. Click requests bring the next query forward to the next physics tick. No player/prop frame callback, global scan, interactable registry or per-object timer. E release/GUI refusal/focus loss/menu/death stops the timer, clears queued use and hides the prompt; unload releases subscriptions. Idle still receives ordinary input events, but performs zero targeting queries/timer callbacks. Fixture has no process callbacks; prompt updates only when text changes.

No carry/grab/throw, movable props, cupboard control, loot transfer, rifle placement, item identity or saves in IX0. Existing DOOR1 contact pushing remains separate. InteractionActionKind currently supports Inspect/Toggle; targets own their state.

## Local validation

Source/diff/loop/resource review only. No dotnet/Godot executable is installed here; attempted build exits 127. **Compilation, the supplied smoke and runtime/visual/performance acceptance remain unverified.**

Run `dotnet build war-machine.csproj`, then `godot --headless --path . res://DEBUG/Interaction/InteractionTargetingTest.tscn` using Godot .NET (expected `[IX0] PASS` / exit 0). This smoke checks real targeting/action code for perspective/orthographic selection, range, faded-solid cover and an upper-floor blocker; it does not prove input dispatch or visual layout.

F6 FirstPersonWorld, then secondary World. Locate the labelled post near spawn; hold E, aim/look, toggle, walk beyond 2.5m. Test walls/floors/closed doors and top-down fading, GUI hover/focus, E release with LMB held, enter focus while holding automatic fire or during a swing, slots 1/2/3, Escape/recapture, inventory, death and world reload. Expect no accidental shot, no use through cover, and a fresh click required for normal firing afterward. Check stopped timer/no queries while E is up. Terrain is not rebaked or edited. One commit on implement-td0-td1; see Git history.
