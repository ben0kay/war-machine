// Optional body samples for eyesight; independent of actor inheritance, cameras and stance booleans.
using Godot;

public interface IPerceptionTarget
{
    Vector3 PerceptionHead { get; }
    Vector3 PerceptionTorso { get; }
}
