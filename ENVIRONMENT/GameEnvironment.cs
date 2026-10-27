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
        SkyController.AtmosphereSettingsApplied += ApplyRays; ApplyRays();
    }

    // =========================================================
    // Configure the existing pass and directional lights without extra lights, volumes or callbacks.
    private void ApplyRays()
    {
        bool enabled = Settings.LightRaysEnabled && Settings.LightRaysDensity > 0 &&
            ProjectSettings.GetSetting("rendering/renderer/rendering_method", "forward_plus").AsString() == "forward_plus";
        var sky = (WorldEnvironment)SkyController.Sky;
        sky.GetNode<DirectionalLight3D>("SunLight").LightVolumetricFogEnergy = enabled ? Mathf.Max(0, Settings.SunRayEnergy) : 0;
        sky.GetNode<DirectionalLight3D>("MoonLight").LightVolumetricFogEnergy = enabled ? Mathf.Max(0, Settings.MoonRayEnergy) : 0;
        sky.Environment.VolumetricFogAlbedo = Colors.White;
        sky.Environment.VolumetricFogEmission = Colors.Black;
        sky.Environment.VolumetricFogAmbientInject = 0.15f;
        sky.Environment.VolumetricFogAnisotropy = Mathf.Clamp(Settings.RayAnisotropy, -0.9f, 0.9f);
        Haze.SetFogBaseline(enabled, enabled ? Mathf.Clamp(Settings.LightRaysDensity, 0, 0.01f) : 0,
            Mathf.Clamp(Settings.LightRaysLength, 20, 160));
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (IsInstanceValid(SkyController)) SkyController.AtmosphereSettingsApplied -= ApplyRays;
        if (IsInstanceValid(_camera)) _camera!.SettingsChanged -= Haze.RefreshView;
    }
}
