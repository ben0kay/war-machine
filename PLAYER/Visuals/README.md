# Player rig presentation — PSR0

PlayerAnimation.cs owns the cached generated rig references, phase clock, movement-facing interpolation and original walk/crouch/crawl Pose. PlayerWeaponPose.cs owns holding/aim/socket presentation, recoil deltas and two-handed shove arm solving. Actual firearms, reload, accepted shots, recoil mechanics and melee delivery remain in Equipment; C/ remains procedural geometry generation.

These are private plain C# collaborators nested in the existing partial PlayerController, not Godot nodes or new scripts attached to the scene. The mesh is built once through the existing PlayerMeshBuilder; original socket paths, rig dimensions, mathematical expressions and materials are preserved. Both helpers use the same PlayerSettings instance supplied to the controller.

Only actual controller movement or existing equipment calls update poses. Locomotion writes the base rig first, then reapplies shove grips, resets the applied recoil delta and reapplies recoil exactly once. Settled players gain no animation loop, timer, scene search or per-frame allocation. WeaponProbeOrigin, WeaponMount and SetWeapon* / SetShoveGrip APIs stay on PlayerController; FPS can still hide VisualRoot and override its movement hooks.

Source-equivalence checks passed for extracted pose/aim/recoil/grip math. Compilation and in-engine walk/crawl, aim/recoil/shove and menu/death cancellation remain pending locally.
