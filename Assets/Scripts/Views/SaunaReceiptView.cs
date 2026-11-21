using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
namespace SuspiciousSauna
{
    public sealed class SaunaReceiptView : MonoBehaviour, IPointerClickHandler
    {
        public RectTransform rect;
        public GameObject normal, compact;
        public Text tableLabel, code, title, description, status, compactLabel;
        public Image patience;
        public Image compactPatience;
        public Text timerText, compactTimerText;
        public GameObject retailLines;
        public SaunaItemQuantityView[] retailItems;
        public System.Action Clicked;
        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
        public static Color TimerColor(float fraction) => fraction > .5f ? SaunaArt.C("4D9161") : fraction > .3f ? SaunaArt.C("D58B32") : SaunaArt.C("C8544D");
        public void Present(SaunaTable table, bool closing, bool small)
        {
            normal.SetActive(!small); compact.SetActive(small);
            if (retailLines != null) retailLines.SetActive(false);
            description.gameObject.SetActive(true);
            tableLabel.text = "TABLE " + (table.index + 1).ToString("00");
            code.text = "#" + table.quest.id; title.text = table.IsFoodOrder ? "음식 주문  ·  " + table.partySize + "인" : table.quest.title;
            string body = table.quest.description;
            description.text = table.IsFoodOrder ? string.Join("\n", table.foodOrder.lines.Select(l => (l.Remaining == 0 ? "✓ " : "· ") + l.item.displayName + "  " + l.delivered + "/" + l.quantity)) : string.IsNullOrEmpty(body) || body.StartsWith("여기에 퀘스트") ? "F로 부탁을 완료해 주세요." : body;
            string progress = closing ? (table.failed ? "시간 초과" : "응대 완료") : table.IsFoodOrder ? "남은 음식 " + table.foodOrder.Remaining + "개" : "F로 완료";
            status.text = tableLabel.text + "\n" + progress;
            compactLabel.text = tableLabel.text + " · " + (table.IsFoodOrder ? string.Join(", ", table.foodOrder.lines.Select(l => l.item.displayName + " " + l.Remaining)) : table.quest.id + " " + table.quest.title);
            float fraction = Mathf.Clamp01(table.patience / Mathf.Max(1, table.totalPatience));
            patience.fillAmount = fraction; patience.color = TimerColor(fraction);
            if (timerText != null) timerText.text = Mathf.CeilToInt(Mathf.Max(0, table.patience)).ToString();
            if (compactPatience != null) { compactPatience.fillAmount = fraction; compactPatience.color = patience.color; compactTimerText.text = timerText.text; }
        }
        public void Present(RetailOrder order, bool closing, bool small)
        {
            normal.SetActive(!small); compact.SetActive(small);
            description.gameObject.SetActive(false); retailLines.SetActive(true);
            tableLabel.text = "POS"; code.text = "잡화"; title.text = "잡화 구매";
            status.text = "포스기\n" + (closing ? (order.success ? "응대 완료" : "응대 종료") : "물건 전달");
            var lines = RetailOrder.Counts(order.receiptItems).ToArray();
            for (int i = 0; i < retailItems.Length; i++) retailItems[i].Present(i < lines.Length ? lines[i].item : null, i < lines.Length ? lines[i].quantity : 0);
            compactLabel.text = "POS · " + string.Join(", ", lines.Select(l => l.item.displayName + " ×" + l.quantity));
            float fraction = Mathf.Clamp01(order.patience / Mathf.Max(1, order.totalPatience));
            patience.fillAmount = fraction; patience.color = TimerColor(fraction);
            timerText.text = Mathf.CeilToInt(Mathf.Max(0, order.patience)).ToString();
            compactPatience.fillAmount = fraction; compactPatience.color = patience.color; compactTimerText.text = timerText.text;
        }
    }
}
