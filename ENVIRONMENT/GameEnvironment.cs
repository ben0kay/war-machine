// Shared environment prefab owner for both playable camera views; no per-frame coordination loop.
using Godot;

public partial class GameEnvironment : Node3D
{
    [Export] public EnvironmentConfig Settings { get; set; } = null!;
    public SkyController SkyController { get; private set; } = null!;
    public WeatherBridge Weather { get; private set; } = null!;
    public GroundHaze Haze { get; private set; } = null!;
    private Camera? _camera;
    private bool _viewInitialized;

    // =========================================================
    // Scene children finish first. One runtime settings copy feeds all existing owners in this instance.
    public override void _Ready()
    {
        Settings = (EnvironmentConfig)Settings.Duplicate(true);
        var sky = GetNode<WorldEnvironment>("Sky");
        sky.Environment.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        sky.Environment.AmbientLightSkyContribution = 0;
        sky.Environment.AmbientLightEnergy = Mathf.Max(0, Settings.AmbientLightEnergy);
        sky.GetNode("SkyDome").Set("moon_light_energy", Mathf.Max(0, Settings.MoonlightEnergy));
        SkyController = new SkyController { Name = "Environment", Sky = sky, Settings = Settings };
        AddChild(SkyController); SkyController.SetProcessUnhandledInput(false);
        Weather = GetNode<WeatherBridge>("Weather"); Haze = GetNode<GroundHaze>("GroundHaze");
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Terrain/player placement is complete before either view binds its explicit camera.
    public void InitializeView(Node3D terrain, PlayerController player, Camera camera)
    {
        if (_viewInitialized) return;
        _viewInitialized = true; _camera = camera;
        Weather.InitializeView(camera, player, Settings);
        var environment = (Godot.Environment)SkyController.Sky.GetNode("SkyDome").Get("environment").AsGodotObject();
        Haze.Initialize(terrain, player, camera, Settings, environment);
        camera.SettingsChanged += Haze.RefreshView;
    }

    // =========================================================
    public override void _ExitTree()
    { if (IsInstanceValid(_camera)) _camera!.SettingsChanged -= Haze.RefreshView; }
}
