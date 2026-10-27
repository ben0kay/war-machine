// Test-only arrow-key adapter. Shared player still owns gravity, capsule collision, acceleration and crouching.
using Godot;

public partial class FirstPersonPlayer : PlayerController
{
    private Camera3D _view = null!;

    // =========================================================
    // Keep the existing actor identity for zombies, but hide its third-person art from the eye camera.
    public override void Initialize(PlayerSettings settings, Camera3D camera)
    { _view = camera; base.Initialize(settings, camera); VisualRoot.Hide(); }

    // =========================================================
    // Releases and camera capture changes wake the shared movement driver so velocity can settle.
    protected override Vector2 ReadMovementInput() => Input.MouseMode == Input.MouseModeEnum.Captured
        ? base.ReadMovementInput() : Vector2.Zero;

    // =========================================================
    // Read current yaw only while moving; looking up/down cannot change walking speed or introduce flight.
    protected override Vector3 GetMovementDirection(Vector2 keys)
    {
        Vector3 forward = -_view.GlobalBasis.Z; forward.Y = 0;
        Vector3 right = _view.GlobalBasis.X; right.Y = 0;
        return (right.Normalized() * keys.X + forward.Normalized() * keys.Y).Normalized();
    }
}
