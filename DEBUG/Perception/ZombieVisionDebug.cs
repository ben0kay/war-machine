// Optional world-local eyesight display: one F7 handler; static meshes follow visual headings without callbacks.
using Godot;
using System.Collections.Generic;

public partial class ZombieVisionDebug : Node
{
    private readonly List<ZombieController> _zombies = new();
    private bool _visible = true;
    public bool ConesVisible => _visible;
    public event System.Action<bool>? VisibilityChanged;
    private static readonly StandardMaterial3D ConeMaterial = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        AlbedoColor = new Color(1, 0.08f, 0.06f, 0.09f),
        CullMode = BaseMaterial3D.CullModeEnum.Disabled, NoDepthTest = true
    };

    // =========================================================
    // Configuration is copied by EntityCore before registration. This fan is potential FOV, not clipped LOS.
    public void Register(ZombieController zombie)
    {
        var settings = (ZombieSettings)zombie.RuntimeSettings;
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        float half = Mathf.DegToRad(Mathf.Clamp(settings.VisionAngleDegrees, 1, 180)) * 0.5f;
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float a = -half + 2 * half * i / segments, b = -half + 2 * half * (i + 1) / segments;
            mesh.SurfaceAddVertex(Vector3.Zero);
            mesh.SurfaceAddVertex(new Vector3(Mathf.Sin(a), 0, -Mathf.Cos(a)));
            mesh.SurfaceAddVertex(new Vector3(Mathf.Sin(b), 0, -Mathf.Cos(b)));
        }
        mesh.SurfaceEnd();
        var cone = new MeshInstance3D { Name = "DebugVisionCone", Mesh = mesh, MaterialOverride = ConeMaterial,
            Position = Vector3.Up * settings.EyeHeight, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        zombie.VisualRoot.AddChild(cone); zombie.AttachVisionDebug(cone); _zombies.Add(zombie);
        zombie.SetVisionDebugVisible(_visible && settings.ShowVisionCone);
    }

    // =========================================================
    // Single source of truth for F7 and the overhead checkbox; affects debug meshes, never AI eyesight.
    public void SetConesVisible(bool visible)
    {
        if (_visible == visible) return;
        _visible = visible;
        for (int i = _zombies.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_zombies[i])) { _zombies.RemoveAt(i); continue; }
            var settings = (ZombieSettings)_zombies[i].RuntimeSettings;
            _zombies[i].SetVisionDebugVisible(visible && settings.ShowVisionCone);
        }
        VisibilityChanged?.Invoke(_visible);
    }

    // =========================================================
    // F7 remains available in both world perspectives with no frame callbacks or additional LOS queries.
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo || key.PhysicalKeycode != Key.F7) return;
        SetConesVisible(!_visible);
        GetViewport().SetInputAsHandled();
    }
}
