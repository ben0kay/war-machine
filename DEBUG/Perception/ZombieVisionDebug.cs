// Optional world-local eyesight display: one F8 handler; static meshes follow visual headings without callbacks.
using Godot;
using System.Collections.Generic;

public partial class ZombieVisionDebug : Node
{
    private readonly List<ZombieController> _zombies = new();
    private bool _visible = true;
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
    }

    // =========================================================
    // One input-only observer for the small test population; no process, physics, polling or extra LOS queries.
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo || key.PhysicalKeycode != Key.F8) return;
        _visible = !_visible;
        for (int i = _zombies.Count - 1; i >= 0; i--)
        {
            if (!IsInstanceValid(_zombies[i])) { _zombies.RemoveAt(i); continue; }
            _zombies[i].SetVisionDebugVisible(_visible);
        }
        GetViewport().SetInputAsHandled();
    }
}
