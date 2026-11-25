using System.Collections.Generic;
using System.Linq;
namespace SuspiciousSauna
{
    public sealed class TableItemLine
    {
        public SaunaItemDefinition item;
        public int quantity, delivered;
        public int Remaining => quantity - delivered;
    }
    public sealed class TableItemOrder
    {
        readonly List<TableItemLine> firstRequest = new List<TableItemLine>();
        List<TableItemLine> nextRequest;
        public List<TableItemLine> lines => IsAdditionalRequest ? nextRequest : firstRequest;
        public bool IsAdditionalRequest { get; private set; }
        public int Quantity => lines.Sum(l => l.quantity);
        public int Remaining => lines.Sum(l => l.Remaining);
        public int DeliveryMistakes { get; private set; }
        int TotalRemaining => firstRequest.Sum(l => l.Remaining) + (nextRequest?.Sum(l => l.Remaining) ?? 0);
        public bool Complete => firstRequest.Count > 0 && TotalRemaining == 0;

        public static TableItemOrder CreateRetail(SaunaItemCatalog catalog)
        {
            int total = UnityEngine.Random.Range(4, 6);
            int firstCount = total == 4 ? 2 : UnityEngine.Random.Range(2, 4);
            var order = new TableItemOrder { nextRequest = new List<TableItemLine>() };
            for (int i = 0; i < total; i++)
                Add(i < firstCount ? order.firstRequest : order.nextRequest, catalog.RandomRetail());
            return order;
        }

        public bool RevealNextRequest()
        {
            if (nextRequest == null || IsAdditionalRequest || Remaining > 0 || Complete) return false;
            IsAdditionalRequest = true;
            return true;
        }
        public void Add(SaunaItemDefinition item) => Add(lines, item);
        static void Add(List<TableItemLine> request, SaunaItemDefinition item)
        {
            if (item == null) return;
            var line = request.Find(l => l.item == item);
            if (line == null) request.Add(new TableItemLine { item = item, quantity = 1 });
            else line.quantity++;
        }
        public int Deliver(HandInventory inventory) => Deliver(lines, inventory);
        static int Deliver(List<TableItemLine> request, HandInventory inventory)
        {
            int count = 0;
            for (int slot = 0; slot < inventory.Capacity; slot++)
            {
                var line = request.Find(l => l.item == inventory[slot] && l.Remaining > 0);
                if (line == null) continue;
                line.delivered++; inventory.Remove(slot); count++;
            }
            return count;
        }

        public RetailDelivery DeliverRetail(HandInventory bag)
        {
            // Shared products fill the announced quantity before the hidden quantity.
            int matched = Deliver(firstRequest, bag);
            if (nextRequest != null) matched += Deliver(nextRequest, bag);
            var batch = new RetailDelivery { matched = matched, missing = TotalRemaining };
            for (int slot = 0; slot < bag.Capacity; slot++)
            {
                var item = bag[slot];
                if (item == null) continue;
                if (firstRequest.Exists(line => line.item == item) || (nextRequest != null && nextRequest.Exists(line => line.item == item))) batch.excess++;
                else batch.wrong++;
                bag.Remove(slot);
            }
            DeliveryMistakes += batch.Incorrect;
            return batch;
        }
    }
}
