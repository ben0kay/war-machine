// Six flat condition values, no limb nodes, ticks or gameplay penalties. Vitals coordinates damage atomically.
using Godot;
using System;

public partial class BodyCondition : Node
{
    public const int RegionCount = 6;
    public event Action<BodyRegion, float, float>? ConditionChanged;
    private readonly float[] _current = new float[RegionCount], _maximum = new float[RegionCount];
    private HealthComponent _life = null!;
    private bool _configured, _updating;
    internal bool IsNotifying { get; private set; }

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public static BodyRegion ResolveRegion(BodyRegion region)
        => (int)region >= 0 && (int)region < RegionCount ? region : BodyRegion.Torso;

    // =========================================================
    internal void Initialize(PlayerVitalsSettings settings, HealthComponent life)
    {
        if (_configured) return;
        _life = life;
        for (int i = 0; i < RegionCount; i++)
        {
            float value = settings.Maximum((BodyRegion)i);
            _current[i] = _maximum[i] = float.IsFinite(value) ? Mathf.Clamp(value, 1, 1000) : 100;
        }
        _configured = true;
    }

    // =========================================================
    public float Current(BodyRegion region) => _current[(int)ResolveRegion(region)];

    // =========================================================
    public float Maximum(BodyRegion region) => _maximum[(int)ResolveRegion(region)];

    // =========================================================
    internal void SetUpdating(bool updating) { _updating = updating; }

    // =========================================================
    // Commit before any life/death notification; the coordinator publishes only after both stores agree.
    internal bool ApplyResolvedDamage(BodyRegion region, float amount)
    {
        if (!_configured || _life.IsDead || !float.IsFinite(amount) || amount <= 0) return false;
        int index = (int)ResolveRegion(region); float previous = _current[index];
        _current[index] = Mathf.Max(0, previous - amount); return _current[index] != previous;
    }

    // =========================================================
    public float HealRegion(BodyRegion region, float amount)
    {
        if (region != ResolveRegion(region)) return 0;
        if (!_configured || _updating || IsNotifying || IsQueuedForDeletion() || _life.IsDead
            || !float.IsFinite(amount) || amount <= 0) return 0;
        region = ResolveRegion(region); int index = (int)region; float previous = _current[index];
        _current[index] = (float)Math.Min(_maximum[index], (double)previous + amount);
        float restored = _current[index] - previous;
        if (restored > 0) Publish(region);
        return restored;
    }

    // =========================================================
    internal void Publish(BodyRegion region)
    {
        IsNotifying = true;
        try { ConditionChanged?.Invoke(region, Current(region), Maximum(region)); }
        finally { IsNotifying = false; }
    }
}
