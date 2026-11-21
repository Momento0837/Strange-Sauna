using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuspiciousSauna
{
    public enum RetailCustomerState { WalkingToQueue, Waiting, Ordered, OverflowApproach, Grumbling, Leaving }
    public sealed class RetailCustomer
    {
        public int id, pathIndex;
        public Vector2 position, destination;
        public List<Vector2> path = new List<Vector2>();
        public RetailCustomerState state;
        public RetailOrder order;
        public float waitingSeconds, reactionSeconds;
        public bool happy;
    }

    public sealed class RetailItemCount
    {
        public SaunaItemDefinition item;
        public int quantity;
    }

    // Requested units and the randomly sampled receipt stay separate: hiding a unit
    // never changes the actual order or the multiset used to evaluate delivery.
    public sealed class RetailOrder
    {
        public readonly List<SaunaItemDefinition> requested = new List<SaunaItemDefinition>();
        public readonly List<SaunaItemDefinition> receiptItems = new List<SaunaItemDefinition>();
        public float patience, totalPatience;
        public bool resolved, success;
        public static IEnumerable<RetailItemCount> Counts(IEnumerable<SaunaItemDefinition> items) =>
            items.Where(i => i != null).GroupBy(i => i).Select(g => new RetailItemCount { item = g.Key, quantity = g.Count() });
        public void SampleReceipt()
        {
            receiptItems.Clear(); var remaining = new List<SaunaItemDefinition>(requested);
            while (remaining.Count > 0 && receiptItems.Count < 3)
            {
                int index = Random.Range(0, remaining.Count);
                receiptItems.Add(remaining[index]); remaining.RemoveAt(index);
            }
        }
        public RetailDelivery Evaluate(HandInventory bag)
        {
            var result = new RetailDelivery(); var unmatched = new List<SaunaItemDefinition>(requested);
            for (int i = 0; i < bag.Capacity; i++)
            {
                var item = bag[i]; if (item == null) continue;
                if (unmatched.Remove(item)) result.matched++;
                else if (requested.Contains(item)) result.excess++;
                else result.wrong++;
            }
            result.missing = unmatched.Count; return result;
        }
    }
    public struct RetailDelivery
    {
        public int matched, wrong, excess, missing;
        public int Errors => wrong + excess + missing;
        public bool Success => Errors == 0;
    }
}
