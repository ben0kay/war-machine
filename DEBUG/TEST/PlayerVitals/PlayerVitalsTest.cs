// Standalone PH0 smoke test against real player/shared-health code. No gameplay hotkeys or world mutations.
using Godot;
using System;

public partial class PlayerVitalsTest : Node
{
    private int _checks;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); CallDeferred(nameof(Run)); }

    // =========================================================
    public void Run()
    {
        try
        {
            var camera = new Camera3D(); AddChild(camera);
            var player = GD.Load<PackedScene>("res://PLAYER/Player.tscn").Instantiate<PlayerController>(); AddChild(player);
            player.ReceiveHit(Packet(50)); // Not initialized: must be harmless.
            var settings = GD.Load<PlayerSettings>("res://PLAYER/Configuration/PlayerMaster.tres");
            player.Initialize((PlayerSettings)settings.Duplicate(true), camera);
            PlayerVitals vitals = player.Vitals;
            Equal(vitals.CurrentHealth, 100, "initial vitality");
            for (int i = 0; i < BodyCondition.RegionCount; i++) Equal(vitals.Body.Current((BodyRegion)i), 100, "initial region");
            foreach (BodyRegion region in new[] { BodyRegion.LeftLeg, BodyRegion.RightLeg, BodyRegion.LeftArm, BodyRegion.RightArm })
            {
                vitals.ReceiveHit(Packet(25), region);
                Equal(vitals.Body.Current(region), 75, "independent limb damage");
                Equal(vitals.CurrentHealth, 100, "limb cannot harm vitality");
            }
            player.ReceiveHit(Packet(25));
            Equal(vitals.Body.Current(BodyRegion.Torso), 75, "capsule defaults torso");
            Equal(vitals.CurrentHealth, 75, "critical vitality damage");
            vitals.HealRegion(BodyRegion.LeftLeg, 10); Equal(vitals.Body.Current(BodyRegion.LeftLeg), 85, "regional heal");
            Equal(vitals.CurrentHealth, 75, "regional heal leaves vitality");
            vitals.HealVitality(10); Equal(vitals.CurrentHealth, 85, "vitality heal");
            Equal(vitals.Body.Current(BodyRegion.Torso), 75, "vitality heal leaves anatomy");
            foreach (float invalid in new[] { -1f, 0f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                vitals.ReceiveHit(Packet(invalid), BodyRegion.Head);
                Equal(vitals.HealVitality(invalid), 0, "invalid vitality heal");
                Equal(vitals.HealRegion(BodyRegion.LeftLeg, invalid), 0, "invalid region heal");
            }
            Equal(vitals.Body.Current(BodyRegion.Head), 100, "invalid damage ignored");
            vitals.HealRegion(BodyRegion.LeftLeg, float.MaxValue);
            Equal(vitals.Body.Current(BodyRegion.LeftLeg), 100, "regional heal clamp");
            Equal(vitals.HealRegion(BodyRegion.LeftLeg, 10), 0, "unchanged heal");
            vitals.ReceiveHit(Packet(1000), BodyRegion.RightLeg);
            Equal(vitals.Body.Current(BodyRegion.RightLeg), 0, "limb clamp");
            Check(!vitals.IsDead, "zero limb is not death");
            vitals.Health.RuntimeSettings.Armour = 10; vitals.Health.RuntimeSettings.PhysicalResistance = 0.5f;
            bool coherent = false;
            vitals.Health.HealthChanged += health => coherent = vitals.Body.Current(BodyRegion.Head) == 85;
            vitals.ReceiveHit(Packet(40), BodyRegion.Head);
            Equal(vitals.Body.Current(BodyRegion.Head), 85, "mitigation once on region");
            Equal(vitals.CurrentHealth, 70, "mitigation once on vitality");
            Check(coherent, "health callback sees committed anatomy");
            int deaths = 0;
            vitals.Died += () => { deaths++; player.ReceiveHit(Packet(1000)); vitals.HealVitality(1000); };
            vitals.ReceiveHit(Packet(1000), BodyRegion.Torso);
            Check(vitals.IsDead && player.GameplayInputBlocked, "death blocks player");
            player.SetGameplayInputBlocked(false);
            Check(player.GameplayInputBlocked, "backpack close cannot release death gate");
            Equal(vitals.HealVitality(1000), 0, "dead vitality heal rejected");
            Equal(vitals.Body.HealRegion(BodyRegion.RightLeg, 1000), 0, "dead direct region heal rejected");
            player.ReceiveHit(Packet(1000)); Equal(deaths, 1, "one latched death");
            Check(player.IsInsideTree() && !player.IsQueuedForDeletion(), "dead player stays for camera");
            Equal(settings.Vitals.Health.MaxHealth, 100, "master unchanged");
            Equal(settings.Vitals.Health.Armour, 0, "runtime defence does not mutate master");
            var npc = new HealthComponent(); AddChild(npc); npc.Configure(new HealthSettings { MaxHealth = 100 });
            npc.ApplyDamage(Packet(95)); Equal(npc.CurrentHealth, 5, "NPC rifle regression");
            npc.ApplyDamage(Packet(95)); Check(npc.IsDead, "NPC death regression");
            GD.Print($"[PH0] PASS: {_checks} checks; visual/world/FPS checks remain manual."); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError($"[PH0] FAIL: {error}"); GetTree().Quit(1); }
    }

    // =========================================================
    private static DamagePacket Packet(float amount) => new(amount, DamageType.Physical, null, Vector3.Zero, Vector3.Forward);

    // =========================================================
    private void Equal(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < 0.001f, message);

    // =========================================================
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new InvalidOperationException(message); }
}
