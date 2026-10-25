// Event-driven bridge into installed Sky3D 2.1; never computes a competing sun or clock.
using Godot;

public partial class SkyController : Node
{
    [Export] public EnvironmentConfig Settings { get; set; } = null!;
    [Export] public Node Sky { get; set; } = null!;
    [Export] public ShaderMaterial GrassMaterial { get; set; } = null!;
    private bool _paused, _preview;

    // =========================================================
    // Apply configuration once after Sky3D and its timer have entered the tree.
    public override void _Ready()
    {
        Sky.Set("current_time", Settings.StartHour);
        Sky.Set("minutes_per_day", Mathf.Max(1, Settings.DayLengthMinutes));
        Sky.Set("update_interval", Mathf.Max(0.016f, Settings.UpdateInterval));
        Sky.Set("editor_time_enabled", false);
        GrassMaterial.SetShaderParameter("wind_direction", Settings.WindDirection.Normalized());
        GrassMaterial.SetShaderParameter("wind_strength", Settings.GrassWindStrength);
        GrassMaterial.SetShaderParameter("wind_speed", Settings.GrassWindSpeed);
        SetPaused(Settings.StartPaused);
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // P pauses only solar time; F8 previews a full day in one configured minute.
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.P) SetPaused(!_paused);
        if (key.Keycode == Key.F8)
        {
            _preview = !_preview;
            Sky.Set("minutes_per_day", Mathf.Max(0.1f, _preview ? Settings.PreviewDayMinutes : Settings.DayLengthMinutes));
            GD.Print($"Accelerated sky preview: {_preview}");
        }
    }

    // =========================================================
    // Sky3D stops its timer while paused; cloud and grass shader motion remain intentional.
    private void SetPaused(bool paused)
    {
        _paused = paused; Sky.Set("game_time_enabled", !paused);
        GD.Print($"Solar time paused: {paused}");
    }
}
