// Shared foliage tint source of truth; runtime copies push GPU globals without renderer readback.
using Godot;

[GlobalClass]
public partial class FoliageTintSettings : Resource
{
    [ExportGroup("Shared tree and grass tint")]
    [Export] public Color Tint { get; set; } = new(0.43f, 0.55f, 0.37f, 1f);
    [Export(PropertyHint.Range, "0,1,0.01")] public float Strength { get; set; } = 0.25f;

    // =========================================================
    // Only call at world startup or on explicit F1 edits; the two shader globals
    // must already be declared in project.godot. No synchronized GET calls.
    public void ApplyShaderGlobals()
    {
        RenderingServer.GlobalShaderParameterSet("wm_foliage_tint", Tint);
        RenderingServer.GlobalShaderParameterSet("wm_foliage_tint_strength", Mathf.Clamp(Strength, 0f, 1f));
    }
}
