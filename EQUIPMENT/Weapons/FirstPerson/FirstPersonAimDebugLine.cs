// FPS-only, opt-in visual debug line: muzzle to exact pre-spread camera aim target.
using Godot;

public partial class FirstPersonAimDebugLine : Node3D
{
    private PlayerController _player = null!;
    private FirstPersonCamera _camera = null!;
    private FirearmController? _weapon;
    private PhysicsRayQueryParameters3D _query = null!;
    private MeshInstance3D _line = null!;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Allocate one reusable cylinder and material. No shot pipeline or world content changes.
    public void Initialize(PlayerController player, FirstPersonCamera camera)
    {
        _player = player; _camera = camera;
        _query = new PhysicsRayQueryParameters3D { HitFromInside = true,
            Exclude = new Godot.Collections.Array<Rid> { player.GetRid() } };

        var material = new StandardMaterial3D {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.1f, 1f, 0.8f, 1f), NoDepthTest = true
        };
        _line = new MeshInstance3D {
            Name = "TrueAimRay", Mesh = new CylinderMesh {
                TopRadius = 0.012f, BottomRadius = 0.012f, Height = 1f, RadialSegments = 6
            },
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false
        };
        AddChild(_line);
    }

    // =========================================================
    // Weapon selection wakes the helper; switching to melee stops its only render callback.
    public void SetWeapon(FirearmController? weapon)
    {
        _weapon = IsInstanceValid(weapon) ? weapon : null;
        bool active = _weapon != null && IsInstanceValid(_line);
        if (IsInstanceValid(_line)) _line.Visible = false;
        SetProcess(active);
    }

    // =========================================================
    // Development visualization intentionally costs one camera-ray query per visible frame.
    // The actual shot uses the same target resolver but adds its normal spread afterward.
    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_weapon) || !IsInstanceValid(_camera) || !IsInstanceValid(_player))
        { _line.Visible = false; SetProcess(false); return; }
        if (_player.GameplayInputBlocked || _player.InteractionModeActive || _player.IsDead
            || Input.MouseMode != Input.MouseModeEnum.Captured || !GetWindow().HasFocus()
            || GetViewport().GuiGetFocusOwner() != null)
        { _line.Visible = false; return; }

        Vector2 centre = _camera.GetViewport().GetVisibleRect().Size * 0.5f;
        Vector3 origin = _camera.ProjectRayOrigin(centre), forward = _camera.ProjectRayNormal(centre);
        _query.CollisionMask = _weapon!.RuntimeSettings.HitMask;
        Vector3 target = FirstPersonAimTarget.Resolve(_player, _query, origin, forward,
            _weapon.RuntimeSettings.Range);
        Vector3 start = _weapon.Muzzle.GlobalPosition;
        Vector3 path = target - start;
        float distance = path.Length();
        if (!float.IsFinite(distance) || distance < 0.001f)
        { _line.Visible = false; return; }

        // Cylinder primitive lies along local +Y. Build an orthogonal world-space basis
        // with Y scaled to the full ray length, leaving its visible thickness constant.
        Vector3 y = path / distance;
        Vector3 reference = Mathf.Abs(y.Y) > 0.98f ? Vector3.Right : Vector3.Up;
        Vector3 x = reference.Cross(y).Normalized();
        Vector3 z = x.Cross(y).Normalized();
        _line.GlobalTransform = new Transform3D(new Basis(x, y * distance, z), start + path * 0.5f);
        _line.Visible = true;
    }
}
