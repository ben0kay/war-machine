# Backpack interface

`Inventory.tscn` hosts `InventoryPanel`, built from native Godot controls with an olive field-pack presentation. `BackpackSlot` handles inspection and drag/drop. Icons are local SVG assets. The shared world controller mounts one panel beside the hotbar, also used by the FPS test.

Press **I** to open, **I / Escape / Close** to close. The world continues running. Drag to move, swap or merge. The player cannot move, look, fire, shove or switch weapons while open.

Storage lives in `INVENTORY/`, not in these controls. See [implementation plan](IMPLEMENTATION_PLAN.md) for implemented scope, local checks and future hotbar integration. No item-use or hotbar binding exists yet.

## Future Health tab (not PH0/PH1)

A later inventory Health / Body tab may show exact six-region condition values, individual injuries, fractures, bleeding and treatment items. PH1 only adds the compact read-only anatomical silhouette to the gameplay HUD. No inventory scene/layout changes were made by this plan. [Anatomy design](../../../PLAYER/Stats/BODY_CONDITION_DESIGN.md).
