// Input requests and bindings only; no scene nodes, processing loop or alternate input owner.
using Godot;

public partial class PlayerController
{
    private sealed class PlayerInput
    {
        private readonly PlayerController _player;
        public bool _toggleCrouch, _toggleProne, _jumpRequested;
        // =========================================================
        // Plain collaborator: only the controller owns engine callbacks.
        public PlayerInput(PlayerController player) => _player = player;

        // =========================================================
        // Preserve GUI-first input, current physical keys and FPS jump gating.
        public void Handle(InputEvent input)
        {
            if (!_player._initialized || _player.GameplayInputBlocked) return;
            if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
            {
                if (_player.CanAcceptJumpInput() && _player.GetWindow().HasFocus() && _player.IsOnFloor() && _player._stance.Current == MovementStance.Standing
                    && _player.GetViewport().GuiGetHoveredControl() == null && _player.GetViewport().GuiGetFocusOwner() == null)
                {
                    _jumpRequested = true; _player.SetPhysicsProcess(true);
                    _player.GetViewport().SetInputAsHandled();
                }
                return;
            }
            if (input is not InputEventKey key || key.Echo) return;
            if (key.PhysicalKeycode == Key.F10 && key.Pressed)
            {
                _player._movement._reportMovementCollision = true; _player.SetPhysicsProcess(true);
                _player.GetViewport().SetInputAsHandled(); return;
            }
            if (key.PhysicalKeycode == Key.Slash && key.Pressed)
            {
                _toggleProne = true; _toggleCrouch = false;
                _player.SetPhysicsProcess(true); _player.GetViewport().SetInputAsHandled(); return;
            }
            if ((key.PhysicalKeycode == Key.Ctrl && key.Location == KeyLocation.Right) && key.Pressed)
            {
                _toggleCrouch = true; _toggleProne = false;
                _player.SetPhysicsProcess(true); _player.GetViewport().SetInputAsHandled(); return;
            }
            if (_player.IsMovementKey(key.PhysicalKeycode))
                _player.SetPhysicsProcess(true);
        }

        // =========================================================
        // Focus changes clear a pending jump and wake the existing physics driver.
        public void Notify(int what)
        {
            if (!_player._initialized) return;
            if (what == Node.NotificationApplicationFocusOut) _jumpRequested = false;
            if (what == Node.NotificationApplicationFocusOut || what == Node.NotificationApplicationFocusIn)
                _player.SetPhysicsProcess(true);
        }

        // =========================================================
        public void ClearRequests() => _toggleCrouch = _toggleProne = _jumpRequested = false;

        // =========================================================
        public static bool MovementKey(Key key) => key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right || key == Key.Shift;

        // =========================================================
        public static Vector2 MovementInput() => new(
            (Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0),
            (Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0));
    }
}
