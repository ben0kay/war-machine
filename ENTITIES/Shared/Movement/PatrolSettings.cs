// Generic waypoint locomotion tuning, independent of robot art and sensors.
using Godot;

[GlobalClass]
public partial class PatrolSettings : Resource
{
    [ExportGroup("Motion")]
    [Export] public float Speed { get; set; } = 2;
    [Export] public float Acceleration { get; set; } = 2;
    [Export] public float Braking { get; set; } = 3;
    [Export] public float TurnDegreesPerSecond { get; set; } = 45;
    [Export] public float ArrivalRadius { get; set; } = 0.6f;
    [Export] public float Gravity { get; set; } = 20;
    [ExportGroup("Stops")]
    [Export] public float SettleSeconds { get; set; } = 0.6f;
    [Export] public float WaitSeconds { get; set; } = 1.5f;
    [Export] public bool Loop { get; set; } = true;
}
