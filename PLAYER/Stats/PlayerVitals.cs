// Single player hit coordinator: mitigate once, commit anatomy, then optional critical vitality loss and events.
using Godot;
using System;

public partial class PlayerVitals : Node
{
    public HealthComponent Health { get; private set; } = null!;
    public BodyCondition Body { get; private set; } = null!;
    public float CurrentHealth => _configured ? Health.CurrentHealth : 0;
    public float MaxHealth => _configured ? Health.MaxHealth : 0;
    public bool IsDead => _configured && Health.IsDead;
    public bool IsConfigured => _configured;
    public event Action? Died;
    private bool _configured, _changing;
    private bool CanChange => _configured && !_changing && !Body.IsNotifying && !IsDead && !IsQueuedForDeletion();

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(PlayerVitalsSettings? settings)
    {
        if (_configured) return;
        var runtime = settings != null ? (PlayerVitalsSettings)settings.Duplicate(true) : new PlayerVitalsSettings();
        Health = GetNode<HealthComponent>("Health"); Body = GetNode<BodyCondition>("BodyCondition");
        Health.Configure(runtime.Health); Body.Initialize(runtime, Health);
        Health.Died += OnDied; _configured = true;
    }

    // =========================================================
    // Explicit regions are a test/future hurtbox route; current IDamageReceiver capsule callers use Unknown.
    public void ReceiveHit(in DamagePacket hit, BodyRegion region = BodyRegion.Unknown)
    {
        if (!CanChange) return;
        DamageResult result = DamageResolver.Resolve(hit, Health.RuntimeSettings);
        if (result.Amount <= 0) return;
        region = BodyCondition.ResolveRegion(region); _changing = true; Body.SetUpdating(true);
        try
        {
            bool changed = Body.ApplyResolvedDamage(region, result.Amount);
            if (region == BodyRegion.Head || region == BodyRegion.Torso) Health.ApplyResolvedDamage(result);
            if (changed) Body.Publish(region);
        }
        finally { Body.SetUpdating(false); _changing = false; }
    }

    // =========================================================
    public float HealVitality(float amount)
    {
        if (!CanChange) return 0;
        _changing = true; Body.SetUpdating(true);
        try { return Health.Heal(amount); }
        finally { Body.SetUpdating(false); _changing = false; }
    }

    // =========================================================
    public float HealRegion(BodyRegion region, float amount) => CanChange ? Body.HealRegion(region, amount) : 0;

    // =========================================================
    private void OnDied(DamagePacket hit) => Died?.Invoke();

    // =========================================================
    public override void _ExitTree()
    { if (_configured && IsInstanceValid(Health)) Health.Died -= OnDied; _configured = false; }
}
