// Globally authored automatic streetlamp hours; Sky3D is the only in-game time owner.
using Godot;

[GlobalClass]
public partial class StreetLampScheduleSettings : Resource
{
    [ExportGroup("All streetlights / Sky3D hours")]
    [Export] public bool AutomaticTimeSwitching { get; set; } = true;
    [Export(PropertyHint.Range, "0,23.99,0.05,suffix:h")] public float TurnOnHour { get; set; } = 17.25f;
    [Export(PropertyHint.Range, "0,23.99,0.05,suffix:h")] public float TurnOffHour { get; set; } = 6.5f;
}
