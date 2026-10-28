// Two explicit test slots shared by both player input adapters. No inventory, per-frame polling or firing logic.
using Godot;

public partial class WeaponSlots : Node
{
    private readonly FirearmController?[] _weapons = new FirearmController?[2];
    private EquipmentConfig _config = null!;
    private CollisionObject3D _holder = null!;
    private Node3D _mount = null!;
    private Vector3 _offset;
    private WorldAudio? _audio;
    private int _selected = -1;
    public FirearmController? Current => _selected < 0 ? null : _weapons[_selected];
    // AM1: the selected firearm changes only through Select(), so HUDs subscribe once.
    public event System.Action<FirearmController?>? SelectionChanged;

    // =========================================================
    // Actual scenes remain data-driven; FPS supplies its eye-camera mount, overhead supplies the hand socket.
    public void Initialize(EquipmentConfig config, CollisionObject3D holder, Node3D mount, Vector3 offset, WorldAudio? audio = null)
    { _config = config; _holder = holder; _mount = mount; _offset = offset; _audio = audio; SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Lazy scene creation is bounded to two weapons. Validate replacement before putting the current gun away.
    public bool Select(int slot)
    {
        if (!_config.Enabled || slot < 0 || slot >= _weapons.Length || slot == _selected) return false;
        var scene = slot == 0 ? _config.DefaultWeaponScene : _config.SecondaryWeaponScene;
        if (scene == null) { GD.PushWarning($"Weapon slot {slot + 1} has no configured scene."); return false; }
        if (!IsInstanceValid(_weapons[slot]))
        {
            Node instance = scene.Instantiate();
            if (instance is not FirearmController firearm)
            { GD.PushError("Weapon slots require FirearmController scene roots."); instance.Free(); return false; }
            _weapons[slot] = firearm; _mount.AddChild(firearm); firearm.Position = _offset;
        }
        if (IsInstanceValid(Current)) { Current!.Unequip(); Current.Hide(); Current.ProcessMode = ProcessModeEnum.Disabled; }
        _selected = slot;
        Current!.ProcessMode = ProcessModeEnum.Inherit; Current.Show(); Current.Equip(_holder, _audio);
        SelectionChanged?.Invoke(Current);
        return true;
    }

    // =========================================================
    // UI retains first refusal. Ignore key repeats; both adapters use exactly the same slot numbering.
    public static int RequestedSlot(InputEvent input)
    {
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return -1;
        return key.PhysicalKeycode == Key.Key1 ? 0 : key.PhysicalKeycode == Key.Key2 ? 1 : -1;
    }

    // =========================================================
    // Guns may be parented outside this helper; explicitly release them even when the adapter alone is removed.
    public override void _ExitTree()
    {
        foreach (var weapon in _weapons)
            if (IsInstanceValid(weapon)) { weapon!.Unequip(); weapon.QueueFree(); }
    }
}
