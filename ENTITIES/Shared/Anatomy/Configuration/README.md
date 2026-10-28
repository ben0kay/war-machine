# Anatomy configuration

AnatomySettings owns detection padding/logging and ELD1 condition, state fractions, sever threshold/resistance. AnatomyMaster.tres is the shared fallback. Zombie masters select their species profile at `ENTITIES/TYPES/Zombies/Configuration/ZombieAnatomyMaster.tres`; runtime copies protect authored resources. Weapon SeverPower lives in each firearm master, independent of HP damage. Player BodyCondition settings are unchanged. See [anatomy ownership and validation](../README.md).
