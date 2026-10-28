# Player input — PSR0

PlayerInput.cs owns physical-key classification, arrow-key reads, queued crouch/prone/jump requests and focus wake-up. Current source bindings are **arrow keys**, Shift sprint, **Right Ctrl crouch**, **slash `/` prone**, RMB grounded jump and F10 contact diagnostics. Echo, GUI/focus, menu/death and FPS mouse-capture rules are preserved from the baseline; this refactor does not remap controls.

The helper is a private plain C# collaborator nested in PlayerController through a partial declaration. This keeps the existing protected FPS hooks private to the actor API without exposing new global components or adding scene nodes. Only PlayerController receives engine input/notification callbacks. The helper has no process/physics callback, timer, subscription or per-frame allocation. Menu blocking clears pending stance/jump input through the same controller gate.

PlayerSettings/PlayerMaster retain all tuning. Build and World/FPS input checks remain local; see the PSR0 section of ../IMPLEMENTATION_PLAN.md.
