// IX0 noncombat toggle fixture only: no item identity, carrying, light system or idle simulation.
using Godot;

public partial class InteractionTestSwitch : StaticBody3D, IInteractable
{
    private static readonly StandardMaterial3D Off = new() { AlbedoColor = new Color(0.7f, 0.3f, 0.12f) };
    private static readonly StandardMaterial3D On = new() { AlbedoColor = new Color(0.15f, 0.7f, 0.48f) };
    private bool _on;
    private MeshInstance3D _indicator = null!;
    // =========================================================
    public override void _Ready()
    {
        _indicator = GetNode<MeshInstance3D>("Indicator"); _indicator.MaterialOverride = Off;
        SetProcess(false); SetPhysicsProcess(false);
    }
    // =========================================================
    public bool TryGetAction(in InteractionContext context, out InteractionAction action)
    {
        action = new InteractionAction(InteractionActionKind.Toggle, _on ? "IX0 test switch: turn OFF" : "IX0 test switch: turn ON");
        return !IsQueuedForDeletion();
    }
    // =========================================================
    public bool TryInteract(in InteractionContext context, InteractionActionKind action)
    {
        if (action != InteractionActionKind.Toggle || IsQueuedForDeletion()) return false;
        _on = !_on; _indicator.MaterialOverride = _on ? On : Off; return true;
    }
}
