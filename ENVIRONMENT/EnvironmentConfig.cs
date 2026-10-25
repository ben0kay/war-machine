// Central atmosphere/time and shared grass wind settings; Sky3D remains the clock owner.
using Godot;

[GlobalClass]
public partial class EnvironmentConfig : Resource
{
    [ExportGroup("Sky3D Time")]
    [Export(PropertyHint.Range, "0,23.99,0.01")] public float StartHour { get; set; } = 9f;
    [Export(PropertyHint.Range, "1,1440,1")] public float DayLengthMinutes { get; set; } = 24f;
    [Export] public float PreviewDayMinutes { get; set; } = 1f;
    [Export] public float UpdateInterval { get; set; } = 0.1f;
    [Export] public bool StartPaused { get; set; }
    [ExportGroup("Shared Wind")]
    [Export] public Vector2 WindDirection { get; set; } = new(1, 0.4f);
    [Export] public float GrassWindStrength { get; set; } = 0.22f;
    [Export] public float GrassWindSpeed { get; set; } = 1.4f;
}
