// Optional species-provided articulation blueprint. Shared physics never searches for named anatomical joints.
using Godot;

public interface IArticulatedCorpsePose : ICorpsePoseAdapter
{
    CorpseRagdollPart[] CreateRagdollParts(Node3D visual);
}

public sealed class CorpseRagdollPart
{
    public Node3D Visual = null!;
    public int Parent = -1;
    public Vector3 Center, Size;
    public float MassWeight = 1;
    public Transform3D Anchor;
    public bool Hinge;
    public float LowerDegrees, UpperDegrees, SwingDegrees = 45, TwistDegrees = 20;
}
