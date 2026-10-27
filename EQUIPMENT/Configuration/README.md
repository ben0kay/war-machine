# Equipment master

`EquipmentMaster.tres` selects and enables the default weapon scene for the active
prototype. Per-weapon combat stats belong in each item's Configuration resource.
The player adapter reads this definition once; it does not mutate the master.
