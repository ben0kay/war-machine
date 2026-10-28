// Zombie 002: fast, lean runner. Inherits the existing zombie AI, perception and damage pipeline.
using Godot;

public partial class ZombieRunnerController : ZombieController
{
    // =========================================================
    // Replaces artwork only; keep original six joint pivots and the same collision capsule.
    protected override void BuildArt()
    {
        var mesh = new ZombieRunnerMeshBuilder(VisualRoot);
        BindArtJoints(mesh.LeftArm, mesh.RightArm, mesh.LeftLeg, mesh.RightLeg, mesh.LeftKnee, mesh.RightKnee);
    }

    // =========================================================
    // The runner accelerates decisively once it spots the player; normal zombies retain their old acceleration.
    protected override float MovementAcceleration => IsPursuing ? 20f : 5f;

    // =========================================================
    // Wider, more forceful running steps without cycling absurdly fast at 8+ metres/second.
    protected override float GaitPhasePerMeter => IsPursuing ? 2.35f : 5f;

    // =========================================================
    // Unalerted: restless shamble. Chasing: forward lean, opposite arm drive, longer stride and knee lift.
    // No attack/extra timers; this uses the base class's existing physics gait callback.
    protected override void Pose(float step, float leftLift = 0, float rightLift = 0)
    {
        bool sprint = IsPursuing;
        VisualRoot.Position = Vector3.Up * (Mathf.Abs(step) * (sprint ? 0.032f : 0.014f));
        // Forward lean follows the current visual yaw without changing the physics body or eye-facing direction.
        VisualRoot.Rotation = new Vector3(sprint ? -0.14f : -0.045f, VisualRoot.Rotation.Y, 0);
        _leftArm.Rotation = new Vector3((sprint ? 0.25f : 0.12f) - step * (sprint ? 0.75f : 0.10f), 0, -0.10f);
        _rightArm.Rotation = new Vector3((sprint ? 0.25f : 0.12f) + step * (sprint ? 0.75f : 0.10f), 0, 0.10f);
        _leftLeg.Rotation = new Vector3(step * (sprint ? 0.57f : 0.24f), 0, 0);
        _rightLeg.Rotation = new Vector3(-step * (sprint ? 0.57f : 0.24f), 0, 0);
        _leftKnee.Rotation = new Vector3(-leftLift * (sprint ? 0.83f : 0.30f), 0, 0);
        _rightKnee.Rotation = new Vector3(-rightLift * (sprint ? 0.83f : 0.27f), 0, 0);
    }
}
