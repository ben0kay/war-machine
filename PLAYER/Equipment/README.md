# Player equipment adapter

C/PlayerWeaponController.cs equips the scene selected by EquipmentMaster and
bridges mouse/camera/player changes to shared FirearmController. It owns input
and aim queries only; it has no rifle path, damage, range or cooldown constants.
The player owns WeaponMount and its holding pose. Camera ViewChanged covers
follow, zoom, pitch and projection. No settled idle physics/process callback.
Release is observed before GUI handling; presses only after GUI refusal.
Focus loss/unload cancels the trigger and releases subscriptions/effects.
