// Test-only WASD adapter. Shared player still owns gravity, capsule collision, acceleration and crouching.
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
    protected override bool IsMovementKey(Key key) => key == Key.W || key == Key.A || key == Key.S || key == Key.D || key == Key.Shift;
    protected override Vector2 ReadMovementInput() => Input.MouseMode != Input.MouseModeEnum.Captured ? Vector2.Zero : new(
        (Input.IsPhysicalKeyPressed(Key.D) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) ? 1 : 0),
        (Input.IsPhysicalKeyPressed(Key.W) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.S) ? 1 : 0));

    // =========================================================
    // Read current yaw only while moving; looking up/down cannot change walking speed or introduce flight.
    protected override Vector3 GetMovementDirection(Vector2 keys)
    {
        Vector3 forward = -_view.GlobalBasis.Z; forward.Y = 0;
        Vector3 right = _view.GlobalBasis.X; right.Y = 0;
        return (right.Normalized() * keys.X + forward.Normalized() * keys.Y).Normalized();
    }
}
