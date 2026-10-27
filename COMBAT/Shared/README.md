# Shared contacts

CombatImpact and IImpactReceiver express damage-independent reactions. HitTargetResolver dispatches optional damage first and optional impact second to one bounded ancestor receiver. Self/scenery/death handling and current callers are documented in ../README.md. Damage/health types remain in ENTITIES/Shared/Combat. No idle callbacks or new physics queries.
