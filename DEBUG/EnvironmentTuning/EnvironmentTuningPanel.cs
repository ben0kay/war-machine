// Compact event-driven tuning UI. Edits runtime copies of the existing master resources only.
using Godot;
using System;
using System.Collections.Generic;

public partial class EnvironmentTuningPanel : CanvasLayer
{
    private Control _panel = null!;
    private VBoxContainer _rows = null!;
    private SkyController _sky = null!;
    private TerrainGrass _grass = null!;
    private TreePlacement _trees = null!;
    private ExplorationController _explorer = null!;
    private readonly List<Action> _refresh = new();
    private readonly List<Action> _reset = new();
    private bool _syncing;
    private Input.MouseModeEnum _previousMouse;

    // =========================================================
    // Build controls once from property bindings; no polling or hidden-panel callbacks.
    public override void _Ready()
    {
        var world = GetParent();
        _sky = world.GetNode<SkyController>("Environment"); _grass = world.GetNode<TerrainGrass>("Vegetation");
        _trees = world.GetNode<TreePlacement>("Trees"); _explorer = world.GetNode<ExplorationController>("Explorer");
        Layer = 20;
        var frame = new PanelContainer { Position = new Vector2(16, 115), CustomMinimumSize = new Vector2(430, 0) };
        AddChild(frame); _panel = frame;
        var outer = new VBoxContainer(); frame.AddChild(outer);
        outer.AddChild(new Label { Text = "Environment tuning · F9 close" });
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(430, 470), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        outer.AddChild(scroll); var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(content);
        Category(content, "Fog / Atmosphere");
        Toggle(_sky.Settings, "FogEnabled", "Enabled", _sky.ApplyMist);
        Slider(_sky.Settings, "FogDensity", "Density", 0, 0.1, 0.0001, _sky.ApplyMist);
        Slider(_sky.Settings, "FogStart", "Start (m)", 0, 500, 1, _sky.ApplyMist);
        Slider(_sky.Settings, "FogEnd", "End (m)", 25, 2000, 1, _sky.ApplyMist);
        Slider(_sky.Settings, "FogFalloff", "Falloff", 0, 50, 0.1, _sky.ApplyMist);
        Category(content, "Sky / Time / Wind");
        Slider(_sky.Settings, "StartHour", "Hour", 0, 24, 0.01, _sky.ApplyTime);
        Slider(_sky.Settings, "DayLengthMinutes", "Day length (minutes)", 1, 1440, 1, _sky.ApplyCycle);
        Toggle(_sky.Settings, "StartPaused", "Pause time", _sky.ApplyCycle);
        Slider(_sky.Settings, "GrassWindStrength", "Shared wind strength", 0, 2, 0.01, ApplyWind);
        Slider(_sky.Settings, "GrassWindSpeed", "Shared wind speed", 0, 0.15, 0.001, ApplyWind);
        Category(content, "Grass");
        Slider(_grass.Settings, "Height", "Height", 0.1, 3, 0.05, _grass.ApplyAppearance);
        Slider(_grass.Settings, "Density", "Density *", 0, 2, 0.05);
        Slider(_grass.Settings, "RenderDistance", "Distance (m) *", 8, 128, 1);
        Slider(_grass.Settings, "WindMultiplier", "Grass wind", 0, 2, 0.01, _grass.ApplyAppearance);
        Category(content, "Pine trees");
        Slider(_trees.Settings, "HeightMultiplier", "Height multiplier *", 0.5, 2, 0.05);
        Slider(_trees.Settings, "FoliageDensity", "Foliage density *", 0.1, 2, 0.05);
        Slider(_trees.Settings, "Density", "Placement density *", 0, 1, 0.01);
        Slider(_trees.Settings, "LeafWindStrength", "Branch wind", 0, 2, 0.01, _trees.ApplyWind);
        outer.AddChild(new Label { Text = "* Pending until Apply. Terrain is never modified." });
        var actions = new HBoxContainer(); outer.AddChild(actions);
        Button(actions, "Apply / Rebuild vegetation", Rebuild);
        Button(actions, "Reset All", () => { foreach (var reset in _reset) reset(); Refresh(); _sky.ApplyTime(); _sky.ApplyCycle(); _sky.ApplyMist(); Rebuild(); });
        _panel.Hide(); SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Capture F9 before exploration input; preserve mouse mode and suspend flight while editing.
    public override void _Input(InputEvent input)
    {
        if (input is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.F9) return;
        bool opening = !_panel.Visible;
        if (opening) { _previousMouse = Input.MouseMode; _sky.Settings.StartHour = _sky.Sky.Get("current_time").AsSingle(); Refresh(); }
        _panel.Visible = opening; _explorer.ControlsBlocked = opening;
        _explorer.SetPhysicsProcess(false);
        Input.MouseMode = opening ? Input.MouseModeEnum.Visible : _previousMouse;
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Collapsible sections keep new settings easy to add without a scene-node forest.
    private void Category(VBoxContainer parent, string title)
    {
        var section = new VBoxContainer(); parent.AddChild(section);
        var button = new Button { Text = "▾ " + title, ToggleMode = true, ButtonPressed = true };
        section.AddChild(button); _rows = new VBoxContainer(); section.AddChild(_rows);
        var rows = _rows; button.Toggled += shown => { rows.Visible = shown; button.Text = (shown ? "▾ " : "▸ ") + title; };
    }

    // =========================================================
    // Bind exported resource properties, including numeric labels and per-setting reset.
    private void Slider(Resource resource, string property, string title, double min, double max, double step, Action? changed = null)
    {
        float initial = resource.Get(property).AsSingle();
        var row = new HBoxContainer(); _rows.AddChild(row);
        row.AddChild(new Label { Text = title, CustomMinimumSize = new Vector2(155, 0) });
        var slider = new HSlider { MinValue = min, MaxValue = max, Step = step, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(135, 0) };
        row.AddChild(slider); var value = new Label { CustomMinimumSize = new Vector2(60, 0) }; row.AddChild(value);
        Action sync = () => { slider.SetValueNoSignal(resource.Get(property).AsSingle()); value.Text = slider.Value.ToString(step < 0.01 ? "0.0000" : "0.##"); };
        Action reset = () => { resource.Set(property, initial); changed?.Invoke(); };
        _refresh.Add(sync); _reset.Add(reset);
        slider.ValueChanged += number => { if (_syncing) return; resource.Set(property, (float)number); value.Text = number.ToString(step < 0.01 ? "0.0000" : "0.##"); changed?.Invoke(); };
        Button(row, "↺", () => { reset(); sync(); }); sync();
    }

    // =========================================================
    // Boolean settings use the same runtime resource and reset contract as sliders.
    private void Toggle(Resource resource, string property, string title, Action changed)
    {
        bool initial = resource.Get(property).AsBool(); var row = new HBoxContainer(); _rows.AddChild(row);
        var check = new CheckButton { Text = title }; row.AddChild(check);
        Action sync = () => check.SetPressedNoSignal(resource.Get(property).AsBool());
        Action reset = () => { resource.Set(property, initial); changed(); };
        _refresh.Add(sync); _reset.Add(reset);
        check.Toggled += value => { if (_syncing) return; resource.Set(property, value); changed(); };
        Button(row, "↺", () => { reset(); sync(); }); sync();
    }

    // =========================================================
    private static void Button(Node parent, string text, Action action)
    { var button = new Button { Text = text }; parent.AddChild(button); button.Pressed += action; }
    // =========================================================
    private void Refresh() { _syncing = true; foreach (var sync in _refresh) sync(); _syncing = false; }
    // =========================================================
    private void ApplyWind() { _grass.ApplyAppearance(); _trees.ApplyWind(); }
    // =========================================================
    private void Rebuild() { _grass.ApplySettings(); _trees.ApplySettings(); }
}
