// Regression for the deer startup cascade: resolve the live child even if the inherited exported wrapper was disposed.
using Godot;
using System;

public partial class DeerStartupTest : Node
{
    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(Run)); }

    // =========================================================
    public void Run()
    {
        try
        {
            var deer = GD.Load<PackedScene>("res://ENTITIES/TYPES/Wildlife/Deer/Deer.tscn").Instantiate<DeerController>();
            var stale = new Node3D(); stale.Free(); deer.VisualRoot = stale;
            AddChild(deer);
            if (!deer.IsAiReady || !IsInstanceValid(deer.VisualRoot)
                || deer.VisualRoot != deer.GetNode<DeerMeshBuilder>("VisualRoot"))
                throw new InvalidOperationException("Deer did not resolve its live visual child and complete Ready.");
            var target = new Node3D(); AddChild(target);
            deer.InitializeTarget(target, 123);
            if (!deer.IsActive) throw new InvalidOperationException("Deer did not activate after completed setup.");
            deer.Deactivate(); deer.Free();
            GD.Print("[Deer startup] PASS: stale-reference recovery, Ready, target configuration and activation");
            GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[Deer startup] FAIL: {error}"); GetTree().Quit(1); }
    }
}
