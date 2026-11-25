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
            var hint = game.Interface.interactionHint;
            var hintCanvas = hint.GetComponentInParent<Canvas>();
            var customerView = game.WorldView.retailGuestContainer.GetComponentsInChildren<SaunaRetailCustomerView>().Single(v => v.orderContents.activeSelf);
            var speechCanvas = customerView.bubble.GetComponentInParent<Canvas>();
            Check(hint.gameObject.activeInHierarchy && hintCanvas.renderMode == RenderMode.WorldSpace && hintCanvas.worldCamera == game.WorldCamera
                && speechCanvas.renderMode == RenderMode.WorldSpace && hintCanvas.sortingLayerID == speechCanvas.sortingLayerID && hintCanvas.sortingOrder < speechCanvas.sortingOrder,
                "register hint renders behind the visible order speech bubble");
            Check(Mathf.Approximately(game.Interface.canvas.scaleFactor, Mathf.Min(Screen.width, Screen.height) / 800f), "HUD scales with the shorter screen dimension");
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
            yield return VerifySeatedRetailOrder();
            game.MainMenu(); game.SetUiScale(previousScale);
            File.WriteAllLines(Path.Combine(output, "runtime-checks.txt"), checks);
            Debug.Log("SAUNA_RETAIL_" + (failed ? "FAILED" : "OK") + " " + checks.Count + " checks"); Application.Quit(failed ? 1 : 0);
        }
        IEnumerator VerifySeatedRetailOrder()
        {
            game.MainMenu(); game.SetUiScale(1); game.StartGame(); game.DebugRating(4);
            var numbers = game.quests.numbers;
            var letters = game.quests.letters;
            var entry = letters.Single(q => q.id == "a");
            game.quests.numbers = game.quests.letters = new[] { entry };
            var menu = game.items.retailMenu;
            var table = game.Tables[5];
            void AtTable() => game.DebugPosition(game.Map.tables[table.index] + Vector2.down * 2);
            void Seat(int partySize) { game.SpawnParty(table.index, partySize); game.DebugTickGuests(50); AtTable(); Interact(); }
            SaunaItemDefinition[] Requested() => table.itemOrder.lines.SelectMany(l => Enumerable.Repeat(l.item, l.Remaining)).ToArray();

            var splits = new HashSet<string>();
            bool allGuessesMatched = true, checkedHiddenOnly = false;
            var randomState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(941);
            for (int i = 0; i < 64; i++)
            {
                var planState = UnityEngine.Random.state;
                var sample = TableItemOrder.CreateRetail(game.items);
                var announced = sample.lines.SelectMany(l => Enumerable.Repeat(l.item, l.quantity)).ToList();
                foreach (var line in sample.lines) line.delivered = line.quantity;
                if (!sample.RevealNextRequest()) { allGuessesMatched = false; continue; }
                splits.Add(announced.Count + "+" + sample.Quantity);
                var hidden = sample.lines.SelectMany(l => Enumerable.Repeat(l.item, l.quantity)).ToList();
                UnityEngine.Random.state = planState;
                var replay = TableItemOrder.CreateRetail(game.items);
                var bag = new HandInventory(5);
                var unannounced = hidden.FirstOrDefault(item => !announced.Contains(item));
                if (!checkedHiddenOnly && unannounced != null)
                {
                    bag.TryAdd(unannounced);
                    var early = replay.DeliverRetail(bag);
                    Check(early.matched == 1 && early.Incorrect == 0 && replay.Remaining == announced.Count && !replay.RevealNextRequest(),
                        "a hidden-only guess is credited while the outstanding first request stays on the receipt");
                    hidden.Remove(unannounced); checkedHiddenOnly = true;
                }
                foreach (var item in hidden.Concat(announced)) bag.TryAdd(item);
                var result = replay.DeliverRetail(bag);
                allGuessesMatched &= result.Success && replay.Complete && !replay.RevealNextRequest() && bag.Count == 0;
            }
            UnityEngine.Random.state = randomState;
            Check(splits.SetEquals(new[] { "2+2", "2+3", "3+2" }), "catalog plans only 4 or 5 units in two batches of 2 or 3, with all three splits represented");
            Check(allGuessesMatched && checkedHiddenOnly, "exact guesses complete every 4- and 5-unit split without a follow-up prompt");

            var beforeSeating = UnityEngine.Random.state;
            Seat(3);
            var first = Requested();
            Check(table.IsTableRetailOrder && table.quest == entry && first.Length >= 2 && first.Length <= 3 &&
                first.All(menu.Contains) && !table.itemOrder.IsAdditionalRequest, "seated a party reveals only its first merchandise request");
            yield return new WaitForSecondsRealtime(.7f);
            var ticket = game.Interface.Receipts.Single(r => r.table == table).view;
            Check(ticket.code.text == "#a" && ticket.title.text == "잡화 주문" && ticket.description.text.Split('\n').Length == table.itemOrder.lines.Count
                && first.All(item => ticket.description.text.Contains(item.displayName)), "first receipt lists exactly the disclosed lines");
            yield return Capture("09-seated-a-order.png");
            Interact();
            Check(table.service == TableServiceState.Ordered && game.Completed == 0 && Requested().Length == first.Length, "empty interaction leaves first request pending");

            game.SpawnRetailCustomer(); game.DebugTickRetail(30); AtRegister(); Interact();
            var register = game.ActiveRetailOrder;
            AtShelf(); Interact(); Pick(first[0]); game.CloseRetailPicking(); AtTable(); Interact();
            Check(game.Model.retailBag.Count == 1 && Requested().Length == first.Length, "POS bag cannot be submitted at a table");
            AtShelf(); Interact(); game.RemoveRetailItem(0);
            foreach (var item in register.requested) Pick(item);
            game.CloseRetailPicking(); AtRegister(); Interact();
            Check(register.success && table.service == TableServiceState.Ordered && game.Model.retailBag.Count == 0, "register completion preserves the table ticket");

            AtShelf(); Interact();
            int spareSlots = 5 - first.Length;
            for (int i = 0; i < spareSlots; i++) Pick(menu[i]);
            foreach (var item in first) Pick(item);
            yield return null;
            var picker = game.Interface.retailPicker;
            Check(game.Model.retailBag.Full && game.Model.retailBag.Count == 5 && game.Model.inventory.Count == 0 &&
                picker.bagButtons.Count(button => button.gameObject.activeSelf) == 5 && picker.capacity.text.Contains("테이블 잡화 5 / 5") &&
                !game.PickRetailItem(menu[0]), "table merchandise uses all five bag slots and rejects a sixth unit");
            yield return Capture("10-five-slot-table-bag.png");
            for (int slot = 0; slot < spareSlots; slot++) game.RemoveRetailItem(slot);
            game.CloseRetailPicking();
            game.SpawnRetailCustomer(); game.DebugTickRetail(30); AtRegister(); Interact();
            Check(game.ActiveRetailOrder == null && game.Model.retailBag.Count == first.Length, "register cannot claim stock already carried for a table");

            AtTable(); float deadline = table.patience, rating = game.Rating;
            game.items.retailMenu = new[] { game.items.cookingMenu[0] };
            Interact();
            game.items.retailMenu = menu;
            var second = Requested();
            Check(table.itemOrder.IsAdditionalRequest && second.All(menu.Contains) && second.Length >= 2 && second.Length <= 3 &&
                first.Length + second.Length >= 4 && first.Length + second.Length <= 5, "follow-up is drawn from the original menu before the first trip");
            Check(game.Model.retailBag.Count == 0 && table.service == TableServiceState.Ordered && !table.dirty &&
                game.Completed == 1 && game.Rating == rating && table.patience == deadline, "first batch in the last bag slots reveals the next request on the same timer without resolving the table");
            yield return new WaitForSecondsRealtime(.6f);
            Check(ticket.title.text == "추가 잡화 주문" && second.All(item => ticket.description.text.Contains(item.displayName)),
                "same table receipt now shows the second batch");
            yield return Capture("11-additional-order.png");
            Interact();
            Check(Requested().Length == second.Length, "pressing F again without a second shelf trip delivers nothing");
            AtShelf(); Interact(); foreach (var item in second) Pick(item);
            game.CloseRetailPicking(); AtTable(); Interact(); Interact();
            Check(table.itemOrder.Complete && table.dirty && table.service == TableServiceState.Departing && game.Completed == 2 &&
                Mathf.Abs(game.Rating - (rating + .15f)) < .001f && game.Model.retailBag.Count == 0 &&
                game.Guests.All(g => g.state == GuestState.Leaving), "second trip completes the table and grants exactly one perfect-order reward");

            // Replay that same order, this time packing the unannounced items on the first trip.
            game.StartGame(); game.DebugRating(4); UnityEngine.Random.state = beforeSeating; Seat(3);
            Check(first.SequenceEqual(Requested()) && !table.itemOrder.IsAdditionalRequest, "replayed first receipt still contains only the announced items");
            AtShelf(); Interact();
            foreach (var item in second.Concat(first)) Pick(item);
            game.CloseRetailPicking(); AtTable();
            yield return new WaitForSecondsRealtime(.55f); yield return Capture("13-first-trip-bag.png");
            Interact();
            Check(table.itemOrder.Complete && !table.itemOrder.IsAdditionalRequest && table.itemOrder.DeliveryMistakes == 0 &&
                game.Model.retailBag.Count == 0 && game.Completed == 1 && table.service == TableServiceState.Departing &&
                Mathf.Abs(game.Rating - 4.15f) < .001f, "guessing the complete hidden batch resolves the table and gives the perfect reward on the first trip");
            yield return new WaitForSecondsRealtime(.45f); yield return Capture("14-first-trip-complete.png");
            Interact();
            Check(game.Completed == 1 && Mathf.Abs(game.Rating - 4.15f) < .001f, "one-trip completion cannot pay twice");

            game.StartGame(); game.DebugRating(4);
            var product = menu[0]; var wrong = menu[1];
            game.items.retailMenu = new[] { product }; Seat(1); game.items.retailMenu = menu;
            int firstUnits = table.itemOrder.Quantity;
            AtShelf(); Interact(); Pick(product); game.CloseRetailPicking(); AtTable(); Interact();
            Check(!table.itemOrder.IsAdditionalRequest && table.itemOrder.Remaining == firstUnits - 1 && game.Rating == 4, "partial correct delivery neither reveals the next request nor penalizes missing units");
            AtShelf(); Interact(); for (int i = 0; i < firstUnits; i++) Pick(product);
            game.CloseRetailPicking(); AtTable(); Interact();
            int followUpLeft = table.itemOrder.Quantity - 1;
            Check(table.itemOrder.IsAdditionalRequest && table.itemOrder.Remaining == followUpLeft && game.Model.retailBag.Count == 0 &&
                table.itemOrder.DeliveryMistakes == 0 && game.Rating == 4, "duplicate units fill the announced batch first and credit one matching hidden unit without a penalty");
            yield return new WaitForSecondsRealtime(.45f); yield return Capture("15-partially-guessed-order.png");
            AtShelf(); Interact(); Pick(wrong); Pick(wrong); game.CloseRetailPicking(); AtTable(); Interact();
            float afterWrong = 4 - .15f;
            Check(table.itemOrder.Remaining == followUpLeft && table.itemOrder.DeliveryMistakes == 2 &&
                game.Model.retailBag.Count == 0 && Mathf.Abs(game.Rating - afterWrong) < .001f, "two wrong goods immediately cost two POS unit penalties");
            yield return new WaitForSecondsRealtime(.55f); yield return Capture("12-wrong-table-delivery.png");
            Interact();
            Check(Mathf.Abs(game.Rating - afterWrong) < .001f, "empty retry does not repeat a delivery penalty");
            AtShelf(); Interact(); foreach (var item in Requested()) Pick(item); Pick(product);
            game.CloseRetailPicking(); AtTable(); Interact();
            Check(game.Completed == 1 && table.itemOrder.Complete && table.itemOrder.DeliveryMistakes == 3 &&
                Mathf.Abs(game.Rating - (afterWrong - .075f)) < .001f, "only the unit above the combined order is excess and the corrected order earns no perfect bonus");

            game.StartGame(); Seat(4); AtShelf(); Interact(); Pick(wrong); game.CloseRetailPicking();
            table.patience = .01f; game.DebugTickGuests(.02f); AtShelf(); Interact();
            Check(game.Failed == 1 && table.failed && game.State == ScreenState.RetailPicking && !game.PickRetailItem(wrong), "expired table order permits returning carried stock without new pickups");
            game.RemoveRetailItem(0); game.CloseRetailPicking(); AtShelf(); Interact();
            Check(game.State == ScreenState.Playing && game.Model.retailBag.Count == 0, "shelf closes normally after returning a timed-out order");
            game.StartGame(); game.items.retailMenu = new[] { product }; Seat(1); game.items.retailMenu = menu;
            AtShelf(); Interact(); Pick(product); Pick(product); game.CloseRetailPicking();
            var neighbor = game.Tables[0];
            game.SpawnParty(neighbor.index, 2); game.DebugTickGuests(50);
            game.DebugPosition(game.Map.tables[neighbor.index] + Vector2.down * 2);
            game.items.retailMenu = new[] { product }; Interact(); game.items.retailMenu = menu;
            Interact();
            Check(game.Model.retailBag.Count == 0 && neighbor.itemOrder.DeliveryMistakes == 0 &&
                (neighbor.itemOrder.IsAdditionalRequest || neighbor.itemOrder.Remaining == neighbor.itemOrder.Quantity - 2) &&
                table.itemOrder.Remaining == table.itemOrder.Quantity && game.Rating == 5,
                "already carried matching table stock counts at the receiving table");
            game.StartGame(); game.items.retailMenu = new[] { product }; Seat(1); game.items.retailMenu = menu;
            game.DebugRating(1.05f);
            AtShelf(); Interact(); Pick(wrong); game.CloseRetailPicking(); AtTable(); Interact();
            Check(game.State == ScreenState.GameOver && game.Rating == 1, "seated retail mistake can trigger game over at the rating floor");
            for (int partySize = 1; partySize <= 4; partySize++)
            {
                game.StartGame(); Seat(partySize);
                var sharedTicket = game.Interface.Receipts.Single();
                int total = 0;
                for (int trip = 0; trip < 2; trip++)
                {
                    var batch = Requested(); total += batch.Length;
                    AtShelf(); Interact(); foreach (var item in batch) Pick(item);
                    game.CloseRetailPicking(); AtTable(); Interact();
                }
                Check((total == 4 || total == 5) && table.partySize == partySize && sharedTicket.table == table &&
                    game.Interface.Receipts.Single() == sharedTicket && game.Completed == 1 && game.Failed == 0 &&
                    game.Guests.Count == partySize && game.Guests.All(g => g.state == GuestState.Leaving),
                    partySize + " seated guests share one a quest, one receipt, 4-5 units total and one completion");
            }
            game.quests.numbers = numbers; game.quests.letters = letters;
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
