// Authoring-only shared audio budgets/mix. The pooled runtime service will be implemented in a later phase.
using Godot;

[GlobalClass]
public partial class AudioSettings : Resource
{
    [ExportGroup("Master")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "-60,12,0.5")] public float MasterVolumeDb { get; set; } = 0f;

    [ExportGroup("Voice budgets")]
    // Targets only until AudioManager exists. Nested caps never increase the global hard limit.
    [Export(PropertyHint.Range, "1,64,1")] public int MaxVoices { get; set; } = 20;
    [Export(PropertyHint.Range, "0,64,1")] public int MaxOneShotVoices { get; set; } = 14;
    [Export(PropertyHint.Range, "0,64,1")] public int MaxLoopVoices { get; set; } = 6;
    [Export(PropertyHint.Range, "0,32,1")] public int MaxCreatureVoices { get; set; } = 5;
    [Export(PropertyHint.Range, "1,16,1")] public int MaxPerEmitterVoices { get; set; } = 2;
    [Export(PropertyHint.Range, "1,16,1")] public int MaxSameCueVoices { get; set; } = 3;

    [ExportGroup("3D distance")]
    [Export(PropertyHint.Range, "1,250,1")] public float DefaultMaxDistance { get; set; } = 60f;

    [ExportGroup("Category mix")]
    [Export(PropertyHint.Range, "-60,12,0.5")] public float WeaponsVolumeDb { get; set; } = 0f;
    [Export(PropertyHint.Range, "-60,12,0.5")] public float CreaturesVolumeDb { get; set; } = -3f;
    [Export(PropertyHint.Range, "-60,12,0.5")] public float FootstepsVolumeDb { get; set; } = -3f;
    [Export(PropertyHint.Range, "-60,12,0.5")] public float WorldVolumeDb { get; set; } = -2f;
    [Export(PropertyHint.Range, "-60,12,0.5")] public float WeatherVolumeDb { get; set; } = -5f;
    [Export(PropertyHint.Range, "-60,12,0.5")] public float UiVolumeDb { get; set; } = 0f;
}
