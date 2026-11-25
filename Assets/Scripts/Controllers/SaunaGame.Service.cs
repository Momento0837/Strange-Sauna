using System.Linq;
using UnityEngine;
namespace SuspiciousSauna
{
    // Food service commands use the generic hand inventory without coupling it to UI or food.
    public sealed partial class SaunaGame
    {
        public SaunaItemCatalog items;
        public bool TargetWasteBin { get; private set; }
        public bool WasteConfirmed => wasteConfirmed;
        bool wasteConfirmed;
        public void OpenCooking()
        {
            if (State != ScreenState.Playing || TargetStation != 0) return;
            if (ActiveRetailOrder != null) { RejectPickup("카운터에 기다리는 손님이 있어요!"); return; }
            if (CarryingTableGoods) { RejectPickup("가방의 잡화를 테이블에 전달하거나 진열대에 반납해 주세요."); return; }
            if (HasCleaningTool) { RejectPickup("청소 도구를 먼저 제자리에 두어 주세요."); return; }
            if (Model.inventory.Full) { RejectPickup("양 손이 꽉 찼어요!"); return; }
            SetState(ScreenState.Cooking); Sound.Play();
        }
        public void CloseCooking() { if (State == ScreenState.Cooking) SetState(ScreenState.Playing); }
        public bool Cook(SaunaItemDefinition item)
        {
            if (State != ScreenState.Cooking || ActiveRetailOrder != null || HasCleaningTool || item == null || !items.cookingMenu.Contains(item)) return false;
            if (!Model.inventory.TryAdd(item)) { RejectPickup("양 손이 꽉 찼어요!"); return false; }
            Sound.Play(1); Say(item.displayName + " 준비 완료!"); WorldView.Sync();
            if (Model.inventory.Full) CloseCooking();
            return true;
        }
        void DeliverTableItems(SaunaTable table)
        {
            var order = table.itemOrder;
            if (table.IsTableRetailOrder && ActiveRetailOrder != null) { Say("포스기에서 받은 잡화 주문을 먼저 전달해 주세요."); return; }
            var batch = table.IsTableRetailOrder ? order.DeliverRetail(Model.retailBag)
                : new RetailDelivery { matched = order.Deliver(Model.inventory) };
            if (batch.matched == 0 && batch.Incorrect == 0)
            { Say("이 테이블에 필요한 " + (table.IsTableRetailOrder ? "잡화를" : "음식을") + " 들고 있지 않아요."); return; }
            WorldView.Sync();
            bool additionalRequest = order.RevealNextRequest();
            string progress = additionalRequest ? "잠깐! 추가 주문도 가져다주세요." : "남은 " + table.ItemLabel + " " + order.Remaining + "개";
            if (batch.Incorrect > 0)
            {
                float penalty = RetailMistakePenalty(batch.Incorrect);
                ApplyRetailRating(-penalty);
                if (order.Complete) { Resolve(table, true); progress = "주문 완료"; }
                else if (additionalRequest) progress = "추가 잡화 주문";
                Sound.Play(2); Say($"잘못 {batch.wrong}개 · 초과 {batch.excess}개 · 별점 −{penalty:0.###}점 · {progress}");
            }
            else if (order.Complete) Resolve(table, true);
            else { Sound.Play(1); Say(additionalRequest ? progress : batch.matched + "개 전달했어요. " + progress); }
        }
        void InteractWasteBin()
        {
            if (Model.inventory.Count == 0) { wasteConfirmed = false; Say("버릴 음식이 없어요."); return; }
            if (!wasteConfirmed) { wasteConfirmed = true; Say("양 손의 음식을 전부 버립니다!"); return; }
            Model.inventory.Clear(); wasteConfirmed = false; Sound.Play(2);
            Say("음식을 버렸어요."); WorldView.Sync();
        }
        void RejectPickup(string message) { Say(message); WorldView.player.Reject(PlayedSeconds); Sound.Play(2); }
    }
}
