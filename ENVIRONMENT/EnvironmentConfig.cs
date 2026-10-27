// Central atmosphere/time and shared grass wind settings; Sky3D remains the clock owner.
using Godot;

[GlobalClass]
public partial class EnvironmentConfig : Resource
{
    [ExportGroup("Gameplay lighting")]
    [Export] public float AmbientLightEnergy { get; set; } = 0.8f;
    [Export] public float MoonlightEnergy { get; set; } = 0.8f;
    [ExportGroup("Top-down visibility fog")]
    [Export] public bool DistanceFogEnabled { get; set; }
    [Export(PropertyHint.Range, "5,150,1")] public float DistanceFogStart { get; set; } = 24;
    [Export(PropertyHint.Range, "6,200,1")] public float DistanceFogEnd { get; set; } = 48;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DistanceFogOpacity { get; set; } = 1;
    [Export] public Color DistanceFogColor { get; set; } = new(0.39f, 0.46f, 0.48f);
    [Export(PropertyHint.Range, "0,8,0.1")] public float DistanceFogEdgeVariation { get; set; } = 2;
    [ExportGroup("Sky3D Time")]
    [Export(PropertyHint.Range, "0,23.99,0.01")] public float StartHour { get; set; } = 9f;
    [Export(PropertyHint.Range, "1,1440,1")] public float DayLengthMinutes { get; set; } = 24f;
    [Export] public float PreviewDayMinutes { get; set; } = 1f;
    [Export] public float UpdateInterval { get; set; } = 0.1f;
    [Export] public bool StartPaused { get; set; }
    [ExportGroup("Sky3D Mist")]
    [Export] public bool FogEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,0.1,0.001")] public float FogDensity { get; set; } = 0.025f;
    [Export(PropertyHint.Range, "0,500,1")] public float FogStart { get; set; } = 0f;
    [Export(PropertyHint.Range, "1,2000,1")] public float FogEnd { get; set; } = 200f;
    [Export(PropertyHint.Range, "0,50,0.1")] public float FogFalloff { get; set; } = 3f;
    [ExportGroup("Ground Haze / Forward+")]
    [Export] public bool GroundHazeEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,0.3,0.001")] public float GroundHazeDensity { get; set; } = 0.18f;
    [Export(PropertyHint.Range, "0.25,8,0.05")] public float GroundHazeHeight { get; set; } = 1.1f;
    [Export] public float GroundHazeNoiseScale { get; set; } = 0.35f;
    [Export] public float GroundHazeDriftSpeed { get; set; } = 0.3f;
    [Export] public float GroundHazeContrast { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float GroundHazeTurbulence { get; set; } = 0.15f;
    [Export(PropertyHint.Range, "6,48,1")] public float GroundHazeCoverageRadius { get; set; } = 9;
    [Export] public Color GroundHazeColor { get; set; } = new(0.64f, 0.71f, 0.73f);
    [ExportGroup("Weather Preview")]
    [Export] public bool WeatherEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RainIntensity { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float SnowIntensity { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float LocalMistIntensity { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float AirborneIntensity { get; set; }
    [Export(PropertyHint.Range, "0,1,0.01")] public float WeatherGusts { get; set; } = 0.3f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float WeatherTurbulence { get; set; } = 0.2f;
    [Export(PropertyHint.Range, "0,2,1")] public float WeatherQuality { get; set; } = 1;
    [Export(PropertyHint.Range, "0.1,3,0.05")] public float WeatherParticleSize { get; set; } = 1;
    [Export(PropertyHint.Range, "0.1,3,0.05")] public float WeatherFallSpeed { get; set; } = 1;
    [Export(PropertyHint.Range, "0,360,1")] public float WindHeading { get; set; } = 22;
    [ExportGroup("Shared Wind")]
    [Export] public Vector2 WindDirection { get; set; } = new(1, 0.4f);
    [Export] public float GrassWindStrength { get; set; } = 1f;
    [Export] public float GrassWindSpeed { get; set; } = 0.025f;
}
