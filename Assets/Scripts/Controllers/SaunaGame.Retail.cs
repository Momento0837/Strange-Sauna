using System;
using System.Linq;
using UnityEngine;

namespace SuspiciousSauna
{
    public sealed partial class SaunaGame
    {
        public SaunaRetailLayout retailLayout;
        public event Action<RetailOrder> RetailAccepted;
        public event Action<RetailOrder> RetailResolved;
        public RetailCustomer RetailFront => Model.retailQueue.FirstOrDefault();
        public RetailOrder ActiveRetailOrder => RetailFront != null && RetailFront.state == RetailCustomerState.Ordered ? RetailFront.order : null;
        public bool TargetRetailShelf { get; private set; }
        float retailSpawnTimer;
        public float RetailDeliverySeconds
        {
            get
            {
                var food = quests.numbers.Concat(quests.letters).Where(q => q != null && q.IsFoodOrder).ToArray();
                float standard = food.Length > 0 ? food.Average(q => q.deliverySeconds) : tuning.orderPatienceSeconds;
                return Mathf.Max(1, standard * tuning.retailTimeMultiplier);
            }
        }
        void ResetRetail() { retailSpawnTimer = 12; TargetRetailShelf = false; }
        public RetailCustomer SpawnRetailCustomer()
        {
            if (retailLayout == null || items.retailMenu == null || items.retailMenu.Length == 0) return null;
            var customer = new RetailCustomer { id = ++nextId, position = Map.entrance,
                waitingSeconds = tuning.orderPatienceSeconds };
            Model.retailCustomers.Add(customer);
            if (Model.retailQueue.Count >= Mathf.Min(tuning.retailQueueCapacity, retailLayout.queueSpots.Length))
            {
                customer.state = RetailCustomerState.OverflowApproach;
                RouteRetail(customer, retailLayout.overflowLookPoint.position);
            }
            else
            {
                Model.retailQueue.Add(customer); customer.state = RetailCustomerState.WalkingToQueue;
                RouteRetail(customer, retailLayout.queueSpots[Model.retailQueue.Count - 1].position);
            }
            return customer;
        }
        void RouteRetail(RetailCustomer customer, Vector2 destination)
        {
            customer.destination = destination; customer.pathIndex = 0;
            customer.path = Navigation.Path(customer.position, destination, customer.id * 31);
        }
        void AdvanceRetailQueue()
        {
            for (int i = 0; i < Model.retailQueue.Count; i++)
            {
                var customer = Model.retailQueue[i];
                if (customer.state == RetailCustomerState.Ordered) continue;
                Vector2 target = retailLayout.queueSpots[i].position;
                if (Vector2.Distance(customer.position, target) > .05f)
                { customer.state = RetailCustomerState.WalkingToQueue; RouteRetail(customer, target); }
            }
        }
        void UpdateRetail(float dt)
        {
            if (retailLayout == null) return;
            retailSpawnTimer -= dt;
            if (retailSpawnTimer <= 0)
            { SpawnRetailCustomer(); retailSpawnTimer = tuning.retailArrivalInterval * UnityEngine.Random.Range(.85f, 1.15f); }
            foreach (var customer in Model.retailCustomers.ToArray())
            {
                var before = customer.state;
                customer.reactionSeconds = Mathf.Max(0, customer.reactionSeconds - dt);
                bool walking = before == RetailCustomerState.WalkingToQueue || before == RetailCustomerState.OverflowApproach || before == RetailCustomerState.Leaving;
                if (walking)
                {
                    float distance = tuning.guestSpeed * (customer.happy ? 1.12f : 1) * dt;
                    while (customer.pathIndex < customer.path.Count && distance > 0)
                    {
                        Vector2 target = customer.path[customer.pathIndex]; float remaining = Vector2.Distance(customer.position, target);
                        if (remaining <= distance) { customer.position = target; distance -= remaining; customer.pathIndex++; }
                        else { customer.position = Vector2.MoveTowards(customer.position, target, distance); distance = 0; }
                    }
                    if (customer.pathIndex >= customer.path.Count)
                    {
                        if (before == RetailCustomerState.Leaving) { Model.retailCustomers.Remove(customer); continue; }
                        if (before == RetailCustomerState.OverflowApproach)
                        {
                            customer.state = RetailCustomerState.Grumbling; customer.reactionSeconds = 1.5f;
                            ApplyRetailRating(-5 * tuning.failurePercent * tuning.retailOverflowPenaltyMultiplier);
                            Say("계산대 줄이 길어 손님이 돌아가요.");
                        }
                        else customer.state = RetailCustomerState.Waiting;
                    }
                }
                else if (before == RetailCustomerState.Grumbling && customer.reactionSeconds <= 0) DepartRetail(customer, false);
                else if (before == RetailCustomerState.Ordered)
                {
                    customer.order.patience = Mathf.Max(0, customer.order.patience - dt);
                    if (customer.order.patience <= 0) FinishRetail(customer, false, 5 * tuning.failurePercent, "기다리던 잡화 손님이 돌아갔어요.");
                }
                else if (before == RetailCustomerState.Waiting && customer == RetailFront)
                {
                    customer.waitingSeconds -= dt;
                    if (customer.waitingSeconds <= 0) FinishRetail(customer, false, 5 * tuning.failurePercent, "포스기의 손님이 주문을 기다리다 돌아갔어요.");
                }
                if (State == ScreenState.GameOver) break;
            }
        }
        public void InteractRetailRegister()
        {
            if (State != ScreenState.Playing) return;
            if (Model.inventory.Count > 0) { RejectPickup("음식을 서빙 중입니다!"); return; }
            if (HasCleaningTool) { RejectPickup("청소 중입니다!"); return; }
            if (retailLayout == null || !retailLayout.AtStaffSide(PlayerPosition)) { Say("카운터 뒤에서 손님을 응대해 주세요."); return; }
            var customer = RetailFront;
            if (customer == null) { Say("계산을 기다리는 손님이 없어요."); return; }
            if (customer.state == RetailCustomerState.WalkingToQueue) { Say("손님이 포스기 앞으로 오고 있어요."); return; }
            if (customer.state == RetailCustomerState.Ordered)
            {
                var result = customer.order.Evaluate(Model.retailBag);
                string message = result.Success ? "잡화 응대 완료!" : $"잡화 전달 · 잘못 {result.wrong} / 초과 {result.excess} / 부족 {result.missing}";
                FinishRetail(customer, result.Success, result.Errors * 5 * tuning.failurePercent * tuning.retailItemPenaltyMultiplier, message);
                return;
            }
            if (customer.state != RetailCustomerState.Waiting) return;
            var order = new RetailOrder { patience = RetailDeliverySeconds, totalPatience = RetailDeliverySeconds };
            int count = UnityEngine.Random.Range(1, 6);
            for (int i = 0; i < count; i++) order.requested.Add(items.retailMenu[UnityEngine.Random.Range(0, items.retailMenu.Length)]);
            order.SampleReceipt(); customer.order = order; customer.state = RetailCustomerState.Ordered;
            RetailAccepted?.Invoke(order); Sound.Play(); Say("잡화 주문을 받았어요. 진열대에서 물건을 담아 주세요.");
        }
        void FinishRetail(RetailCustomer customer, bool success, float penalty, string message)
        {
            if (customer.state != RetailCustomerState.Ordered && customer.state != RetailCustomerState.Waiting) return;
            if (customer.order != null)
            {
                customer.order.resolved = true; customer.order.success = success;
                RetailResolved?.Invoke(customer.order); Model.retailBag.Clear();
            }
            if (State == ScreenState.RetailPicking) CloseRetailPicking();
            Model.retailQueue.Remove(customer); DepartRetail(customer, success); AdvanceRetailQueue();
            if (success) Completed++; else Failed++;
            Sound.Play(success ? 1 : 2); Say(message);
            ApplyRetailRating(success ? 5 * tuning.successPercent : -penalty); WorldView.Sync();
        }
        void DepartRetail(RetailCustomer customer, bool happy)
        {
            customer.state = RetailCustomerState.Leaving; customer.happy = happy; customer.reactionSeconds = 2.2f;
            customer.pathIndex = 0;
            // Step into the side aisle before leaving so the front customer doesn't walk
            // straight through the waiting line.
            Vector2 side = retailLayout.exitSidePoint.position;
            if (customer.position.y > side.y - 2)
            {
                customer.path = Navigation.Path(customer.position, side, customer.id + 197);
                customer.path.AddRange(Navigation.Path(side, Map.entrance, customer.id + 203));
            }
            else customer.path = Navigation.Path(customer.position, Map.entrance, customer.id + 197);
        }
        void ApplyRetailRating(float change)
        {
            Rating = Mathf.Clamp(Rating + change, 1, 5);
            if (Rating <= 1.0001f) SetState(ScreenState.GameOver);
        }
        public void OpenRetailPicking()
        {
            if (State != ScreenState.Playing || !TargetRetailShelf) return;
            if (ActiveRetailOrder == null) { Say("포스기에서 잡화 주문을 먼저 받아 주세요."); return; }
            SetState(ScreenState.RetailPicking); Sound.Play();
        }
        public void CloseRetailPicking() { if (State == ScreenState.RetailPicking) SetState(ScreenState.Playing); }
        public bool PickRetailItem(SaunaItemDefinition item)
        {
            if (State != ScreenState.RetailPicking || ActiveRetailOrder == null || item == null || !items.retailMenu.Contains(item)) return false;
            if (!Model.retailBag.TryAdd(item)) { RejectPickup("잡화는 최대 5개까지 들 수 있어요!"); return false; }
            Sound.Play(1); WorldView.Sync(); return true;
        }
        public void RemoveRetailItem(int slot)
        {
            if (State != ScreenState.RetailPicking || slot < 0 || slot >= Model.retailBag.Capacity) return;
            Model.retailBag.Remove(slot); WorldView.Sync();
        }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        public void DebugTickRetail(float dt) => UpdateRetail(dt);
        public void DebugDelayRetailArrivals(float seconds) => retailSpawnTimer = seconds;
#endif
    }
}
