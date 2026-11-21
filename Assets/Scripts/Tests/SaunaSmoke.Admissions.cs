#if UNITY_STANDALONE_WIN || UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
namespace SuspiciousSauna
{
    public sealed partial class SaunaSmoke
    {
        IEnumerator VerifyNoCleanTable()
        {
            for (int size = 1; size <= 4; size++)
            {
                game.StartGame(); yield return null;
                foreach (var table in game.Tables) table.dirty = true;
                Check(!game.SpawnParty(-1, size) && game.Guests.Count == size && game.Guests.All(g => g.table == -1 && g.state == GuestState.NoSeatApproach), size + " guests enter without reserving a dirty table");
                Check(Mathf.Approximately(game.Rating, 4.75f) && game.Failed == 1 && game.Interface.Receipts.Count == 0, size + " rejected guests incur one normal table-sized penalty and no receipt");
                Check(game.Guests.All(g => g.path.Count > 0 && g.path.All(p => game.Navigation.Walkable(p))), size + " no-seat guests follow valid entrance paths");
                game.DebugTickGuests(2); game.WorldView.Sync();
                var actors = game.WorldView.guestContainer.GetComponentsInChildren<SaunaCharacterView>().Where(v => v.name.EndsWith("_NoSeat")).ToArray();
                Check(game.Guests.All(g => g.state == GuestState.Leaving && g.failedDeparture && g.path.Last() == game.Map.entrance) && actors.Length == size && actors.All(v => !v.guestStatus.patienceGauge.activeSelf && v.guestStatus.departureBubble.activeSelf), size + " no-seat guests turn back with dust bubbles and no seated gauges");
                if (size == 4)
                {
                    game.DebugPosition(game.Map.entrance + new Vector2(-2, 2.5f));
                    yield return null; yield return Capture("19-no-clean-table.png");
                }
                game.DebugTickGuests(10); game.WorldView.Sync();
                Check(game.Guests.Count == 0 && Mathf.Approximately(game.Rating, 4.75f) && game.Failed == 1 && game.Tables.All(t => t.dirty && t.service == TableServiceState.Empty), size + " no-seat departures finish without another penalty or table mutation");
            }
            game.StartGame(); foreach (var table in game.Tables) table.dirty = true;
            game.DebugTool(true); game.FinishCleaning(game.Tables[0]);
            Check(game.SpawnParty(-1, 4) && game.Guests.All(g => g.table == 0) && game.Failed == 0 && game.Rating == 5, "cleaning a single table restores normal admission without a penalty");
            game.StartGame(); game.SpawnParty(0, 2);
            foreach (var table in game.Tables.Skip(1)) table.dirty = true;
            Check(!game.SpawnParty(-1, 3) && game.Guests.Count(g => g.table == -1) == 3 && game.Guests.Count(g => g.table == 0) == 2 && game.Failed == 1, "occupied clean table plus dirty remaining tables cannot seat another party");
            game.StartGame();
            for (int i = 0; i < game.tuning.maxOccupiedTables; i++) game.SpawnParty(i, 1);
            Check(!game.SpawnParty() && game.Guests.All(g => g.table >= 0) && game.Failed == 0 && game.Rating == 5, "configured occupancy cap does not punish the player while a clean free table exists");
            game.StartGame(); foreach (var table in game.Tables) table.dirty = true;
            game.DebugRating(1.25f); game.SpawnParty(-1, 4);
            Check(game.State == ScreenState.GameOver && game.Rating == 1 && game.Failed == 1, "one rejected party can trigger game over without multiplying the penalty");
            game.StartGame();
            Check(game.Guests.Count == 0 && game.Tables.All(t => !t.dirty) && game.Failed == 0 && game.Rating == 5, "restart clears rejected guests and their failure state");
        }
    }
}
#endif
