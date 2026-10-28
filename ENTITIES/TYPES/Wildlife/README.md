# Wildlife — reusable species

Wildlife uses shared `EntityCore`, `HealthComponent` and optional `VisibilityResolver`, but its decisions remain species-specific. No zombie chases or robot scanning beams.

- [Deer](Deer/README.md): D-AI1 wander, notice a nearby player, flee a bounded distance and wander around the final stopping position; shots trigger flight.
- [Future hunting phases](Deer/AI_IMPLEMENTATION_PLAN.md): navigation polish, hearing, population and food harvesting not yet implemented.

Current active world still has one configurable deer preview. No wildlife population manager is introduced.
