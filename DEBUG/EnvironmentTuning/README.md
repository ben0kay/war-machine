# Environment tuning panel

F9 opens/closes compact collapsible categories for atmosphere, Sky3D time, shared wind, grass and pines. Mouse mode is restored on close; exploration input is blocked while open. Exported properties of runtime copies of EnvironmentMaster, VegetationMaster and TreeMaster supply values and initial reset defaults. Each setting has a numeric label and reset button; Reset All restores the initial master values and rebuilds vegetation.

Fog, time, cycle speed/pause and wind apply on input events. Grass height refreshes the existing GPU pool. Starred tree shape/density and grass density/distance settings wait for the explicit Apply/Rebuild button. No polling, saving, authored terrain writes or source resource writes. Optional colour/preset saving is not included. Pending settings remain in the runtime copies until applied or reset. Time slider is refreshed from Sky3D when opening; while open it represents the last edited time, not a continuously ticking clock.

To extend, bind another exported resource property using Slider or Toggle under a Category. Initial compile and headless startup passed; user should test F9/mouse restoration, sliders, resets and rebuilding in the editor.
