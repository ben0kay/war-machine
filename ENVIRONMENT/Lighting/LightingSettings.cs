// Authoring-only defaults for future local lighting. No light nodes or animation are created here.
using Godot;

[GlobalClass]
public partial class LightingSettings : Resource
{
    [ExportGroup("Local lights (future)")]
    [Export] public bool Enabled { get; set; } = true;
    // A design safety budget for future pooled, transient effects; not enforced yet.
    [Export(PropertyHint.Range, "1,16,1")] public int MaxTransientLights { get; set; } = 4;

    [ExportGroup("Weapon muzzle light (future)")]
    [Export] public bool MuzzleLightEnabled { get; set; } = true;
    [Export] public Color MuzzleLightColor { get; set; } = new(1f, 0.78f, 0.52f);
    [Export(PropertyHint.Range, "0,8,0.1")] public float MuzzleLightEnergy { get; set; } = 2f;
    [Export(PropertyHint.Range, "0.5,15,0.1")] public float MuzzleLightRange { get; set; } = 4f;
    [Export(PropertyHint.Range, "0.02,0.2,0.01")] public float MuzzleLightDurationSeconds { get; set; } = 0.07f;
}
