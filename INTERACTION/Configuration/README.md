# Interaction configuration

InteractionMaster.tres is the single IX0 default profile, copied per owner. Enabled=true; FocusKey defaults to E; ReachMeters=2.5 (finite clamp 0.5–4); TargetIntervalSeconds=0.15 (finite clamp 0.05–0.5). QueryMask=543 includes world/actors/trunks and reserved interaction layer 10; world/trunk bits 17 are always enforced. No corpse/camera-fade masks by default.

SpawnTestSwitch=true adds one explicitly labelled debug fixture near spawn only on supported clear ground (four candidates maximum). Turn it off after validation. No persistent authoring, asset catalogue, carry, weight, inventory or future-phase settings. Restart the scene after profile edits. Build/runtime acceptance is pending; see ../README.md.
