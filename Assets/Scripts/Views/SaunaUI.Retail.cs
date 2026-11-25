using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SuspiciousSauna
{
    public sealed partial class SaunaUI
    {
        public SaunaRetailPickerView retailPicker;
        public Text retailStatus;
        public RectTransform retailBagRow;
        public SaunaItemQuantityView[] retailBagItems;
        public SaunaBubbleView retailIndicator;
        void InitializeRetail()
        {
            if (retailPicker != null) retailPicker.Initialize(game);
            game.RetailAccepted += ShowRetailReceipt; game.RetailResolved += HideRetailReceipt;
        }
        void ShowRetailReceipt(RetailOrder order)
        {
            var view = Instantiate(receiptPrefab, receiptsScroll.content); view.name = "Receipt_POS";
            var receipt = new Receipt { retailOrder = order, view = view }; Receipts.Insert(0, receipt);
            view.Clicked = () => { Receipts.Remove(receipt); Receipts.Insert(0, receipt); receiptsScroll.verticalNormalizedPosition = 1; };
            receiptsScroll.verticalNormalizedPosition = 1;
        }
        void HideRetailReceipt(RetailOrder order)
        {
            foreach (var receipt in Receipts) if (receipt.retailOrder == order) receipt.closing = true;
            receiptsScroll.StopMovement(); receiptsScroll.verticalNormalizedPosition = 1;
        }
        void PresentRetailHUD()
        {
            if (retailStatus == null) return;
            retailStatus.text = "포스기  " + game.Model.retailQueue.Count + " / " + game.tuning.retailQueueCapacity + (game.ActiveRetailOrder != null ? "  ·  준비 중" : "");
            bool showBag = game.ActiveRetailOrder != null || game.Model.retailBag.Count > 0;
            retailBagRow.gameObject.SetActive(showBag);
            if (showBag)
            {
                heldItemsText.text = (game.ActiveRetailOrder != null ? "포스기 잡화  " : "테이블 잡화  ") + game.Model.retailBag.Count + " / " + game.Model.retailBag.Capacity;
                for (int i = 0; i < retailBagItems.Length; i++) retailBagItems[i].Present(game.Model.retailBag[i], 1);
            }
        }
        void LayoutRetail(float scale)
        {
            if (retailPicker == null) return;
            statusScroll.content.sizeDelta = new Vector2(statusScroll.content.sizeDelta.x, 650);
            PlaceModal(retailPicker.card, null, 650, 670, scale);
            int columns = Mathf.Clamp(Mathf.FloorToInt((retailPicker.card.rect.width - 56) / (180 * scale)), 1, 3);
            retailPicker.content.localScale = Vector3.one * scale;
            retailPicker.content.sizeDelta = new Vector2(columns * 180, Mathf.CeilToInt(retailPicker.choices.Length / (float)columns) * 120);
            for (int i = 0; i < retailPicker.choices.Length; i++) Box((RectTransform)retailPicker.choices[i].button.transform, i % columns * 180, i / columns * 120, 164, 108);
            retailPicker.scroll.horizontal = false;
            float bagScale = Mathf.Min(scale, (retailPicker.card.rect.width - 56) / 320);
            retailPicker.bagRow.localScale = Vector3.one * bagScale;
            retailPicker.bagRow.anchorMin = retailPicker.bagRow.anchorMax = new Vector2(.5f, 0);
            retailPicker.bagRow.pivot = new Vector2(.5f, 0); retailPicker.bagRow.anchoredPosition = new Vector2(0, 86);
            retailPicker.bagRow.sizeDelta = new Vector2(320, 56);
            var scrollRect = (RectTransform)retailPicker.scroll.transform;
            scrollRect.offsetMin = new Vector2(28, Mathf.Max(210, 86 + 56 * bagScale + 20));
        }
        void UpdateRetailIndicator()
        {
            if (retailIndicator == null) return;
            var customer = game.RetailFront;
            bool active = customer != null && (customer.state == RetailCustomerState.Waiting || customer.state == RetailCustomerState.Ordered);
            if (!active) { retailIndicator.gameObject.SetActive(false); return; }
            var p = Play; Vector2 raw = Point(customer.position + Vector2.up * 2);
            bool outside = !p.Contains(raw); retailIndicator.gameObject.SetActive(outside);
            if (!outside) return;
            Vector2 delta = raw - p.center; float pad = 26;
            float ratio = Mathf.Min((p.width * .5f - pad) / Mathf.Max(.001f, Mathf.Abs(delta.x)), (p.height * .5f - pad) / Mathf.Max(.001f, Mathf.Abs(delta.y)));
            var position = p.center + delta * ratio;
            Box(retailIndicator.rect, position.x - 20, position.y - 20, 40, 40);
            retailIndicator.label.text = "!";
            retailIndicator.direction.text = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? (delta.x > 0 ? "›" : "‹") : (delta.y > 0 ? "↓" : "↑");
        }
    }
}
