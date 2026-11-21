using System.Collections.Generic;
using System.Linq;
namespace SuspiciousSauna
{
    public sealed class FoodOrderLine
    {
        public SaunaItemDefinition item;
        public int quantity, delivered;
        public int Remaining => quantity - delivered;
    }
    public sealed class TableFoodOrder
    {
        public readonly List<FoodOrderLine> lines = new List<FoodOrderLine>();
        public int Quantity => lines.Sum(l => l.quantity);
        public int Remaining => lines.Sum(l => l.Remaining);
        public bool Complete => lines.Count > 0 && Remaining == 0;
        public void Add(SaunaItemDefinition item)
        {
            if (item == null) return;
            var line = lines.Find(l => l.item == item);
            if (line == null) lines.Add(new FoodOrderLine { item = item, quantity = 1 });
            else line.quantity++;
        }
        public int Deliver(HandInventory inventory)
        {
            int count = 0;
            for (int slot = 0; slot < 2; slot++)
            {
                var line = lines.Find(l => l.item == inventory[slot] && l.Remaining > 0);
                if (line == null) continue;
                line.delivered++; inventory.Remove(slot); count++;
            }
            return count;
        }
    }
}
