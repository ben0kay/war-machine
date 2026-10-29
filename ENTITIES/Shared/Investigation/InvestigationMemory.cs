// Passive fixed-point memory and per-episode give-up policy; no actor references, AI, physics, timers or callbacks.
using Godot;

public enum InvestigationPhase { None, Travel, Look }
public enum InvestigationClue { LastSeen, Hit, Sound }

public sealed class InvestigationMemory
{
    private readonly InvestigationSettings _settings;
    public InvestigationPhase Phase { get; private set; }
    public InvestigationClue Clue { get; private set; }
    public Vector3 Point { get; private set; }
    public Vector3 Destination { get; private set; }
    public bool HasPoint { get; private set; }
    public bool IsActive => Phase != InvestigationPhase.None;
    public bool Thorough { get; private set; }
    private ulong _searchUntil, _lookUntil;

    // =========================================================
    public InvestigationMemory(InvestigationSettings settings)
    { _settings = (InvestigationSettings)settings.Duplicate(true); }

    // =========================================================
    public bool Remember(Vector3 point, InvestigationClue clue)
    {
        if (!point.IsFinite()) return false;
        Point = point; Clue = clue; HasPoint = true; return true;
    }

    // =========================================================
    // Confirmed perception supersedes investigation but keeps a last-seen snapshot for subsequent sight loss.
    public void Observe(Vector3 point)
    {
        if (!Remember(point, InvestigationClue.LastSeen)) return;
        Phase = InvestigationPhase.None; Thorough = false; _searchUntil = _lookUntil = 0;
    }

    // =========================================================
    // Same seeded draws and sanitized ranges as the original zombie PER2 episode; no hidden target reads.
    public bool Begin(Vector3 from, float speed, ulong now, RandomNumberGenerator random, Vector3? commitmentWeights = null)
    {
        if (!HasPoint || !from.IsFinite()) return false;
        Vector3 weights = commitmentWeights ?? _settings.Weights;
        float early = Safe(weights.X, 0, 100), middle = Safe(weights.Y, 0, 100);
        float full = Safe(weights.Z, 0, 100), total = early + middle + full;
        float choice = total > 0 ? random.Randf() * total : 0;
        Thorough = total == 0 || choice >= early + middle;
        float fraction = Thorough ? 1 : Sample(random, choice < early ? _settings.EarlyFraction : _settings.ModerateFraction, 0.05f, 1);
        Destination = from.Lerp(Point, fraction);
        Vector3 distance = Destination - from; distance.Y = 0;
        float travel = distance.Length() / Safe(speed, 0.1f, 30);
        float seconds = travel * Sample(random, _settings.TimeScale, 1, 3) + Safe(_settings.TimeAllowance, 0.5f, 20);
        seconds = Mathf.Min(seconds, Safe(_settings.MaximumSeconds, 1, 120));
        _searchUntil = now + Milliseconds(seconds); _lookUntil = 0; Phase = InvestigationPhase.Travel;
        return true;
    }

    // =========================================================
    public bool Arrive(ulong now, RandomNumberGenerator random)
    {
        if (Phase != InvestigationPhase.Travel || !Thorough) return false;
        Phase = InvestigationPhase.Look;
        _lookUntil = now + Milliseconds(Sample(random, _settings.LookSeconds, 0.1f, 5));
        return true;
    }

    // =========================================================
    public bool Expired(ulong now)
        => IsActive && (now >= _searchUntil || (Phase == InvestigationPhase.Look && now >= _lookUntil));

    // =========================================================
    public float LookOffset(ulong now)
        => Phase == InvestigationPhase.Look && now < _lookUntil ? Mathf.Sin((float)(_lookUntil - now) * 0.006f) * 0.55f : 0;

    // =========================================================
    public void Clear()
    {
        Phase = InvestigationPhase.None; HasPoint = Thorough = false;
        Point = Destination = Vector3.Zero; _searchUntil = _lookUntil = 0; Clue = InvestigationClue.LastSeen;
    }

    // =========================================================
    private static float Sample(RandomNumberGenerator random, Vector2 range, float min, float max)
    {
        float a = Safe(range.X, min, max), b = Safe(range.Y, min, max);
        return random.RandfRange(Mathf.Min(a, b), Mathf.Max(a, b));
    }

    // =========================================================
    private static float Safe(float value, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min;
    // =========================================================
    private static ulong Milliseconds(float seconds) => (ulong)Mathf.CeilToInt(seconds * 1000);
}
