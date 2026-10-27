// Minimal hit contract for weapons, actors and future destructible scenery; no rifle dependency.
using Godot;

public interface IDamageReceiver
{
    void ReceiveHit(float damage, Vector3 position, Vector3 direction, Node3D source);
}
