// One editable, reproducible cave preview, positioned relative to TestWorld's spawn.
using Godot;
[GlobalClass]
public partial class CaveSettings : Resource
{
    [Export] public Vector2 SpawnOffset { get; set; } = new(48, -28);
    [Export] public float Length { get; set; } = 90;
    [Export] public float Width { get; set; } = 3.5f;
    [Export] public float Height { get; set; } = 4.8f;
    [Export] public float Bend { get; set; } = 11;
    [Export] public float HeightVariation { get; set; } = 3;
    [Export] public int Sections { get; set; } = 48;
    [Export] public float LightEnergy { get; set; } = 2;
    [Export] public Color LightColor { get; set; } = new(0.35f, 0.65f, 1);
}
