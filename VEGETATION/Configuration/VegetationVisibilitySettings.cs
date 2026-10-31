// GV1: one globally authored vegetation visibility/LOD policy for active streamed tree and grass systems.
using Godot;

[GlobalClass]
public partial class VegetationVisibilitySettings : Resource
{
    [ExportGroup("Fog-linked visibility")]
    // Only used when distance fog is OFF or not fully opaque. Fog ON has no separate vegetation cap.
    [Export(PropertyHint.Range, "48,512,8")] public float NoFogDistance { get; set; } = 160f;
    // Native MultiMesh range checks batch centres; keep a safety band behind the opaque fog.
    [Export(PropertyHint.Range, "8,48,2")] public float FogSafetyBuffer { get; set; } = 24f;

    [ExportGroup("Shared LOD thresholds")]
    [Export(PropertyHint.Range, "0.15,0.6,0.01")] public float Lod0EndFraction { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0.45,0.9,0.01")] public float Lod1EndFraction { get; set; } = 0.70f;
    [Export(PropertyHint.Range, "2,16,1")] public float LodTransitionMargin { get; set; } = 6f;
}
