# Shared weapon behaviour — reserved

Candidate responsibilities when needed: common fire-rate/cooldown, damage request, shot query, muzzle effect and common equipment/target contracts.

**No code yet.** The temporary Rifle_001 should first have a clean small implementation. Move logic here when another firearm would otherwise duplicate it. Avoid an always-running weapon controller while unequipped/idle.

Weapon-specific C# procedural geometry belongs under that weapon's `C/`; imported artwork belongs under its `Artwork/`.
