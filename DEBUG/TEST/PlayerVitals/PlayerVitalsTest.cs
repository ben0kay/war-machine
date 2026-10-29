// Standalone PH0/PH2 smoke test against real player/shared-health/stamina code. No gameplay hotkeys or world mutations.
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
            PlayerStamina stamina = vitals.Stamina;
            Equal(stamina.Current, 100, "stamina starts full");
            Check(stamina.CanSprint && !stamina.IsPhysicsProcessing(), "full stamina is dormant");
            stamina.RecordSprint(false, 1);
            Equal(stamina.Current, 100, "walking costs no stamina");
            stamina.RecordSprint(true, 1); Equal(stamina.Current, 90, "sprint drains at configured rate");
            Check(!stamina.IsPhysicsProcessing(), "sprint uses movement, not another drain callback");
            stamina.RecordSprint(false, 0);
            stamina._PhysicsProcess(1); Equal(stamina.Current, 90, "recovery waits after exertion");
            stamina._PhysicsProcess(1); Equal(stamina.Current, 97.5f, "only post-delay time recovers");
            stamina._PhysicsProcess(1); Equal(stamina.Current, 100, "recovery clamps full");
            Check(!stamina.IsPhysicsProcessing(), "full recovery sleeps");
            Check(stamina.TryJump(), "funded jump accepted"); Equal(stamina.Current, 85, "jump cost once");
            Check(!stamina.TrySpend(1000), "unfunded exact action rejected");
            Equal(stamina.Current, 85, "rejected action spends nothing");
            stamina.SpendPartial(1000);
            Check(stamina.Exhausted && !stamina.CanSprint && !stamina.TryJump(), "depletion gates sprint and jump");
            Equal(vitals.CurrentHealth, 100, "stamina depletion does not damage vitality");
            stamina._PhysicsProcess(1.5); stamina._PhysicsProcess(1);
            Equal(stamina.Current, 15, "walking/rest recovery");
            Check(stamina.Exhausted && !stamina.CanSprint, "sprint remains locked below recovery threshold");
            stamina._PhysicsProcess(0.34);
            Check(stamina.CanSprint && !stamina.Exhausted, "20 percent unlocks sprint");
            stamina._PhysicsProcess(10);
            foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
            { stamina.SpendPartial(invalid); Check(!stamina.TrySpend(invalid), "invalid exact cost rejected"); }
            Equal(stamina.Current, 100, "invalid costs ignored");
            stamina.SpendPartial(0.5f); Equal(stamina.Current, 99.5f, "small accepted-shot exertion");
            stamina._PhysicsProcess(2);
            Equal(settings.Vitals.Stamina.Maximum, 100, "stamina master unchanged");
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
            Check(stamina.IsStopped && !stamina.IsPhysicsProcessing(), "death stops stamina recovery");
            float deathStamina = stamina.Current; stamina._PhysicsProcess(10); stamina.SpendPartial(1);
            Equal(stamina.Current, deathStamina, "dead stamina cannot recover or spend");
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
