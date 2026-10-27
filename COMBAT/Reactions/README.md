# Reaction state

HitReaction is a small value state with active duration, strength/direction and re-flinch deadline. TryStart refuses active/cooldown hits and reserves an explicit recovery gap. Advance is called only by an already-active recipient's existing physics callback; Reset clears lifecycle state. It creates no nodes/timers or idle work. The recipient owns movement/visuals and immunity policy. Current consumer: ZombieController. See ../README.md for tuning and deferred local validation.
