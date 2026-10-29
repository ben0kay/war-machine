# IX0 view adapters

InteractionView.cs contains the small IInteractionView contract and plain TopDownInteraction/FirstPersonInteraction adapters. They select cursor versus viewport centre, FPS capture eligibility and optional faded-camera selection. They own no nodes, timers, world state or physics rules. WorldController chooses top-down; FirstPersonWorld overrides only the factory. Shared targeting/actions remain authoritative. No FPS handle dragging is implemented in IX0.
