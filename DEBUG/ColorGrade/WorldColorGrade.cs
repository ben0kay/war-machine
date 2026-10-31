// GD1: optional single-pass screen-space grade beneath HUD and F1 layers.
// Settings only apply when edited; neutral values disable the full-screen draw entirely.
using Godot;

public partial class WorldColorGrade : CanvasLayer
{
    public WorldColorGradeSettings Settings { get; private set; } = null!;
    private ColorRect _surface = null!;
    private ShaderMaterial _material = null!;

    // =========================================================
    // CanvasLayer 0 runs above the 3D render but before default debug CanvasLayer 1 and HUD 2.
    public override void _Ready()
    {
        Layer = 0;
        _material = new ShaderMaterial {
            Shader = GD.Load<Shader>("res://DEBUG/ColorGrade/Shaders/GlobalColorGrade.gdshader")
        };
        _surface = new ColorRect {
            Name = "ScreenGrade", Material = _material,
            Color = Colors.White, MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_surface);
        _surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _surface.Hide();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    public void Initialize(WorldColorGradeSettings settings)
    {
        Settings = (WorldColorGradeSettings)settings.Duplicate(true);
        ApplySettings();
    }

    // =========================================================
    // No GPU pass at neutral values, no permanent polling, one material uniform update per slider input.
    public void ApplySettings()
    {
        if (Settings == null || _surface == null) return;
        float hue = Mathf.Clamp(Settings.HueDegrees, -180f, 180f);
        float sat = Mathf.Clamp(Settings.Saturation, 0f, 2f);
        float contrast = Mathf.Clamp(Settings.Contrast, 0.5f, 1.5f);
        float brightness = Mathf.Clamp(Settings.Brightness, 0.5f, 1.5f);
        bool neutral = Mathf.IsZeroApprox(hue) && Mathf.IsEqualApprox(sat, 1f)
            && Mathf.IsEqualApprox(contrast, 1f) && Mathf.IsEqualApprox(brightness, 1f);
        if (!Settings.Enabled || neutral) { _surface.Hide(); return; }
        _material.SetShaderParameter("hue_degrees", hue);
        _material.SetShaderParameter("saturation", sat);
        _material.SetShaderParameter("contrast", contrast);
        _material.SetShaderParameter("brightness", brightness);
        _surface.Show();
    }
}
