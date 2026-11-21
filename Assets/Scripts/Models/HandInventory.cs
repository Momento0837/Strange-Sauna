namespace SuspiciousSauna
{
    // Slot identity stays stable: removing the right item never moves the left item.
    // Inventory accepts any item category; food rules live in the service controller.
    public sealed class HandInventory
    {
        readonly SaunaItemDefinition[] slots;
        public HandInventory(int capacity = 2) { slots = new SaunaItemDefinition[System.Math.Max(2, capacity)]; }
        public int Capacity => slots.Length;
        public SaunaItemDefinition this[int index] => slots[index];
        public SaunaItemDefinition Right => slots[0];
        public SaunaItemDefinition Left => slots[1];
        public int Count { get { int count = 0; foreach (var item in slots) if (item != null) count++; return count; } }
        public bool Full => Count == Capacity;
        public bool TryAdd(SaunaItemDefinition item)
        {
            if (item == null) return false;
            for (int i = 0; i < slots.Length; i++) if (slots[i] == null) { slots[i] = item; return true; }
            return false;
        }
        public void Remove(int slot) => slots[slot] = null;
        public void Clear() => System.Array.Clear(slots, 0, slots.Length);
    }
}
