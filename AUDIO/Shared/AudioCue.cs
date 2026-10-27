// Reusable authoring data; streams remain unassigned until an actual sound asset is supplied.
using Godot;

public enum SoundCategory { Weapons, Creatures, Footsteps, World, Weather, UI }

[GlobalClass]
public partial class AudioCue : Resource
{
    [Export] public string CueId { get; set; } = "";
    [Export] public AudioStream? Stream { get; set; }
    [Export] public SoundCategory Category { get; set; } = SoundCategory.World;
    [Export] public bool Spatial { get; set; } = true;
    [Export(PropertyHint.Range, "0,100,1")] public int Priority { get; set; } = 50;
    [Export] public float VolumeDb { get; set; }
    [Export] public float MaxDistance { get; set; } = 60;
    [Export(PropertyHint.Range, "0,0.2,0.005")] public float PitchVariation { get; set; } = 0.025f;
}
