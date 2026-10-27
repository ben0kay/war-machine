// Reusable health state: resolves incoming packets and notifies the actor, with no polling/physics loop.
using Godot;
using System;

public partial class HealthComponent : Node
{
    public event Action<DamageResult>? Damaged;
    public event Action<DamagePacket>? Died;
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDead { get; private set; }
    public HealthSettings RuntimeSettings { get; private set; } = null!;
    private bool _configured;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Explicit initialization/reset; never write runtime damage back into an authored .tres master.
    public void Configure(HealthSettings? settings)
    {
        RuntimeSettings = settings != null ? (HealthSettings)settings.Duplicate(true) : new HealthSettings();
        MaxHealth = float.IsFinite(RuntimeSettings.MaxHealth) ? Mathf.Max(1, RuntimeSettings.MaxHealth) : 100;
        CurrentHealth = MaxHealth; IsDead = false; _configured = true;
    }

    // =========================================================
    // Death is latched before callbacks so additional/reentrant hits cannot notify death twice.
    public DamageResult ApplyDamage(in DamagePacket hit)
    {
        if (!_configured || IsDead) return new DamageResult(hit, 0, 0);
        DamageResult result = DamageResolver.Resolve(hit, RuntimeSettings);
        if (result.Amount <= 0) return result;
        CurrentHealth = Mathf.Max(0, CurrentHealth - result.Amount);
        bool lethal = CurrentHealth <= 0;
        if (lethal) IsDead = true;
        Damaged?.Invoke(result);
        if (lethal) Died?.Invoke(hit);
        return result;
    }
}
