using System.Collections.Generic;
using UnityEngine;

namespace SuspiciousSauna
{
    public enum ScreenState { Menu, Playing, Paused, Settings, GameOver, Cooking, RetailPicking }
    public enum GuestState { Entering, Seated, Leaving, NoSeatApproach }
    public enum TableServiceState { Empty, Arriving, Waiting, Ordered, Departing }

    // Guests are presentation actors. Orders, timers, and outcomes belong to the table.
    public sealed class Guest
    {
        public int id, table, seat, pathIndex;
        public GuestState state;
        public Vector2 position;
        public List<Vector2> path;
        public bool failedDeparture;
        public float departureReaction;
    }

    public sealed class SaunaTable
    {
        public int index;
        public bool dirty;
        public int cleaningMode = -1, presses;
        public float held;
        public TableServiceState service;
        public QuestEntry quest;
        public int partySize;
        public TableFoodOrder foodOrder = new TableFoodOrder();
        public bool IsFoodOrder => quest != null && quest.IsFoodOrder;
        public float patience, totalPatience;
        public bool failed;

        public void ResetCleaning() { cleaningMode = -1; held = 0; presses = 0; }
        public void Reset()
        {
            dirty = false; service = TableServiceState.Empty; quest = null;
            partySize = 0; foodOrder = new TableFoodOrder();
            patience = totalPatience = 0; failed = false; ResetCleaning();
        }
    }

    // No GameObjects, rendering, input, audio, or persistence in the session model.
    public sealed class SaunaSessionModel
    {
        public ScreenState screen = ScreenState.Menu, settingsReturn;
        public readonly List<Guest> guests = new List<Guest>();
        public readonly List<SaunaTable> tables = new List<SaunaTable>();
        public Vector2 playerPosition = SaunaMap.Start;
        public float rating = 5, playedSeconds;
        public int completed, failed;
        public bool hasCleaningTool;
        public readonly HandInventory inventory = new HandInventory();
        public readonly HandInventory retailBag = new HandInventory(5);
        public readonly List<RetailCustomer> retailCustomers = new List<RetailCustomer>();
        public readonly List<RetailCustomer> retailQueue = new List<RetailCustomer>();

        public SaunaSessionModel() => ConfigureTables(SaunaMap.TableCount);
        public void ConfigureTables(int count)
        {
            tables.Clear();
            for (int i = 0; i < count; i++) tables.Add(new SaunaTable { index = i });
        }
        public void Reset(SaunaTuning tuning)
        {
            guests.Clear();
            foreach (var table in tables) table.Reset();
            rating = tuning.initialRating; playedSeconds = 0; completed = failed = 0;
            hasCleaningTool = false; playerPosition = SaunaMap.Start;
            inventory.Clear();
            retailBag.Clear(); retailQueue.Clear(); retailCustomers.Clear();
        }
    }
}
