// SOC0: optional per-asset primitive box, in UNscaled Model-local metres.
// Read-only source resource; runtime collision shapes are unique to the instance.
using Godot;

[Tool]
[GlobalClass]
public partial class StaticCollisionBox : Resource
{
    [Export] public Vector3 Center { get; set; } = Vector3.Zero;
    [Export] public Vector3 Size { get; set; } = Vector3.One;
}
