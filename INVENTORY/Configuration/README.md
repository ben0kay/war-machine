# Inventory configuration

Edit `InventoryMaster.tres` in the inspector: Capacity (6–60), Columns (4–8), StartingItems (InventoryStartingItem resources containing an ItemDefinition and quantity). Default: 24 pockets, six columns, two water bottles, three bandages and 20 rifle rounds. Clear StartingItems for an empty pack. Quantities must fit; a rejected starter stack logs a warning without partially inserting it. Unit weight is display-only. Definitions contain a stable type ID for future serialization; runtime stack IDs are generated per player session.
