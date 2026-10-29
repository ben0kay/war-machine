// View differences only; shared targeting owns reach, solid cover and collider-to-owner resolution.
using Godot;

public interface IInteractionView
{
    bool CanFocus { get; }
    bool UseFadedCameraSelection { get; }
    Vector2 AimPosition(Viewport viewport);
}

public sealed class TopDownInteraction : IInteractionView
{
    public bool CanFocus => true;
    public bool UseFadedCameraSelection => true;
    // =========================================================
    public Vector2 AimPosition(Viewport viewport) => viewport.GetMousePosition();
}

public sealed class FirstPersonInteraction : IInteractionView
{
    public bool CanFocus => Input.MouseMode == Input.MouseModeEnum.Captured;
    public bool UseFadedCameraSelection => false;
    // =========================================================
    public Vector2 AimPosition(Viewport viewport) => viewport.GetVisibleRect().Size * 0.5f;
}
