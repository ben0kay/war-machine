# Inventory storage

`PlayerBackpack` owns one `BackpackStorage` for its player lifetime. `ItemDefinition` is shared descriptive data; runtime stacks have unique IDs and quantities. Storage exposes atomic TryAdd/TryRemove and MoveOrMerge, bounded capacity and one Changed event after a successful mutation. Failed operations leave contents unchanged. Definitions match by resource reference; callers must reuse the owner's canonical definitions rather than loading duplicate definitions for one item type.

`Configuration/InventoryMaster.tres` sets capacity, UI columns and optional starter stacks. Settings are duplicated once per player. There is no save/load, world pickup, item use or weapon-ammo integration yet. These stacks are a storage foundation, not the complete IX5 ownership system.

[Backpack UI and phased plan](../UI/GameplayHUD/Inventory/IMPLEMENTATION_PLAN.md). Storage has no timers or per-frame processing; closed UI refreshes only when opened, open UI observes mutations.
