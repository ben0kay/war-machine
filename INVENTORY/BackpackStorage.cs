// Per-player backpack authority: bounded stacks, stable stack IDs and atomic add/remove/move operations.
// No node, timer, input, UI, equipped weapon copies, physics or idle processing.
using System;

public sealed class BackpackStorage
{
    public sealed class Stack
    {
        public string InstanceId { get; } = Guid.NewGuid().ToString("N");
        public ItemDefinition Item { get; }
        public int Quantity { get; internal set; }
        internal Stack(ItemDefinition item, int quantity) { Item = item; Quantity = quantity; }
    }
    private readonly Stack?[] _slots;
    public event Action? Changed;
    public int Capacity => _slots.Length;
    public Stack? this[int slot] => Valid(slot) ? _slots[slot] : null;

    // =========================================================
    public BackpackStorage(int capacity) { _slots = new Stack?[Math.Clamp(capacity, 6, 60)]; }

    // =========================================================
    // All-or-nothing add: check available capacity before changing any stack. Definitions match by reference.
    public bool TryAdd(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return false;
        int maximum = Math.Clamp(item.MaxStack, 1, 999), space = 0;
        foreach (var stack in _slots)
            space += stack == null ? maximum : stack.Item == item ? Math.Max(0, maximum - stack.Quantity) : 0;
        if (space < quantity) return false;
        int remaining = quantity;
        foreach (var stack in _slots)
        {
            if (stack == null || stack.Item != item) continue;
            int move = Math.Min(remaining, Math.Max(0, maximum - stack.Quantity));
            stack.Quantity += move; remaining -= move;
            if (remaining == 0) break;
        }
        for (int i = 0; remaining > 0 && i < Capacity; i++)
        {
            if (_slots[i] != null) continue;
            int move = Math.Min(remaining, maximum); _slots[i] = new Stack(item, move); remaining -= move;
        }
        Changed?.Invoke(); return true;
    }

    // =========================================================
    // Future consumers call this after their own action checks; current UI does not consume/use items.
    public bool TryRemove(int slot, int quantity)
    {
        var stack = this[slot];
        if (stack == null || quantity <= 0 || stack.Quantity < quantity) return false;
        stack.Quantity -= quantity; if (stack.Quantity == 0) _slots[slot] = null;
        Changed?.Invoke(); return true;
    }

    // =========================================================
    // Empty destination moves identity; matching stacks merge up to cap; different items swap atomically.
    public bool MoveOrMerge(int from, int to)
    {
        if (!Valid(from) || !Valid(to) || from == to || _slots[from] == null) return false;
        var source = _slots[from]!; var destination = _slots[to];
        if (destination != null && destination.Item == source.Item)
        {
            int move = Math.Min(source.Quantity, Math.Max(0, Math.Clamp(source.Item.MaxStack, 1, 999) - destination.Quantity));
            if (move == 0) return false;
            destination.Quantity += move; source.Quantity -= move;
            if (source.Quantity == 0) _slots[from] = null;
        }
        else { _slots[to] = source; _slots[from] = destination; }
        Changed?.Invoke(); return true;
    }

    // =========================================================
    private bool Valid(int slot) => slot >= 0 && slot < Capacity;
}
