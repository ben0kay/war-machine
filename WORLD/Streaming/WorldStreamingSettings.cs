// WS2 bounded static proof settings. No active-world opt-in is implied by this resource.
using Godot;

[GlobalClass]
public partial class WorldStreamingSettings : Resource
{
    [Export] public float LoadDistance { get; set; } = 180;
    [Export] public float UnloadDistance { get; set; } = 240;
    [Export] public float MovementStep { get; set; } = 8;
    [Export] public int MaxPlacements { get; set; } = 128;
    [Export] public int MaxResident { get; set; } = 16;
    [Export] public int MaxInFlight { get; set; } = 2;
    [Export] public int OperationsPerFrame { get; set; } = 1;

    // =========================================================
    public void Validate()
    {
        if (!float.IsFinite(LoadDistance) || !float.IsFinite(UnloadDistance) || !float.IsFinite(MovementStep)
            || MovementStep < 1 || LoadDistance < 1 || UnloadDistance <= LoadDistance + MovementStep * 2
            || MaxPlacements < 1 || MaxPlacements > 1024 || MaxResident < 1 || MaxResident > MaxPlacements
            || MaxInFlight < 1 || MaxInFlight > 8 || OperationsPerFrame < 1 || OperationsPerFrame > 8)
            throw new System.ArgumentException("Invalid WS2 distances or work limits.");
    }
}
