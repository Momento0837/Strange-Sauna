#if UNITY_STANDALONE_WIN || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SuspiciousSauna
{
    public sealed class SaunaRetailSmoke : MonoBehaviour
    {
        SaunaGame game;
        readonly List<string> checks = new List<string>();
        string output; bool failed;
        void Check(bool value, string name)
        { checks.Add((value ? "PASS " : "FAIL ") + name); if (!value) { failed = true; Debug.LogError("RETAIL FAILED: " + name); } }
        IEnumerator Start()
        {
            game = GetComponent<SaunaGame>();
            Application.logMessageReceived += CheckRuntimeLog;
            string[] args = Environment.GetCommandLineArgs(); int arg = Array.IndexOf(args, "--sauna-output");
            output = arg >= 0 ? args[arg + 1] : Path.Combine(Application.persistentDataPath, "RetailQA"); Directory.CreateDirectory(output);
            Application.runInBackground = true;
            game.SessionStarted += () => game.DebugDelayArrivals(10000);
            yield return null;
            int previousScale = game.UiScale;
            game.SetUiScale(1); game.StartGame();
            Check(game.items.retailMenu.Length == 9 && game.items.retailMenu.All(i => i.icon != null && i.category == ItemCategory.Hygiene), "nine editable retail items have icons and categories");
            Check(game.retailLayout.queueSpots.Length == 3 && game.WorldView.retailGuestPrefab != null && game.Interface.retailPicker != null, "authored POS queue shelf and native picker wired");
            Check(game.Model.retailBag.Capacity == 5 && game.Model.inventory.Capacity == 2, "retail five-slot bag preserves two food hands");
            var first = game.SpawnRetailCustomer(); var second = game.SpawnRetailCustomer(); var third = game.SpawnRetailCustomer();
            Check(game.Model.retailQueue.Count == 3, "capacity includes the customer at the register");
            Check(game.Model.retailCustomers.All(c => c.path.Count > 0 && c.path.All(p => game.Navigation.Walkable(p))), "retail arrival paths avoid tables and counter obstacles");
            game.DebugTickRetail(30);
            Check(game.Model.retailQueue.All(c => c.state == RetailCustomerState.Waiting) && Vector2.Distance(second.position, third.position) > 1, "three customers occupy distinct queue anchors");
            game.DebugPosition(game.retailLayout.staffPoint.position);
            yield return null; yield return Capture("01-three-person-queue.png");
            game.DebugPosition(first.position); game.InteractRetailRegister();
            Check(game.ActiveRetailOrder == null, "customer side cannot accept an order through the counter");
            game.DebugPosition((Vector2)game.retailLayout.staffPoint.position + Vector2.up * 1.1f);
            Check(game.TargetStation == 1 && game.Navigation.Walkable(game.PlayerPosition), "entire authored staff interaction radius targets POS");
            AtRegister(); game.Model.inventory.TryAdd(game.items.cookingMenu[0]); Interact();
            Check(game.ActiveRetailOrder == null && game.Toast == "음식을 서빙 중입니다!", "food blocks POS with requested narration");
            game.Model.inventory.Clear(); game.DebugTool(true); Interact();
            Check(game.ActiveRetailOrder == null && game.Toast == "청소 중입니다!", "cleaning tool blocks POS with requested narration");
            game.DebugTool(false); game.DebugRating(4); Interact();
            var order = game.ActiveRetailOrder;
            Check(order != null && order.requested.Count >= 1 && order.requested.Count <= 5 && order.requested.All(game.items.retailMenu.Contains), "POS accepts one random one-to-five-unit order");
            Check(Mathf.Approximately(order.totalPatience, 50), "retail deadline is half the default food deadline");
            var a = game.items.retailMenu[1]; var b = game.items.retailMenu[2]; var c = game.items.retailMenu[3]; var d = game.items.retailMenu[4];
            order.requested.Clear(); order.requested.AddRange(new[] { a, a, b, c, d }); order.SampleReceipt();
            Check(order.receiptItems.Count == 3 && order.requested.Count == 5, "receipt hides exactly two of five requested units without changing true order");
            var sampled = order.receiptItems.ToArray();
            for (int i = 0; i < 3; i++) game.Interface.Receipts.First(r => r.retailOrder == order).view.Present(order, false, false);
            Check(order.receiptItems.SequenceEqual(sampled), "receipt sample remains stable across redraws");
            Check(ModelOrderCount() == 1 && second.order == null && third.order == null, "only queue front has an accepted retail order");
            yield return new WaitForSecondsRealtime(.6f); yield return Capture("02-icons-and-limited-receipt.png");
            game.DebugPosition(game.Map.stations[0] + Vector2.down * 2); Interact();
            Check(game.State == ScreenState.Playing && game.Toast == "카운터에 기다리는 손님이 있어요!", "active retail order blocks cooking interaction");
            game.DebugPosition(game.Map.stations[3] + Vector2.down * 1.5f); Interact();
            Check(!game.HasCleaningTool && game.Toast == "카운터에 기다리는 손님이 있어요!", "active retail order blocks taking cleaning tools");
            AtShelf(); Interact();
            Check(game.State == ScreenState.RetailPicking, "F opens separate retail selection window at shelf");
            float time = order.patience; game.DebugTickRetail(.5f);
            Check(order.patience < time, "retail timer continues while selecting goods");
            yield return Capture("03-retail-selection.png");
            foreach (var item in order.requested) Pick(item);
            Check(game.Model.retailBag.Count == 5 && !game.PickRetailItem(a), "five goods fit and sixth is rejected");
            game.RemoveRetailItem(4); Check(game.Model.retailBag.Count == 4, "selected goods can be returned to the shelf"); Pick(d);
            Check(order.Evaluate(game.Model.retailBag).Success, "duplicate quantities and exact five-item delivery match");
            game.Pause(); float paused = order.patience;
            yield return new WaitForSecondsRealtime(.2f);
            Check(order.patience == paused, "pause freezes retail deadline");
            game.OpenSettings(); game.SetUiScale(3); Check(game.UiScale == 1, "UI scale remains locked during retail service"); game.CloseSettings(); game.Resume();
            yield return Capture("04-five-items-held.png");
            AtRegister(); Interact();
            Check(order.resolved && order.success && first.happy && first.state == RetailCustomerState.Leaving && game.Model.retailBag.Count == 0, "exact delivery consumes all goods and starts happy departure");
            Check(Mathf.Abs(game.Rating - 4.15f) < .001f && game.Completed == 1, "successful retail customer grants one normal success reward");
            Check(game.Model.retailQueue.Count == 2 && game.RetailFront == second && second.destination == (Vector2)game.retailLayout.queueSpots[0].position, "remaining queue advances after service");
            Check(first.path.All(p => game.Navigation.Walkable(p)) && first.path.Last() == game.Map.entrance, "happy departure follows obstacle-free exit path");
            yield return null; yield return Capture("05-happy-departure.png");
            game.DebugTickRetail(3); AtRegister(); Interact();
            var bad = game.ActiveRetailOrder;
            Check(bad != null && bad != order, "next order can be accepted only after previous customer leaves service");
            bad.requested.Clear(); bad.requested.AddRange(new[] { a, a, b }); bad.SampleReceipt();
            AtShelf(); Interact(); Pick(a); Pick(b); Pick(b); Pick(c); game.CloseRetailPicking();
            var mistakes = bad.Evaluate(game.Model.retailBag);
            Check(mistakes.missing == 1 && mistakes.excess == 1 && mistakes.wrong == 1 && mistakes.Errors == 3, "delivery distinguishes missing duplicate excess and wrong product units");
            game.DebugRating(4); AtRegister(); Interact();
            Check(Mathf.Abs(game.Rating - 3.775f) < .001f && !second.happy && game.Model.retailBag.Count == 0, "each of three mismatches costs normal penalty times point-three and all goods are consumed");
            yield return null; yield return Capture("06-unsuccessful-departure.png");
            game.StartGame(); game.DebugRating(4);
            for (int i = 0; i < 3; i++) game.SpawnRetailCustomer();
            var overflow = game.SpawnRetailCustomer();
            Check(game.Model.retailQueue.Count == 3 && overflow.state == RetailCustomerState.OverflowApproach && !game.Model.retailQueue.Contains(overflow), "fourth arrival observes full queue without reserving a slot");
            game.DebugTickRetail(30); game.DebugPosition(overflow.position + Vector2.down * 2);
            Check(overflow.state == RetailCustomerState.Grumbling && Vector2.Distance(overflow.position, game.retailLayout.overflowLookPoint.position) < .01f && Mathf.Abs(game.Rating - 3.875f) < .001f, "overflow customer grumbles at map center and costs half normal penalty");
            yield return Capture("07-full-queue-grumble.png");
            game.DebugTickRetail(1.6f); Check(overflow.state == RetailCustomerState.Leaving, "overflow customer returns to entrance after grumbling");
            game.DebugTickRetail(30); Check(!game.Model.retailCustomers.Contains(overflow), "overflow actor despawns at original entrance");
            game.StartGame(); var late = game.SpawnRetailCustomer(); game.DebugTickRetail(30); AtRegister(); Interact();
            var lateOrder = game.ActiveRetailOrder; AtShelf(); Interact(); Pick(a); game.DebugRating(4); lateOrder.patience = .01f; game.DebugTickRetail(.02f);
            Check(lateOrder.resolved && !lateOrder.success && game.ActiveRetailOrder == null && game.Model.retailBag.Count == 0 && game.State == ScreenState.Playing, "timeout while picker open dismisses picker and clears order and bag");
            Check(Mathf.Abs(game.Rating - 3.75f) < .001f && !late.happy, "retail delivery timeout uses normal failure penalty and quiet departure");
            game.StartGame(); var unserved = game.SpawnRetailCustomer(); game.DebugTickRetail(30); unserved.waitingSeconds = .01f; game.DebugRating(1.25f); game.DebugTickRetail(.02f);
            Check(game.State == ScreenState.GameOver && game.Rating == 1 && game.Model.retailQueue.Count == 0, "unaccepted front customer can time out and trigger game over");
            game.StartGame(); Check(game.Model.retailCustomers.Count == 0 && game.Model.retailQueue.Count == 0 && game.Model.retailBag.Count == 0 && game.Interface.Receipts.Count == 0, "restart clears retail actors queue bag and receipts");
            game.SpawnParty(5, 1); game.DebugTickGuests(50); game.Accept(game.Tables[5]);
            game.SpawnRetailCustomer(); game.DebugTickRetail(30); AtRegister(); Interact();
            Check(game.Interface.Receipts.Count == 2 && game.Interface.Receipts.Count(r => r.retailOrder != null) == 1 && game.Interface.Receipts.Count(r => r.table != null) == 1, "food and retail receipts coexist in one animated stack");
            game.MainMenu(); game.SetUiScale(3); game.StartGame(); game.SpawnRetailCustomer(); game.DebugTickRetail(30); AtRegister(); Interact(); AtShelf(); Interact();
            yield return null; yield return Capture("08-retail-picker-300.png");
            game.MainMenu(); game.SetUiScale(previousScale);
            File.WriteAllLines(Path.Combine(output, "runtime-checks.txt"), checks);
            Debug.Log("SAUNA_RETAIL_" + (failed ? "FAILED" : "OK") + " " + checks.Count + " checks"); Application.Quit(failed ? 1 : 0);
        }
        int ModelOrderCount() => game.Model.retailCustomers.Count(c => c.order != null && !c.order.resolved);
        void CheckRuntimeLog(string message, string trace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            failed = true; checks.Add("FAIL runtime " + type + ": " + message);
        }
        void OnDestroy() => Application.logMessageReceived -= CheckRuntimeLog;
        void AtRegister() => game.DebugPosition(game.retailLayout.staffPoint.position);
        void AtShelf() => game.DebugPosition(game.retailLayout.shelfInteraction.position);
        void Interact() { game.RefreshTarget(); game.HandleInteraction(true, true, .016f); }
        void Pick(SaunaItemDefinition item) => game.Interface.retailPicker.choices.First(c => c.item == item).button.onClick.Invoke();
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture != null) { File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG()); Destroy(texture); }
            else Check(false, "screenshot " + name);
            yield return new WaitForSecondsRealtime(.18f);
        }
    }
}
#endif
