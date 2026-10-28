// Per-player storage lifetime. Starter supplies are real stored stacks, without gameplay use/equip hooks yet.
using Godot;

public partial class PlayerBackpack : Node
{
    public BackpackStorage Storage { get; private set; } = null!;
    public InventorySettings Settings { get; private set; } = null!;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(InventorySettings settings)
    {
        if (Storage != null) return;
        Settings = (InventorySettings)settings.Duplicate(true);
        Storage = new BackpackStorage(Settings.Capacity);
        foreach (Resource entry in Settings.StartingItems)
            if (entry is InventoryStartingItem starter && starter.Item != null)
                if (!Storage.TryAdd(starter.Item, starter.Quantity)) GD.PushWarning("Backpack starter stack did not fit; check InventoryMaster.");
    }
}
