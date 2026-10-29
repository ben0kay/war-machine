# Player rig presentation — PSR0

PlayerAnimation.cs owns the cached generated rig references, phase clock, movement-facing interpolation and original walk/crouch/crawl Pose. PlayerWeaponPose.cs owns holding/aim/socket presentation, recoil deltas and two-handed shove arm solving. Actual firearms, reload, accepted shots, recoil mechanics and melee delivery remain in Equipment; C/ remains procedural geometry generation.

These are private plain C# collaborators nested in the existing partial PlayerController, not Godot nodes or new scripts attached to the scene. The mesh is built once through the existing PlayerMeshBuilder; original socket paths, rig dimensions, mathematical expressions and materials are preserved. Both helpers use the same PlayerSettings instance supplied to the controller.

Only actual controller movement or existing equipment calls update poses. Locomotion writes the base rig first, then reapplies shove grips, resets the applied recoil delta and reapplies recoil exactly once. Settled players gain no animation loop, timer, scene search or per-frame allocation. WeaponProbeOrigin, WeaponMount and SetWeapon* / SetShoveGrip APIs stay on PlayerController; FPS can still hide VisualRoot and override its movement hooks.

Source-equivalence checks passed for extracted pose/aim/recoil/grip math. Compilation and in-engine walk/crawl, aim/recoil/shove and menu/death cancellation remain pending locally.


## CM3 top-down one-handed baseball bat — first pass (2026-10-28)

`PlayerBatPose.cs` is a plain nested player helper (no Node) that owns the **top-down-only bat presentation**. Equip slot 3 positions the actual bat at the right side within arm reach, registers its `GripRight` marker and immediately applies a one-handed holding stance. The existing two-bone `PlayerWeaponPose.ReachGrip` IK places the right glove on that marker, and the left arm remains free for counterbalance. The normal gait/legs are unchanged. `MeleeWeaponController` reuses its existing windup/active/recovery times to rotate the bat locally around the player's weapon socket and publish a [-1,1] swing cue; `PlayerBatPose` adds small torso twist, shoulder travel and left-arm balance and resolves the right hand on every active strike tick. It does **not** create a new processing loop, change hit detection, or animate the actual shared player body in FPS (which hides the 3rd-person rig and uses its own camera-mounted bat).
 
The pose resets when the strike ends, the bat is unequipped, Q shove temporarily takes both hands, or the scene exits. Base locomotion re-applies the single bat grip after the normal rig pose. The model is still an early procedural prototype; verify wrist alignment through wide swings and low stances in Godot. Source implemented; local `dotnet build`/Godot visual checks pending.
