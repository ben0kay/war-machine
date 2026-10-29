// Species-independent opt-in soft movement modifier; updates only on overlap enter/exit/removal.
using System.Collections.Generic;

public interface ICorpseSlowable
{
    bool CorpseSlowdownEnabled { get; }
    void SetCorpseSlowdown(ulong corpseId, float multiplier, float floor);
    void RemoveCorpseSlowdown(ulong corpseId);
}

public sealed class CorpseMovementModifiers
{
    private readonly Dictionary<ulong, (float Multiplier, float Floor)> _sources = new();
    public float Multiplier { get; private set; } = 1;
    // =========================================================
    public void Set(ulong id, float multiplier, float floor) { _sources[id] = (multiplier, floor); Refresh(); }
    // =========================================================
    public void Remove(ulong id) { if (_sources.Remove(id)) Refresh(); }
    // =========================================================
    // Additive penalties with a strict floor; never exponentially multiply overlapping areas.
    private void Refresh()
    {
        float penalty = 0, floor = 0;
        foreach (var source in _sources.Values) { penalty += 1 - source.Multiplier; floor = System.Math.Max(floor, source.Floor); }
        Multiplier = System.Math.Max(floor, 1 - penalty);
    }
}
