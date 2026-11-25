#if UNITY_STANDALONE_WIN || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SuspiciousSauna
{
    // Opt-in local checks. Normal builds can run these without Unity's network debug connection.
    public sealed partial class SaunaSmoke : MonoBehaviour
    {
        SaunaGame game;
        readonly List<string> checks = new List<string>();
        string output;
        bool failed;
        void Check(bool value, string label)
        {
            checks.Add((value ? "PASS " : "FAIL ") + label);
            if (!value) { failed = true; Debug.LogError("SMOKE FAILED: " + label); }
        }
        IEnumerator Start()
        {
            game = GetComponent<SaunaGame>();
            Application.logMessageReceived += CheckRuntimeLog;
            game.SessionStarted += () => game.DebugDelayArrivals(10000);
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--sauna-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "QA");
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            yield return null;
            Check(game.State == ScreenState.Menu, "opens at main menu");
            Check(game.WorldView.layout.tables.Length == 8 && game.WorldView.player != null, "authored scene objects exist before starting");
            Check(game.Interface.canvas != null && game.Interface.startButton != null && game.Interface.uiScaleDropdown != null, "native editable Canvas UI is wired");
            var body = game.WorldView.player.body;
            var originalSprite = body.sprite;
            body.sprite = game.WorldView.layout.tables[0].tableSprite.sprite;
            game.WorldView.Sync();
            Check(body.sprite == game.WorldView.layout.tables[0].tableSprite.sprite, "view sync preserves a manually replaced Sprite");
            body.sprite = originalSprite;
            foreach (var quest in game.quests.numbers.Concat(game.quests.letters)) quest.kind = QuestKind.FoodOrder;
            yield return Capture("01-main-menu.png");
            game.StartGame();
            Check(game.Rating == 5 && game.Guests.Count == 0 && !game.HasCleaningTool, "fresh session state");
            Check(game.Tables.Count == 8 && game.Interface.bubbles.Length == 8 && game.Map.tables.Select(p => p.x).Distinct().Count() == 4 && game.Map.tables.GroupBy(p => p.y).Count() == 2 && game.Map.tables.GroupBy(p => p.y).All(row => row.Count() == 4), "scene model and indicators use four columns and two rows");
            Check(game.Map.bounds.size == new Vector2(24, 23) && Mathf.Approximately(Vector2.Distance(game.Map.tables[0], game.Map.tables[1]), 5) && Mathf.Approximately(Vector2.Distance(game.Map.tables[0], game.Map.tables[4]), 5), "actual map is 24 by 23 with five-unit table spacing on both axes");
            Check(!game.SpawnParty(8, 1), "removed ninth table cannot accept guests");
            game.DebugPosition(new Vector2(1, 5)); game.MovePlayer(Vector2.up, 1);
            Check(Mathf.Approximately(game.tuning.playerSpeed, 8.4f) && Vector2.Distance(game.PlayerPosition, new Vector2(1, 13.4f)) < .001f, "player moves 8.4 units in one second, seventy percent of previous speed");
            game.DebugPosition(game.Map.tables[0] + Vector2.down * 3); game.MovePlayer(Vector2.up, 1);
            Check(game.PlayerPosition.y < game.Map.tables[0].y - 1 && game.Navigation.Walkable(game.PlayerPosition), "player movement cannot tunnel through a table during a long frame");
            float originalHalf = game.WorldView.cameraHalf;
            game.WorldView.cameraHalf = 15; game.DebugPosition(game.Map.bounds.center);
            yield return null; yield return Capture("15-four-by-two.png");
            game.WorldView.cameraHalf = originalHalf;
            Check(game.SpawnParty(5, 3), "three-person party created");
            Check(game.Guests.Select(g => g.seat).Distinct().Count() == 3, "three unique random seats");
            game.DebugTickGuests(50);
            Check(game.Guests.All(g => g.state == GuestState.Seated), "party walks to its seats");
            game.WorldView.Sync();
            Check(game.WorldView.SpawnedGuestCount == 3, "guests instantiate assigned character prefabs only");
            game.DebugPosition(game.Map.playerStart);
            yield return Capture("02-gameplay.png");
            var order = game.Tables[5];
            var tableView = game.WorldView.layout.tables.Single(t => t.index == order.index);
            var tableCenter = game.Map.tables[order.index];
            var footprint = tableView.GetComponent<SaunaObstacle>().WorldRect();
            game.DebugPosition(tableCenter + Vector2.up * 2.2f);
            game.MovePlayer(Vector2.down, .4f);
            game.RefreshTarget();
            yield return Capture("21-order-from-behind.png");
            Check(game.TargetOrderTable == order && game.PlayerPosition.y > footprint.yMax && game.WorldView.player.sorting.sortingOrder < tableView.tableSprite.sortingOrder,
                "walking down to take an order stops behind the tabletop");
            Check(game.Interface.interactionText.canvas.sortingOrder > tableView.tableSprite.sortingOrder, "table order hint stays readable from behind");
            game.HandleInteraction(true, true, .016f);
            Check(order.service == TableServiceState.Ordered && game.Interface.Receipts.Count == 1, "F creates one order and one receipt for the party");
            Check(order.itemOrder.Quantity == 3 && order.partySize == 3, "three guests request three items on one order");
            game.DebugPosition(tableCenter + Vector2.down * 2.2f);
            game.MovePlayer(Vector2.up, .4f);
            game.RefreshTarget();
            yield return Capture("22-order-from-front.png");
            var dirtOrder = tableView.dirt.GetComponent<SpriteRenderer>().sortingOrder;
            Check(game.TargetOrderTable == order && game.PlayerPosition.y < footprint.yMin && game.WorldView.player.sorting.sortingOrder > dirtOrder && dirtOrder > tableView.tableSprite.sortingOrder,
                "walking up to serve draws the player in front of the table and its dirt");
            game.HandleInteraction(true, true, .016f);
            Check(order.service == TableServiceState.Ordered && game.Completed == 0, "empty-hand second F cannot complete food order");
            yield return new WaitForSecondsRealtime(.6f);
            yield return Capture("03-order-receipt.png");
            float priorTime = order.patience, priorPlayTime = game.PlayedSeconds;
            game.Pause();
            yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Approximately(priorTime, order.patience) && Mathf.Approximately(priorPlayTime, game.PlayedSeconds), "pause freezes table timer and game clock");
            game.OpenSettings(); int previousScale = game.UiScale; game.SetUiScale(3);
            Check(game.UiScale == previousScale && game.InSession, "in-game UI scale locked");
            yield return Capture("04-paused-settings.png");
            game.CloseSettings(); game.Resume(); game.DebugRating(4);
            var wanted = order.itemOrder.lines[0].item;
            var wrong = game.items.cookingMenu.First(item => !order.itemOrder.lines.Any(l => l.item == item));
            AtCooking(); game.HandleInteraction(true, true, .016f);
            Check(game.State == ScreenState.Cooking && game.Interface.cooking.foods.Length == 9, "F at counter opens all nine food choices");
            float beforeCooking = order.patience; game.DebugTickGuests(.5f);
            Check(order.patience < beforeCooking, "order timer continues while choosing food");
            Pick(wanted); Pick(wrong);
            Check(game.Model.inventory.Right == wanted && game.Model.inventory.Left == wrong && game.State == ScreenState.Playing, "first item right, second left, full hands close cooking");
            game.WorldView.Sync();
            Check(game.WorldView.player.rightHand.icon.sprite == wanted.icon && game.WorldView.player.leftHand.icon.sprite == wrong.icon, "held food icons match physical hands");
            game.HandleInteraction(true, true, .016f); game.WorldView.Sync();
            Check(game.State == ScreenState.Playing && game.Toast == "양 손이 꽉 찼어요!" && game.WorldView.player.sweatEffect.activeSelf, "full hands block cooking with narration and sweat");
            yield return new WaitForSecondsRealtime(.2f);
            yield return Capture("10-full-hands.png");
            game.DebugPosition(game.Map.tables[5] + Vector2.down * 2); game.HandleInteraction(true, true, .016f);
            Check(game.Model.inventory.Right == null && game.Model.inventory.Left == wrong && order.itemOrder.Remaining == 2 && game.Completed == 0, "partial delivery frees matching hand and preserves wrong food");
            AtCooking(); game.HandleInteraction(true, true, .016f); Pick(wanted);
            Check(game.Model.inventory.Right == wanted && game.Model.inventory.Left == wrong, "refilling right hand preserves left item");
            game.DebugPosition(game.Map.stations[3] + Vector2.down * 1.5f); game.HandleInteraction(true, true, .016f);
            Check(!game.HasCleaningTool && game.Model.inventory.Count == 2, "cannot take cleaning tool while holding food");
            AtWaste(); game.HandleInteraction(true, true, .016f);
            Check(game.WasteConfirmed && game.Model.inventory.Count == 2 && game.Toast == "양 손의 음식을 전부 버립니다!", "first waste F only warns");
            game.DebugPosition(game.Map.playerStart); AtWaste(); game.HandleInteraction(true, true, .016f);
            Check(game.Model.inventory.Count == 2, "moving away cancels waste confirmation");
            game.HandleInteraction(true, true, .016f);
            Check(game.Model.inventory.Count == 0 && !game.WasteConfirmed, "second consecutive waste F empties hands");
            game.DebugPosition(game.Map.stations[3] + Vector2.down * 1.5f); game.HandleInteraction(true, true, .016f);
            AtCooking(); game.HandleInteraction(true, true, .016f);
            Check(game.HasCleaningTool && game.State == ScreenState.Playing, "cleaning tool blocks cooking");
            game.DebugPosition(game.Map.stations[3] + Vector2.down * 1.5f); game.HandleInteraction(true, true, .016f);
            AtCooking(); game.HandleInteraction(true, true, .016f);
            yield return null;
            yield return Capture("11-cooking-menu.png"); game.CloseCooking();
            ServeAll(order);
            Check(game.Guests.All(c => c.state == GuestState.Leaving) && Mathf.Abs(game.Rating - 4.15f) < .001f && game.Completed == 1, "complete delivery sends whole party out and adds 3 percent once");
            Check(game.Interface.Receipts[0].closing && game.Tables[5].dirty, "one receipt animates out and table becomes dirty");
            game.DebugTickGuests(50);
            Check(game.Guests.Count == 0 && game.CanClean(5), "all guests exit through entrance, table cleanable");
            Check(!game.SpawnParty(5, 1), "dirty table cannot be reserved");
            game.DebugPosition(game.Map.tables[5] + Vector2.down * 1.7f);
            game.HandleInteraction(true, true, .1f);
            Check(game.Tables[5].dirty && game.Tables[5].cleaningMode == -1, "cleaning requires tool from counter");
            game.DebugPosition(SaunaMap.Stations[3] + Vector2.down * 1.5f); game.HandleInteraction(true, true, .016f);
            Check(game.HasCleaningTool, "F picks up cleaning tool at counter");
            game.DebugPosition(game.Map.tables[5] + Vector2.down * 1.7f);
            // Drive the held key explicitly while capturing. Live keyboard polling would release it.
            game.enabled = false;
            game.Tables[5].cleaningMode = 0; game.HandleInteraction(false, true, 1.5f);
            yield return Capture("20-cleaning-hold-half.png");
            var track = game.Interface.cleaningTrack;
            var fill = game.Interface.cleaningProgress.rectTransform;
            var hint = game.Interface.interactionHint;
            float trackTop = hint.InverseTransformPoint(track.TransformPoint(new Vector2(0, track.rect.yMax))).y;
            Check(track.gameObject.activeInHierarchy && trackTop < hint.rect.yMin && fill.rect.height > 0
                && Mathf.Abs(fill.rect.width - track.rect.width * .5f) < .01f, "half-held cleaning key draws a half-width bar below the hint");
            int gaugeOrder = fill.GetComponentInParent<Canvas>().sortingOrder;
            Check(game.WorldView.layout.tables.All(t => t.numberBadge.sortingOrder < gaugeOrder && t.numberLabel.canvas.sortingOrder < gaugeOrder),
                "table number badges cannot cover the cleaning gauge");
            game.HandleInteraction(false, false, .1f);
            yield return new WaitForEndOfFrame();
            Check(track.gameObject.activeInHierarchy && Mathf.Approximately(fill.rect.width, 0), "releasing F empties the visible hold gauge");
            Check(game.Tables[5].held == 0 && game.Tables[5].dirty, "release resets continuous hold");
            game.HandleInteraction(false, true, 2.9f);
            Check(game.Tables[5].dirty, "hold must last full three seconds");
            game.HandleInteraction(false, true, .11f);
            Check(!game.Tables[5].dirty, "three-second hold cleans table");
            yield return new WaitForEndOfFrame();
            Check(!track.gameObject.activeInHierarchy, "completed cleaning dismisses the gauge");
            game.enabled = true;
            game.Tables[5].dirty = true; game.Tables[5].cleaningMode = 1;
            for (int n = 0; n < 9; n++) game.HandleInteraction(true, true, .01f);
            Check(game.Tables[5].dirty && game.Tables[5].presses == 9, "nine presses do not finish mash");
            yield return Capture("05-cleaning.png");
            game.HandleInteraction(true, true, .01f);
            Check(!game.Tables[5].dirty && game.SpawnParty(5, 4), "ten presses clean table, four new guests can reserve");
            Check(game.Guests.Select(c => c.seat).Distinct().Count() == 4, "four-person party uses all chairs");
            game.StartGame(); game.SpawnParty(0, 1); game.DebugTickGuests(60);
            game.DebugRating(3); var impatient = game.Tables[0]; impatient.patience = .01f; game.DebugTickGuests(.1f);
            Check(impatient.service == TableServiceState.Departing && game.Failed == 1 && Mathf.Abs(game.Rating - 2.75f) < .001f, "late order loses five percent per table");
            game.StartGame(); game.SpawnParty(0, 4); game.DebugTickGuests(60);
            impatient = game.Tables[0]; game.Accept(impatient); game.DebugRating(1.25f); impatient.patience = .01f; game.DebugTickGuests(.1f);
            Check(game.State == ScreenState.GameOver && game.Rating == 1, "late delivery triggers game over at one star");
            yield return Capture("06-game-over.png");
            game.StartGame(); Check(game.State == ScreenState.Playing && game.Rating == 5 && game.Failed == 0 && game.Interface.Receipts.Count == 0, "restart resets the session");
            for (int partySize = 1; partySize <= 4; partySize++)
            {
                game.StartGame(); game.SpawnParty(0, partySize); game.DebugTickGuests(60); game.DebugRating(4);
                var table = game.Tables[0]; game.Accept(table); game.Accept(table);
                Check(game.Interface.Receipts.Count == 1 && game.Guests.Count == partySize, partySize + " guests share exactly one order");
                Check(table.itemOrder.Quantity == partySize, partySize + " guests order exactly one item each");
                ServeAll(table); game.Resolve(table, true);
                Check(game.Completed == 1 && Mathf.Abs(game.Rating - 4.15f) < .001f && game.Guests.All(c => c.state == GuestState.Leaving), partySize + " guests resolve once and all leave");
                game.StartGame(); game.SpawnParty(0, partySize); game.DebugTickGuests(60);
                game.Tables[0].patience = .01f; game.DebugTickGuests(.1f);
                Check(game.Failed == 1 && Mathf.Abs(game.Rating - 4.75f) < .001f && game.Guests.All(c => c.state == GuestState.Leaving), partySize + " guests time out with only one penalty");
            }
            yield return VerifyGuestWaiting();
            yield return VerifyNoCleanTable();
            game.DebugPosition(new Vector2(1, 1)); yield return null;
            Check(game.WorldCamera.transform.position.x >= SaunaMap.ViewHalf - .001f && game.WorldCamera.transform.position.y >= SaunaMap.ViewHalf - .001f, "camera clamps at lower map edge");
            game.DebugPosition(game.Map.bounds.center); yield return null;
            Check(Vector2.Distance(game.WorldCamera.transform.position, game.Map.bounds.center) < .001f, "camera follows again away from edge");
            Check(Mathf.Abs(game.WorldCamera.pixelWidth - game.WorldCamera.pixelHeight) <= 1, "rendered viewport is square");
            game.MainMenu(); game.OpenSettings(); game.SetUiScale(2); Check(game.UiScale == 2, "menu permits 200 percent UI");
            game.SetUiScale(3); Check(game.UiScale == 3, "menu permits 300 percent UI");
            yield return Capture("07-settings-300.png");
            game.CloseSettings(); game.StartGame();
            game.SpawnParty(4, 3); game.SpawnParty(5, 4); game.SpawnParty(6, 2); game.DebugTickGuests(50);
            game.DebugPosition(game.Map.tables[5] + Vector2.down * 2.1f);
            foreach (var table in game.Tables.Where(t => t.service == TableServiceState.Waiting)) game.Accept(table);
            yield return new WaitForSecondsRealtime(.6f);
            yield return Capture("08-gameplay-300.png");
            Check(game.Interface.Receipts.All(r => r.view.rect.rect.width * r.view.rect.localScale.x <= game.Interface.receiptsScroll.viewport.rect.width), "300 percent HUD keeps receipt timers inside rail");
            AtCooking(); game.HandleInteraction(true,true,.016f); yield return null;
            yield return Capture("14-cooking-300.png"); game.CloseCooking();
            game.MainMenu(); game.StartGame(); game.SpawnParty(0,1); game.SpawnParty(1,1); game.DebugTickGuests(60);
            var source = game.Tables[0]; var destination = game.Tables[1]; game.Accept(source); game.Accept(destination);
            destination.itemOrder = new TableItemOrder(); destination.itemOrder.Add(source.itemOrder.lines[0].item);
            AtCooking(); game.HandleInteraction(true,true,.016f); Pick(source.itemOrder.lines[0].item); game.CloseCooking();
            game.DebugPosition(game.Map.tables[1] + Vector2.down * 2); game.HandleInteraction(true,true,.016f);
            Check(destination.service == TableServiceState.Departing && source.service == TableServiceState.Ordered && game.Model.inventory.Count == 0, "matching food can be delivered to another table requesting it");
            game.MainMenu(); game.SetUiScale(1); game.StartGame();
            game.SpawnParty(4, 3); game.SpawnParty(5, 4); game.SpawnParty(6, 2); game.DebugTickGuests(50);
            foreach (var table in game.Tables.Where(t => t.service == TableServiceState.Waiting)) game.Accept(table);
            game.Tables[4].patience = 70; game.Tables[5].patience = 45; game.Tables[6].patience = 20;
            game.DebugPosition(game.Map.tables[5] + Vector2.down * 2);
            yield return new WaitForSecondsRealtime(.6f);
            Check(game.Interface.Receipts[0].view.patience.color == SaunaReceiptView.TimerColor(.7f) && game.Interface.Receipts[1].view.patience.color == SaunaReceiptView.TimerColor(.45f) && game.Interface.Receipts[2].view.patience.color == SaunaReceiptView.TimerColor(.2f), "stacked receipts show green orange red radial countdowns");
            yield return Capture("12-stacked-receipts.png");
            var clicked = game.Interface.Receipts[2]; clicked.view.Clicked(); yield return new WaitForSecondsRealtime(.4f);
            Check(game.Interface.Receipts[0] == clicked, "click exposed receipt brings its order to front");
            yield return Capture("13-selected-receipt.png");
            game.MainMenu(); game.StartGame();
            foreach (var quest in game.quests.numbers.Concat(game.quests.letters)) quest.kind = QuestKind.Placeholder;
            game.SpawnParty(0, 2); game.DebugTickGuests(60); game.DebugPosition(game.Map.tables[0] + Vector2.down * 2);
            game.HandleInteraction(true,true,.01f); game.HandleInteraction(true,true,.01f);
            Check(game.Completed == 1 && game.Guests.All(c => c.state == GuestState.Leaving), "placeholder special quest still resolves with second F");
            foreach (var quest in game.quests.numbers.Concat(game.quests.letters)) quest.kind = QuestKind.FoodOrder;
            game.MainMenu(); game.SetUiScale(1);
            Screen.SetResolution(720, 900, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.5f);
            game.StartGame(); game.SpawnParty(5, 1); game.DebugTickGuests(50);
            game.DebugPosition(game.Map.tables[5] + Vector2.down * 2.1f); game.Accept(game.Tables[5]);
            yield return new WaitForSecondsRealtime(.6f);
            Check(Mathf.Abs(game.WorldCamera.pixelWidth - game.WorldCamera.pixelHeight) <= 1, "portrait window retains square viewport");
            var strip = game.Interface.receiptsScroll;
            Check(strip.horizontal && !strip.vertical && strip.viewport.rect.height >= 35 && game.Interface.Receipts[0].view.compact.activeInHierarchy,
                "portrait order strip fits the compact countdown and scrolls horizontally");
            yield return Capture("09-portrait.png");
            game.MainMenu(); game.SetUiScale(previousScale); Screen.SetResolution(1440, 900, FullScreenMode.Windowed);
            File.WriteAllLines(Path.Combine(output, "runtime-checks.txt"), checks);
            Debug.Log("SAUNA_SMOKE_" + (failed ? "FAILED" : "OK") + " " + checks.Count + " checks");
            Application.Quit(failed ? 1 : 0);
        }
        void AtCooking() => game.DebugPosition(game.Map.stations[0] + Vector2.down * 2.1f);
        void CheckRuntimeLog(string message, string trace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            failed = true; checks.Add("FAIL runtime " + type + ": " + message);
        }
        void OnDestroy() => Application.logMessageReceived -= CheckRuntimeLog;
        void AtWaste() => game.DebugPosition((Vector2)game.WorldView.layout.foodWasteBin.position + Vector2.down * 1.2f);
        void Pick(SaunaItemDefinition item) => game.Interface.cooking.foods.First(f => f.item == item).button.onClick.Invoke();
        void ServeAll(SaunaTable table)
        {
            int guard = 0;
            while (table.service == TableServiceState.Ordered && table.itemOrder.Remaining > 0 && guard++ < 5)
            {
                AtCooking(); game.HandleInteraction(true, true, .016f);
                var remaining = table.itemOrder.lines.SelectMany(l => Enumerable.Repeat(l.item, l.Remaining)).Take(2).ToArray();
                foreach (var item in remaining) Pick(item);
                game.CloseCooking(); game.DebugPosition(game.Map.tables[table.index] + Vector2.down * 2);
                game.HandleInteraction(true, true, .016f);
            }
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot != null)
            {
                File.WriteAllBytes(Path.Combine(output, name), screenshot.EncodeToPNG());
                Destroy(screenshot);
            }
            else Check(false, "screenshot " + name);
            yield return new WaitForSecondsRealtime(.18f);
        }
    }
}
#endif

