// MAT1: reuse authored shader material presets for Robot_001's generated primitive artwork.
// Duplicate only once per finish/robot, never per part; leave the existing emissive optics untouched.
using Godot;

public static class RobotMaterialFactory
{
    private const string Root = "res://ENTITIES/TYPES/Robots/Robot_001/Artwork/Materials/";

    // =========================================================
    // Worn military painted armour with local-space chips, dirt and fine scratches.
    public static Material Armour(RobotSettings s)
    {
        var m = Make(s, "WeatheredArmour", s.ArmourColor);
        if (m is ShaderMaterial shader)
        {
            shader.SetShaderParameter("base_color", s.ArmourColor);
            shader.SetShaderParameter("metallic_level", Mathf.Clamp(s.ArmourMetallic, 0f, 1f));
            shader.SetShaderParameter("base_roughness", Mathf.Clamp(s.ArmourRoughness, 0f, 1f));
            shader.SetShaderParameter("wear_amount", Mathf.Clamp(s.ArmourWear, 0f, 1f));
            shader.SetShaderParameter("dirt_amount", Mathf.Clamp(s.ArmourDirt, 0f, 1f));
            shader.SetShaderParameter("scratch_amount", Mathf.Clamp(s.ArmourScratches, 0f, 1f));
        }
        return m;
    }

    // =========================================================
    // Industrial joint metal; colour distinguishes housings from stronger exposed steel.
    public static Material Joint(RobotSettings s, Color color)
    {
        var m = Make(s, "JointMetal", color);
        if (m is ShaderMaterial shader)
        {
            shader.SetShaderParameter("base_color", color);
            shader.SetShaderParameter("grime_amount", Mathf.Clamp(s.ArmourDirt * 0.75f, 0f, 1f));
        }
        return m;
    }

    // =========================================================
    // Brighter, slightly smoother brushed piston/bolt metal.
    public static Material Steel(RobotSettings s, Color color)
    {
        var m = Make(s, "BrushedSteel", color);
        if (m is ShaderMaterial shader)
        {
            shader.SetShaderParameter("base_color", color);
            shader.SetShaderParameter("grime_amount", Mathf.Clamp(s.ArmourDirt * 0.45f, 0f, 1f));
        }
        return m;
    }

    // =========================================================
    // Matt rubberized cable sheathing, gasket material and deep recesses.
    public static Material Rubber(RobotSettings s, Color color)
    {
        var m = Make(s, "IndustrialRubber", color);
        if (m is ShaderMaterial shader) shader.SetShaderParameter("base_color", color);
        return m;
    }

    // =========================================================
    // Scene-level material resource cache is provided by Godot; only the duplicated instance
    // receives robot-specific parameters. No changes to a shared .tres or global shaders.
    private static Material Make(RobotSettings s, string preset, Color fallbackColor)
    {
        if (s.ProceduralMaterialsEnabled)
        {
            var source = GD.Load<ShaderMaterial>(Root + preset + ".tres");
            if (source != null) return (ShaderMaterial)source.Duplicate();
            GD.PushWarning($"Robot_001 missing MAT1 material: {preset}; using flat fallback.");
        }
        return new StandardMaterial3D { AlbedoColor = fallbackColor, Metallic = 0.65f, Roughness = 0.58f };
    }
}
