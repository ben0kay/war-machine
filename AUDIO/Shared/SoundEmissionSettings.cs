// Shared action loudness and gameplay reach. Playback still uses an independent AudioCue and its voice budgets.
using Godot;

[GlobalClass]
public partial class SoundEmissionSettings : Resource
{
    [Export(PropertyHint.Range, "0,1000,0.5,suffix:m")] public float HearingRadiusMeters { get; set; }
    // Linear amplitude: 1 is authored default, 0 silences the action, 0.5 is quieter.
    [Export(PropertyHint.Range, "0,4,0.05")] public float Loudness { get; set; } = 1f;
}
