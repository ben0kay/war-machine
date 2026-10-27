// Scene composition defaults only; individual weapons own their editable stats resources.
using Godot;

[GlobalClass]
public partial class EquipmentConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public PackedScene SecondaryWeaponScene { get; set; } = null!;
    [Export] public PackedScene DefaultWeaponScene { get; set; } = null!;
}
