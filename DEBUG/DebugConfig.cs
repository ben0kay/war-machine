// Central settings for the lightweight TestWorld diagnostic overlay.
using Godot;

[GlobalClass]
public partial class DebugConfig : Resource
{
    [ExportGroup("FPS Overlay")]
    [Export] public bool ShowFps { get; set; } = true;
    [Export(PropertyHint.Range, "0.1,5,0.1")] public double FpsUpdateSeconds { get; set; } = 0.5;
}
