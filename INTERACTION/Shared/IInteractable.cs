// IX0 query/use boundary. Object state belongs to the target, never the selection controller.
using Godot;

public enum InteractionActionKind { Inspect, Toggle }

public readonly struct InteractionAction
{
    public readonly InteractionActionKind Kind;
    public readonly string Label;
    // =========================================================
    public InteractionAction(InteractionActionKind kind, string label) { Kind = kind; Label = label; }
}

public readonly struct InteractionContext
{
    public readonly Node3D Actor;
    public readonly Vector3 Position;
    // =========================================================
    public InteractionContext(Node3D actor, Vector3 position) { Actor = actor; Position = position; }
}

public interface IInteractable
{
    bool TryGetAction(in InteractionContext context, out InteractionAction action);
    bool TryInteract(in InteractionContext context, InteractionActionKind action);
}
