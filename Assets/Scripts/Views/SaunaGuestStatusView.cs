using UnityEngine;
using UnityEngine.UI;
namespace SuspiciousSauna
{
    // Each seated guest displays the table's shared timer. Rating and resolution
    // still happen once per table; this view never owns a separate countdown.
    public sealed class SaunaGuestStatusView : MonoBehaviour
    {
        public GameObject patienceGauge, departureBubble;
        public Image remaining;
        public void Present(Guest guest, SaunaTable table)
        {
            bool waiting = table != null && guest.state == GuestState.Seated &&
                (table.service == TableServiceState.Arriving || table.service == TableServiceState.Waiting || table.service == TableServiceState.Ordered);
            patienceGauge.SetActive(waiting);
            float fraction = table != null ? Mathf.Clamp01(table.patience / Mathf.Max(1, table.totalPatience)) : 0;
            remaining.fillAmount = fraction; remaining.color = SaunaReceiptView.TimerColor(fraction);
            departureBubble.SetActive(guest.state == GuestState.Leaving && guest.failedDeparture && guest.departureReaction > 0);
        }
    }
}
