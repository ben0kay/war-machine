// GD1: scene-wide colour grading controls, neutral by default; authoring resource stays untouched by F1.
using Godot;

[GlobalClass]
public partial class WorldColorGradeSettings : Resource
{
    [ExportGroup("Global colour grading")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "-180,180,1")] public float HueDegrees { get; set; } = 0f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float Saturation { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.5,1.5,0.01")] public float Contrast { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.5,1.5,0.01")] public float Brightness { get; set; } = 1f;
}
