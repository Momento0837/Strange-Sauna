#if UNITY_STANDALONE_WIN || UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
namespace SuspiciousSauna
{
    public sealed partial class SaunaSmoke
    {
        SaunaCharacterView[] TableActors(int index) => game.WorldView.guestContainer.GetComponentsInChildren<SaunaCharacterView>()
            .Where(v => v.name.EndsWith("_Table_" + (index + 1).ToString("00"))).ToArray();
        IEnumerator VerifyGuestWaiting()
        {
            game.StartGame(); yield return null;
            game.SpawnParty(4, 1); game.SpawnParty(5, 4); game.SpawnParty(6, 2); game.DebugTickGuests(50);
            game.WorldView.Sync();
            var table = game.Tables[5]; var actors = TableActors(5);
            Check(actors.Length == 4 && actors.All(v => v.guestStatus != null && v.guestStatus.patienceGauge.activeSelf), "all four seated guests have authored horizontal patience gauges");
            foreach (float fraction in new[] { .51f, .5f, .31f, .3f, .01f })
            {
                table.patience = table.totalPatience * fraction; game.WorldView.Sync();
                Check(actors.All(v => Mathf.Abs(v.guestStatus.remaining.fillAmount - fraction) < .001f && v.guestStatus.remaining.color == SaunaReceiptView.TimerColor(fraction)), "shared guest gauges match receipt timer at " + Mathf.RoundToInt(fraction * 100) + " percent");
            }
            game.Tables[4].patience = 70; table.patience = 45; game.Tables[6].patience = 20;
            game.DebugPosition(game.Map.tables[5] + Vector2.down * 2.2f);
            yield return null; yield return Capture("16-waiting-gauges.png");
            table.patience = .01f; game.DebugTickGuests(.02f); game.WorldView.Sync();
            Check(Mathf.Approximately(game.Rating, 4.75f) && game.Failed == 1 && table.dirty && game.Guests.Where(g => g.table == 5).All(g => g.state == GuestState.Leaving && g.failedDeparture && g.departureReaction > 0), "late order sends entire party out grumbling with one table penalty");
            Check(actors.All(v => !v.guestStatus.patienceGauge.activeSelf && v.guestStatus.departureBubble.activeSelf), "waiting timeout replaces each guest gauge with a dust bubble");
            yield return null; yield return Capture("17-wait-timeout.png");
            game.DebugTickGuests(2.3f); game.WorldView.Sync();
            Check(actors.All(v => v != null && !v.guestStatus.departureBubble.activeSelf), "dust bubbles disappear after a short departure reaction");
            game.StartGame(); yield return null;
            game.SpawnParty(5, 4); game.DebugTickGuests(50); game.Accept(game.Tables[5]);
            table = game.Tables[5]; game.WorldView.Sync(); actors = TableActors(5);
            Check(actors.All(v => v.guestStatus.patienceGauge.activeSelf && v.guestStatus.remaining.fillAmount > .99f), "accepting order starts a full delivery gauge on every seated guest");
            table.patience = table.totalPatience * .3f; game.WorldView.Sync(); game.Pause();
            float frozen = table.patience; yield return new WaitForSecondsRealtime(.2f);
            Check(table.patience == frozen && actors.All(v => Mathf.Abs(v.guestStatus.remaining.fillAmount - .3f) < .001f), "paused delivery freezes head gauges with the shared order timer");
            game.Resume(); game.DebugPosition(game.Map.tables[5] + Vector2.down * 2.2f);
            table.patience = .01f; game.DebugTickGuests(.02f); game.WorldView.Sync();
            Check(Mathf.Approximately(game.Rating, 4.75f) && game.Failed == 1 && actors.All(v => v.guestStatus.departureBubble.activeSelf) && game.Interface.Receipts.Single().closing, "late food delivery uses same dust departure and closes receipt with one penalty");
            float rating = game.Rating; game.Resolve(table, false);
            Check(game.Rating == rating && game.Failed == 1, "expired party cannot receive a second penalty");
            yield return null; yield return Capture("18-delivery-timeout.png");
        }
    }
}
#endif
