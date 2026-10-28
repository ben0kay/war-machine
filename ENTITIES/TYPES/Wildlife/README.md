# Wildlife — passive animal entities

Wildlife species live under `ENTITIES/TYPES/Wildlife/` and reuse `ENTITIES/Shared/Entity.tscn`, `EntityCore` and optionally `Shared/Combat/HealthComponent`, without zombie chasing/roaming or robot scanning logic.

- [Deer](Deer/README.md): first passive wildlife prefab, original lightweight procedural deer mesh, shared health and one configurable world test spawn.

**No wildlife AI is implemented yet.** Future wandering/fleeing/hearing and herd spawns should be opt-in components, using event-driven relevance rather than idle frame polling. The species owns its visual/health; the world owns terrain grounding and placement.
