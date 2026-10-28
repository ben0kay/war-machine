# Backpack and hotbar implementation plan

The backpack is a live-world menu: it never pauses zombies, weather, physics or time. Storage belongs to the player; the UI observes it. This is independent of completing the broader interaction plan.

| Phase | Work | Status | Suggested owner |
| --- | --- | --- | --- |
| BI0 | Per-player bounded storage, definitions, stack identity, atomic add/remove/move/merge, configurable starter items | Implemented; local validation pending | Codex |
| BI1 | Backpack interface, item details, drag rearrangement and merge, I/Escape, shared overhead/FPS input blocking | Implemented; local validation pending | Codex; Regular Chat for appearance tuning |
| BI2 | Connect nine-slot hotbar to inventory item identities; assign/clear bindings, show live quantities, resolve selection through storage | Planned | Codex |
| BI3 | World pickup/drop and equipped ownership bridge, item actions and real ammo consumption | Planned; coordinate IX0/IX5/IX6/IX7 | Codex |
| BI4 | Persist backpack contents, identities and hotbar bindings | Planned; coordinate IX8/save architecture | Codex |

## Implemented slice: BI0–BI1

24 pockets by default. Water, bandages and rifle rounds are configurable starter stacks in `INVENTORY/Configuration/InventoryMaster.tres`; clear StartingItems for an empty pack. Capacity and columns are adjustable there. Drag onto an empty pocket to move; onto another item to swap; onto the same definition to merge up to its cap. Overflow remains in its source pocket. Item details show quantity and stack weight. Weight is informational, with no encumbrance mechanic.

I opens/closes; Escape and Close close. Mouse capture is restored only when appropriate. Opening cancels held/queued weapon attacks and shoves, blocks player movement/look/weapon switching and releases the cursor. Gravity continues. Both the shared world and inherited FPS test use one player model and one HUD panel. Storage and UI have no per-frame callbacks; storage changes update the open UI and opening refreshes current contents.

This slice does not implement item use, splitting stacks, pickups, drop/delete, saving or hotbar bindings. Existing keys 1/2 still select test weapons outside the menu; starter ammunition does not supply those weapons. Stack IDs identify runtime stacks, not individual rounds; splitting or persistence must explicitly define identity behavior.

## BI2: hotbar contract

Hotbar entries reference stable inventory identities, never copied stacks or slot indices. Moving a stack keeps its bindings valid. Removing/fully merging a bound source must explicitly transfer or clear its binding, with no stale references. Render icons/counts from storage events. Empty/invalid bindings are harmless. Keep a selected item separate from executing its action; clicks inside inventory cannot fire a weapon. Resolve equipment through one shared bridge in overhead and FPS.

## BI3: ownership and actions

Complete the broader IX5 World/Inventory/Equipped ownership contract before adapting real weapons. A pickup transfers exactly once, leaving the world object intact if capacity rejects it; drop transfers the same identity back to one world representation. Define action checks and atomic consumption before adding bandage/water effects. Do not duplicate functional weapons or equate physics-held props with stored items. No automatic completion of other interaction phases is implied.

## Local acceptance

Build in Godot C#. Open I in both views while walking, holding fire and queuing a shove; confirm immediate cancellation, cursor access and continuing zombies/weather. Try moving, swapping, partial/full merging, filling capacity and reopening. Verify no lost/duplicated quantities and changed-event behavior. Check Escape, close button, alt-tab, scene reload, small window layout and drag release outside the pack. Verify keyboard/mouse controls resume cleanly without a queued shot. BI2 onward require separate implementation and validation.

No Godot/.NET runtime was available in the implementation environment; build, visual and gameplay acceptance remain pending.
