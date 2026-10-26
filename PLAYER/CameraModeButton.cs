// Lightweight on-screen mode switch; labels update only when the explorer changes mode.
using Godot;
public partial class CameraModeButton : CanvasLayer
{
    private ExplorationController _explorer = null!;
    private Button _button = null!;
    // =========================================================
    public override void _Ready()
    {
        _explorer = GetParent().GetNode<ExplorationController>("Explorer");
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore }; AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _button = new Button { Text = "F6 · Free camera → First person", OffsetLeft = -310, OffsetTop = 16, OffsetRight = -16, OffsetBottom = 50, AnchorLeft = 1, AnchorRight = 1 };
        root.AddChild(_button); _button.Pressed += _explorer.ToggleCameraMode;
        _explorer.CameraModeChanged += UpdateLabel;
        SetProcess(false); SetPhysicsProcess(false);
    }
    // =========================================================
    private void UpdateLabel(bool firstPerson) => _button.Text = firstPerson ? "F6 · First person → Free camera" : "F6 · Free camera → First person";
    // =========================================================
    public override void _ExitTree()
    { if (IsInstanceValid(_explorer)) _explorer.CameraModeChanged -= UpdateLabel; }
}
