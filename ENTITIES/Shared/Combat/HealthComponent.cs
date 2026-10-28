// Reusable health state: resolves incoming packets and notifies the actor, with no polling/physics loop.
using Godot;
using System;

public partial class HealthComponent : Node
{
    public event Action<DamageResult>? Damaged;
    public event Action<DamagePacket>? Died;
    // Generic health observers receive the source rather than an ambiguous damage amount.
    public event Action<HealthComponent>? HealthChanged;
    public const string FeedbackGroup = "health_components";
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDead { get; private set; }
    public HealthSettings RuntimeSettings { get; private set; } = null!;
    private bool _configured, _changing;

    // =========================================================
    public override void _Ready()
    { AddToGroup(FeedbackGroup); SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Explicit initialization/reset; never write runtime damage back into an authored .tres master.
    public void Configure(HealthSettings? settings)
    {
        if (_changing) return;
        RuntimeSettings = settings != null ? (HealthSettings)settings.Duplicate(true) : new HealthSettings();
        MaxHealth = float.IsFinite(RuntimeSettings.MaxHealth) ? Mathf.Max(1, RuntimeSettings.MaxHealth) : 100;
        CurrentHealth = MaxHealth; IsDead = false; _configured = true;
        HealthChanged?.Invoke(this);
    }

    // =========================================================
    // Death is latched before callbacks so additional/reentrant hits cannot notify death twice.
    public DamageResult ApplyDamage(in DamagePacket hit)
    {
        if (!_configured || IsDead) return new DamageResult(hit, 0, 0);
        DamageResult result = DamageResolver.Resolve(hit, RuntimeSettings);
        return ApplyResolvedDamage(result);
    }

    // =========================================================
    // Trusted coordinator path: the caller already used DamageResolver. Never mitigate this result again.
    public DamageResult ApplyResolvedDamage(in DamageResult result)
    {
        if (!_configured || IsDead || _changing || !float.IsFinite(result.Amount) || result.Amount <= 0)
            return new DamageResult(result.Hit, 0, result.Mitigated);
        _changing = true;
        try
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - result.Amount);
            bool lethal = CurrentHealth <= 0;
            if (lethal) IsDead = true;
            Damaged?.Invoke(result);
            HealthChanged?.Invoke(this);
            if (lethal) Died?.Invoke(result.Hit);
            return result;
        }
        finally { _changing = false; }
    }

    // =========================================================
    // Return actual restored health; ordinary healing cannot reset death or emit unchanged snapshots.
    public float Heal(float amount)
    {
        if (!_configured || IsDead || _changing || !float.IsFinite(amount) || amount <= 0) return 0;
        float previous = CurrentHealth;
        CurrentHealth = (float)Math.Min(MaxHealth, (double)previous + amount);
        float restored = CurrentHealth - previous;
        if (restored <= 0) return 0;
        _changing = true;
        try { HealthChanged?.Invoke(this); }
        finally { _changing = false; }
        return restored;
    }
}
