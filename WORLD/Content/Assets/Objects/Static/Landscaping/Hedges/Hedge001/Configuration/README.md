# Hedge001 visual size master

`HedgeMaster.tres` is a typed `PlaceableSizeSettings` resource shared with other GLB catalogue assets. Its `SizeMultiplier = Vector3(1, 1, 1)` keeps the authored GLB at ~**4.805m wide × 2.138m tall × 1.274m deep**.

X scales width; Y scales height; Z scales depth. Multipliers are **relative values, not metres**. Existing `PlaceableVisual` applies them to the `Model` container once at scene initialization. Override with a duplicated resource for individual hedge sizes. No hedge-specific settings class, geometry C#, idle loop, or matching collider has been added.
