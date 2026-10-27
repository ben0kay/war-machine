# Shared hit contract

IDamageReceiver is a small reusable interface for a hit's damage, point, direction
and source. Firearms resolve it from the hit collider/ancestors, without scanning
the world or depending on a zombie class. Zombies currently implement it using
MaxHealth from ZombieMaster.tres. Other actors/destructibles can opt in later.
No global health manager, damage polling, armour, robot combat or player damage
has been added.
