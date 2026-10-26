# Environment tuning panel

F9 opens/closes compact collapsible categories for atmosphere, Sky3D time, shared wind, grass and pines. Mouse mode is restored on close; exploration input is blocked while open. Exported properties of runtime copies of EnvironmentMaster, VegetationMaster and TreeMaster supply values and initial reset defaults. Each setting has a numeric label and reset button; Reset All restores the initial master values and rebuilds vegetation.

Fog, time, cycle speed/pause and wind apply on input events. Grass height refreshes the existing GPU pool. Starred tree shape/density and grass density/distance settings wait for the explicit Apply/Rebuild button. No polling, saving, authored terrain writes or source resource writes. Optional colour/preset saving is not included. Pending settings remain in the runtime copies until applied or reset. Time slider is refreshed from Sky3D when opening; while open it represents the last edited time, not a continuously ticking clock.

To extend, bind another exported resource property using Slider or Toggle under a Category. Initial compile and headless startup passed; user should test F9/mouse restoration, sliders, resets and rebuilding in the editor.

## RM5 robot controls

F9 now adds **Entities / Robot_001** after scene readiness. Bindings edit the robot's runtime resource and nested patrol/sensor copies, preserving saved masters. Controls: enable, patrol speed, staged scale + Apply, leg animation, beam visibility, duration/range/full angle, opacity/brightness/band contrast/line speed, head sweep width/rate, logical sensor, quiet audio toggle/level and force scan. Reset All restores robot values and applies scale as well as existing environment/vegetation settings. Art dimension regeneration is not provided; scale updates transforms/capsule without rebuilding art meshes.

Stage/active status refreshes on opening and meaningful events only; hidden UI has no polling callback. Subscriptions are released on exit. Controls have source checks only; local build, reset, two instances and visual/audio acceptance remain pending.
