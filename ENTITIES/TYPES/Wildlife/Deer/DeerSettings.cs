// Per-species deer identity, health and simple procedural colour controls.
// No AI or movement settings until wildlife behaviour is explicitly implemented.
using Godot;

[GlobalClass]
public partial class DeerSettings : EntitySettings
{
    [ExportGroup("Health")]
    [Export] public HealthSettings HealthProfile { get; set; } = null!;

    [ExportGroup("Appearance")]
    [Export] public Color CoatColor { get; set; } = new(0.48f, 0.29f, 0.16f);
    [Export] public Color BellyColor { get; set; } = new(0.72f, 0.62f, 0.47f);
    [Export] public Color HoofColor { get; set; } = new(0.15f, 0.13f, 0.11f);
    [Export] public Color AntlerColor { get; set; } = new(0.43f, 0.33f, 0.22f);
    [Export] public bool HasAntlers { get; set; } = true;
}
