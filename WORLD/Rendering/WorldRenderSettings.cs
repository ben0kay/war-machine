// WS1 visual policy only. Authored ranges, collisions, actors and section residency remain independently owned.
using Godot;

[GlobalClass]
public partial class WorldRenderSettings : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "4,64,1")] public float HiddenBuffer { get; set; } = 8;
    [Export(PropertyHint.Range, "1,8,0.5")] public float CameraRefreshStep { get; set; } = 2;
    [Export(PropertyHint.Range, "0,160,1")] public float ShadowBuffer { get; set; } = 48;
    [Export(PropertyHint.Range, "0,8,0.5")] public float DeformationPadding { get; set; } = 2;
    [Export] public bool FadeLocalLights { get; set; } = true;
    [Export(PropertyHint.Range, "1,32,1")] public float LightFadeLength { get; set; } = 8;
}
