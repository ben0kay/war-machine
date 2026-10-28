// Isolated WS2 walker. Input wakes physics; settled movement sleeps. No gameplay/player/actor ownership changes.
using Godot;
using System;

public partial class StreamingProofWalker : CharacterBody3D
{
    public event Action<Vector3>? Moved;
    public Func<Vector3, bool>? CanEnter;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo) SetPhysicsProcess(true);
    }

    // =========================================================
    public override void _PhysicsProcess(double delta)
    {
        Vector3 direction = new(
            (Input.IsPhysicalKeyPressed(Key.D) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) ? 1 : 0), 0,
            (Input.IsPhysicalKeyPressed(Key.S) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.W) ? 1 : 0));
        float speed = Input.IsPhysicalKeyPressed(Key.Shift) ? 80 : 32;
        Velocity = direction.Normalized() * speed + Vector3.Down * 2;
        if (CanEnter == null || CanEnter(GlobalPosition + Velocity * (float)delta)) MoveAndSlide();
        Moved?.Invoke(GlobalPosition);
        if (direction == Vector3.Zero) { Velocity = Vector3.Zero; SetPhysicsProcess(false); }
    }
}
