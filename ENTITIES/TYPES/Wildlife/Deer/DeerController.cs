// Passive deer actor: shared EntityCore lifecycle plus the standard damage/health component.
// No movement, senses, attack, timer, gravity polling or AI; world placement supplies initial grounding.
using Godot;

public partial class DeerController : EntityCore, IDamageReceiver
{
    public float Health => _health?.CurrentHealth ?? 0f;
    public bool IsDead => _health?.IsDead ?? false;
    private HealthComponent? _health;

    // =========================================================
    // The shared health component is registered before base activation and is independent per deer.
    public override void _Ready()
    {
        _health = GetNode<HealthComponent>("Health");
        base._Ready();
        var config = (DeerSettings)RuntimeSettings;
        _health.Died += OnKilled;
        _health.Configure(config.HealthProfile);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Existing firearm/melee hit dispatch resolves health with the same rules as a zombie.
    public void ReceiveHit(DamagePacket hit)
    {
        if (IsActive && _health != null && !_health.IsDead)
            _health.ApplyDamage(hit);
    }

    // =========================================================
    // No death animation or loot yet: stop the actor and remove it immediately like current zombies.
    private void OnKilled(DamagePacket hit)
    {
        Deactivate();
        CollisionLayer = 0; CollisionMask = 0;
        Hide(); QueueFree();
    }

    // =========================================================
    // Release C# event references if the scene is unloaded or the deer dies.
    protected override void OnEntityExitTree()
    {
        if (_health != null && GodotObject.IsInstanceValid(_health))
            _health.Died -= OnKilled;
    }
}
