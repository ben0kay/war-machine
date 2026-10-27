// Reusable target health/defences resource. Embed in an actor's master or share an authored profile.
using Godot;

[GlobalClass]
public partial class HealthSettings : Resource
{
    [ExportGroup("Health")]
    [Export] public float MaxHealth { get; set; } = 100;
    [ExportGroup("Defences")]
    [Export] public float Armour { get; set; }
    [Export(PropertyHint.Range, "-1,1,0.01")] public float PhysicalResistance { get; set; }
    [Export(PropertyHint.Range, "-1,1,0.01")] public float EnergyResistance { get; set; }
    [Export(PropertyHint.Range, "-1,1,0.01")] public float ExplosiveResistance { get; set; }
    [Export(PropertyHint.Range, "-1,1,0.01")] public float FireResistance { get; set; }

    // =========================================================
    // Fractions: 0.2 is 20% reduction, negative values are vulnerabilities; 1 grants immunity.
    public float ResistanceFor(DamageType type) => type switch
    {
        DamageType.Physical => PhysicalResistance,
        DamageType.Energy => EnergyResistance,
        DamageType.Explosive => ExplosiveResistance,
        DamageType.Fire => FireResistance,
        _ => 0
    };
}
