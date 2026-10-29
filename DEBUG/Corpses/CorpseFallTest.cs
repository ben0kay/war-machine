// ZD0 actual-code integration smoke: both zombie variants, one lethal handoff, frozen visual fall and no retained actor collision.
using Godot;
using System;
using System.Threading.Tasks;

public partial class CorpseFallTest : Node3D
{
    private int _checks;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(Run)); }

    // =========================================================
    public async void Run()
    {
        try
        {
            var floor = new StaticBody3D { CollisionLayer = 1 }; AddChild(floor);
            floor.AddChild(new CollisionShape3D { Position = new Vector3(0, -0.5f, 0), Shape = new BoxShape3D { Size = new Vector3(40, 1, 40) } });
            var root = new Node3D { Name = "Corpses" }; AddChild(root);
            var manager = new CorpseManager(); AddChild(manager);
            manager.Initialize(root, GD.Load<CorpseSettings>("res://ENTITIES/Shared/Corpses/Configuration/CorpseMaster.tres"));
            var target = new Node3D { Position = new Vector3(0, 0, 100) }; AddChild(target);
            foreach (string path in new[] { "res://ENTITIES/TYPES/Zombies/Zombie.tscn", "res://ENTITIES/TYPES/Zombies/Zombie_002/Zombie_002.tscn" })
            {
                var zombie = GD.Load<PackedScene>(path).Instantiate<ZombieController>(); AddChild(zombie);
                zombie.GlobalPosition = new Vector3(manager.SpawnedCount * 5, 0.06f, 0);
                zombie.CorpseOwner = manager; zombie.InitializeTarget(target, (uint)(123 + manager.SpawnedCount));
                Node3D visual = zombie.VisualRoot; ulong visualId = visual.GetInstanceId();
                var lethal = new DamagePacket(100000, DamageType.Physical, target, zombie.GlobalPosition, Vector3.Forward);
                int before = manager.SpawnedCount;
                zombie.ReceiveHit(lethal); zombie.ReceiveHit(lethal);
                Check(!zombie.IsActive && zombie.CollisionLayer == 0 && zombie.CollisionMask == 0, "death disables living actor");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Check(manager.SpawnedCount == before + 1, "exactly one corpse for duplicate lethal hits");
                Check(IsInstanceValid(visual) && visual.GetInstanceId() == visualId && visual.GetParent() is CorpseBody, "original visual transferred independently");
                var corpse = (CorpseBody)visual.GetParent();
                ulong deadline = Time.GetTicksMsec() + 6000;
                while (IsInstanceValid(corpse) && !corpse.IsSettled && Time.GetTicksMsec() < deadline)
                    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Check(IsInstanceValid(corpse) && corpse.IsSettled, "supported flat-ground fall settles");
                Check(corpse.Freeze && corpse.CollisionLayer == CorpseBody.PileLayer && corpse.CollisionMask == 0
                    && !corpse.IsProcessing() && !corpse.IsPhysicsProcessing(), "settled corpse has pile-only collision and no frame loops");
                Transform3D pose = corpse.GlobalTransform;
                for (int i = 0; i < 10; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                Check(corpse.GlobalTransform == pose, "settled world pose remains frozen");
            }
            Check(CorpseBody.FallDirection(new Vector3(float.NaN, 0, 1), 0) == Vector3.Back, "invalid direction uses deterministic fallback");
            Check(!manager.IsProcessing() && !manager.IsPhysicsProcessing(), "manager stays dormant");
            root.Free();
            GD.Print($"[ZD0] PASS: {_checks} checks"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[ZD0] FAIL: {error}"); GetTree().Quit(1); }
    }

    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException(message); }
}
