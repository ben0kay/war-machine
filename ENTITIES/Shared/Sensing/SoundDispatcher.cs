// One world-local discrete notification route. No singleton, retained history, nodes, timers or listener scans.
using System;

public sealed class SoundDispatcher
{
    // HE1 registers active listeners explicitly, range-filters snapshots and unsubscribes on death/deactivation/exit.
    public event Action<SoundStimulus>? Emitted;

    // =========================================================
    internal void Publish(SoundStimulus stimulus) => Emitted?.Invoke(stimulus);

    // =========================================================
    internal void Clear() => Emitted = null;
}
