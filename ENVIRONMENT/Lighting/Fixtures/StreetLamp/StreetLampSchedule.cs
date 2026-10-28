// One event-driven streetlamp day/night scheduler, using Sky3D's actual time.
// Lamps register only when instantiated; no independent clock, lamp polling or scene tree scans.
using Godot;
using System.Collections.Generic;

public partial class StreetLampSchedule : Node
{
    private readonly HashSet<StreetLampLighting> _lamps = new();
    private SkyController? _sky;
    private StreetLampScheduleSettings _settings = null!;
    private bool _on;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Bind after SkyController is ready. New lamps are brought into the current policy immediately.
    public void Initialize(SkyController sky, StreetLampScheduleSettings settings)
    {
        _sky = sky; _settings = (StreetLampScheduleSettings)settings.Duplicate(true);
        _on = ShouldBeOn(sky.CurrentHour);
        StreetLampLighting.Available += OnLampAvailable;
        StreetLampLighting.Removed += OnLampRemoved;
        sky.TimeChanged += OnTimeChanged;
        // Catch fixtures that happened to enter the scene before this manager was created.
        foreach (Node node in GetTree().GetNodesInGroup("streetlamp_scheduled"))
            if (node is StreetLampLighting lamp) OnLampAvailable(lamp);
    }

    // =========================================================
    private void OnLampAvailable(StreetLampLighting lamp)
    {
        if (!IsInstanceValid(lamp) || !_lamps.Add(lamp)) return;
        lamp.SetEnabled(_on && lamp.Settings != null && lamp.Settings.LightsEnabled);
    }

    // =========================================================
    private void OnLampRemoved(StreetLampLighting lamp) => _lamps.Remove(lamp);

    // =========================================================
    // Clock may update 10x/s but lamp writes happen only when the scheduled state changes.
    private void OnTimeChanged(float hour)
    {
        bool on = ShouldBeOn(hour);
        if (on == _on) return;
        _on = on;
        foreach (var lamp in _lamps)
            if (IsInstanceValid(lamp)) lamp.SetEnabled(_on && lamp.Settings != null && lamp.Settings.LightsEnabled);
    }

    // =========================================================
    // Wrap across midnight. Matching times mean "on all day"; disabling schedule preserves fixture master.
    private bool ShouldBeOn(float hour)
    {
        if (!_settings.AutomaticTimeSwitching) return true;
        float on = Mathf.PosMod(_settings.TurnOnHour, 24f);
        float off = Mathf.PosMod(_settings.TurnOffHour, 24f);
        hour = Mathf.PosMod(hour, 24f);
        if (Mathf.IsEqualApprox(on, off)) return true;
        return on > off ? hour >= on || hour < off : hour >= on && hour < off;
    }

    // =========================================================
    // Dynamic config changes can be reapplied explicitly, without another manager tick.
    public void ApplySettings()
    {
        if (!IsInstanceValid(_sky)) return;
        bool on = ShouldBeOn(_sky!.CurrentHour);
        _on = on;
        foreach (var lamp in _lamps)
            if (IsInstanceValid(lamp)) lamp.SetEnabled(_on && lamp.Settings != null && lamp.Settings.LightsEnabled);
    }

    // =========================================================
    public override void _ExitTree()
    {
        StreetLampLighting.Available -= OnLampAvailable;
        StreetLampLighting.Removed -= OnLampRemoved;
        if (IsInstanceValid(_sky)) _sky!.TimeChanged -= OnTimeChanged;
        _lamps.Clear();
    }
}
