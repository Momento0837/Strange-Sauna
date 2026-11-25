using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuspiciousSauna
{
    // Controller: keyboard/menu commands advance the model and publish presentation events.
    public sealed partial class SaunaGame : MonoBehaviour
    {
        public QuestCatalog quests;
        public SaunaTuning tuning;
        public Material spriteMaterial;
        public SaunaSessionModel Model { get; } = new SaunaSessionModel();
        public ScreenState State { get => Model.screen; private set => Model.screen = value; }
        public ScreenState SettingsReturn { get => Model.settingsReturn; private set => Model.settingsReturn = value; }
        public List<Guest> Guests => Model.guests;
        public List<SaunaTable> Tables => Model.tables;
        public Vector2 PlayerPosition { get => Model.playerPosition; private set => Model.playerPosition = value; }
        public float Rating { get => Model.rating; private set => Model.rating = value; }
        public int Completed { get => Model.completed; private set => Model.completed = value; }
        public int Failed { get => Model.failed; private set => Model.failed = value; }
        public bool HasCleaningTool { get => Model.hasCleaningTool; private set => Model.hasCleaningTool = value; }
        public float PlayedSeconds { get => Model.playedSeconds; private set => Model.playedSeconds = value; }
        public string Toast { get; private set; }
        public float ToastTime { get; private set; }
        public SaunaTable TargetOrderTable { get; private set; }
        public SaunaTable TargetTable { get; private set; }
        public int TargetStation { get; private set; } = -1;
        public int UiScale { get; private set; } = 1;
        public Camera WorldCamera => WorldView.Camera;
        [SerializeField] SaunaWorldView worldView;
        [SerializeField] SaunaUI userInterface;
        [SerializeField] SaunaAudio sound;
        public SaunaWorldView WorldView => worldView;
        public SaunaUI Interface => userInterface;
        public SaunaMapData Map { get; private set; }
        public Rect PlayRect => SaunaMap.PlayRect(Screen.width, Screen.height);
        public SaunaAudio Sound => sound;
        public SaunaNavigation Navigation { get; private set; }
        public bool InSession => State == ScreenState.Playing || State == ScreenState.Cooking || State == ScreenState.RetailPicking || State == ScreenState.Paused || (State == ScreenState.Settings && SettingsReturn == ScreenState.Paused);
        float spawnTimer;
        int nextId;
        public event Action SessionStarted;
        public event Action<SaunaTable, QuestEntry> OrderAccepted;
        public event Action<SaunaTable, bool> OrderResolved;

        void Awake()
        {
            Application.targetFrameRate = 60;
            if (quests == null) { quests = Resources.Load<QuestCatalog>("QuestCatalog"); if (quests == null) { quests = ScriptableObject.CreateInstance<QuestCatalog>(); quests.FillDefaults(); } }
            if (tuning == null) tuning = Resources.Load<SaunaTuning>("SaunaTuning") ?? ScriptableObject.CreateInstance<SaunaTuning>();
            if (items == null) items = Resources.Load<SaunaItemCatalog>("SaunaItemCatalog");
            UiScale = Mathf.Clamp(PlayerPrefs.GetInt("sauna.uiScale", 1), 1, 3);
            if (worldView == null || userInterface == null || sound == null)
            {
                Debug.LogError("Sauna 씬의 Controller 참조를 연결하세요. 수상한 찜질방 > 씬 하이어라키 구성 메뉴를 사용할 수 있습니다.", this);
                enabled = false; return;
            }
            Map = worldView.layout.Capture();
            Model.ConfigureTables(Map.tables.Length);
            Navigation = new SaunaNavigation(Map);
            Model.playerPosition = Map.playerStart;
            WorldView.Initialize(Model, Map);
            Interface.Initialize(this);
            Rating = tuning.initialRating;
            PlayerPosition = Map.playerStart;
            SetState(ScreenState.Menu);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            if (Environment.GetCommandLineArgs().Contains("--sauna-smoke")) gameObject.AddComponent<SaunaSmoke>();
            if (Environment.GetCommandLineArgs().Contains("--sauna-retail-smoke")) gameObject.AddComponent<SaunaRetailSmoke>();
#endif
        }

        public void StartGame()
        {
            Model.Reset(tuning);
            PlayerPosition = Map.playerStart;
            nextId = 0; spawnTimer = 5;
            TargetOrderTable = null; TargetTable = null; TargetStation = -1;
            TargetWasteBin = wasteConfirmed = false;
            ResetRetail();
            WorldView.player.ClearReaction();
            SetState(ScreenState.Playing); Say("영업 시작!  손님이 오면 느낌표를 따라가세요.");
            SessionStarted?.Invoke(); WorldView.Sync();
        }
        public void MainMenu() { SetState(ScreenState.Menu); Say(""); }
        public void Pause() { if (State == ScreenState.Playing || State == ScreenState.Cooking || State == ScreenState.RetailPicking) SetState(ScreenState.Paused); }
        public void Resume() { if (State == ScreenState.Paused) SetState(ScreenState.Playing); }
        public void OpenSettings() { SettingsReturn = State; SetState(ScreenState.Settings); }
        public void CloseSettings() { PlayerPrefs.Save(); SetState(SettingsReturn); }
        public void SetUiScale(int scale)
        {
            if (InSession) return;
            UiScale = Mathf.Clamp(scale, 1, 3); PlayerPrefs.SetInt("sauna.uiScale", UiScale); PlayerPrefs.Save();
        }
        public void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void SetState(ScreenState state)
        {
            State = state; Time.timeScale = state == ScreenState.Playing || state == ScreenState.Cooking || state == ScreenState.RetailPicking ? 1 : 0;
            if (state != ScreenState.Playing) wasteConfirmed = false;
            WindowResizeLock.SetLocked(InSession);
            if (state != ScreenState.Playing && TargetTable != null) ResetCleaning(TargetTable);
        }
        void OnApplicationFocus(bool focused) { if (!focused) Pause(); else WindowResizeLock.SetLocked(InSession); }
        void OnDestroy() { Time.timeScale = 1; WindowResizeLock.SetLocked(false); }
        public void Say(string message) { Toast = message; ToastTime = string.IsNullOrEmpty(message) ? 0 : 4.5f; }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == ScreenState.Settings) CloseSettings();
                else if (State == ScreenState.Cooking) CloseCooking();
                else if (State == ScreenState.RetailPicking) CloseRetailPicking();
                else if (State == ScreenState.Playing) Pause();
                else if (State == ScreenState.Paused) Resume();
            }
            if (State != ScreenState.Playing && State != ScreenState.Cooking && State != ScreenState.RetailPicking) return;
            float dt = Time.deltaTime;
            PlayedSeconds += dt; ToastTime = Mathf.Max(0, ToastTime - dt);
            Vector2 movement = new Vector2((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
            MovePlayer(movement, dt);
            UpdateGuests(dt);
            if (State == ScreenState.GameOver) return;
            UpdateRetail(dt);
            if (State != ScreenState.Playing && State != ScreenState.Cooking && State != ScreenState.RetailPicking) return;
            spawnTimer -= dt;
            if (spawnTimer <= 0) { SpawnParty(); spawnTimer = tuning.arrivalInterval * UnityEngine.Random.Range(.85f, 1.15f); }
            if (State == ScreenState.Cooking || State == ScreenState.RetailPicking) return;
            RefreshTarget();
            HandleInteraction(Input.GetKeyDown(KeyCode.F), Input.GetKey(KeyCode.F), dt);
        }
        public void MovePlayer(Vector2 direction, float dt)
        {
            if (State != ScreenState.Playing) return;
            PlayerPosition = Navigation.Move(PlayerPosition, Vector2.ClampMagnitude(direction, 1) * tuning.playerSpeed * Mathf.Max(0, dt));
        }
        public bool IsTableBusy(int index) => Guests.Any(g => g.table == index && g.state != GuestState.Leaving);
        public bool CanClean(int index) => Tables[index].dirty && !IsTableBusy(index) && !Guests.Any(g => g.table == index && Vector2.Distance(g.position, Map.tables[index]) < 2.4f);

        public bool SpawnParty(int forcedTable = -1, int forcedSize = 0)
        {
            var free = Tables.Where(t => !t.dirty && !IsTableBusy(t.index) && !Guests.Any(g => g.table == t.index)).ToList();
            int occupied = Tables.Count(t => IsTableBusy(t.index));
            if (free.Count == 0)
            {
                // A natural arrival still enters and turns away. Explicit table requests
                // are placement attempts and must not create an unrelated rejected party.
                if (forcedTable < 0) TurnAwayArrivingParty(forcedSize);
                return false;
            }
            if (forcedTable < 0 && occupied >= tuning.maxOccupiedTables) return false;
            var table = forcedTable >= 0 ? free.Find(t => t.index == forcedTable) : free[UnityEngine.Random.Range(0, free.Count)];
            if (table == null) return false;
            table.service = TableServiceState.Arriving; table.quest = null; table.failed = false;
            table.patience = table.totalPatience = tuning.orderPatienceSeconds;
            int count = forcedSize > 0 ? Mathf.Clamp(forcedSize, 1, 4) : UnityEngine.Random.Range(1, 5);
            table.partySize = count; table.itemOrder = new TableItemOrder();
            int[] seats = { 0, 1, 2, 3 };
            for (int i = 3; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); int temp = seats[i]; seats[i] = seats[j]; seats[j] = temp; }
            for (int i = 0; i < count; i++)
            {
                Vector2 start = Map.entrance + new Vector2((i - (count - 1) * .5f) * .42f, 0);
                var g = new Guest { id = ++nextId, table = table.index, seat = seats[i], state = GuestState.Entering, position = start };
                g.path = Navigation.Path(start, Map.seats[g.table][g.seat], g.id);
                Guests.Add(g);
            }
            Sound.Play(); Say(count + "명의 손님이 들어왔어요 · " + (table.index + 1).ToString("00") + "번 테이블");
            return true;
        }
        void UpdateGuests(float dt)
        {
            for (int i = Guests.Count - 1; i >= 0; i--)
            {
                var guest = Guests[i];
                guest.departureReaction = Mathf.Max(0, guest.departureReaction - dt);
                bool walking = guest.state == GuestState.Entering || guest.state == GuestState.Leaving || guest.state == GuestState.NoSeatApproach;
                if (walking)
                {
                    float distance = tuning.guestSpeed * dt;
                    while (guest.pathIndex < guest.path.Count && distance > 0)
                    {
                        var target = guest.path[guest.pathIndex]; float remaining = Vector2.Distance(guest.position, target);
                        if (remaining <= distance) { guest.position = target; distance -= remaining; guest.pathIndex++; }
                        else { guest.position = Vector2.MoveTowards(guest.position, target, distance); distance = 0; }
                    }
                    if (guest.pathIndex >= guest.path.Count)
                    {
                        if (guest.state == GuestState.Leaving) { Guests.RemoveAt(i); continue; }
                        if (guest.state == GuestState.NoSeatApproach)
                        {
                            guest.state = GuestState.Leaving; guest.pathIndex = 0;
                            guest.failedDeparture = true; guest.departureReaction = 2.2f;
                            guest.path = Navigation.Path(guest.position, Map.entrance, guest.id + 197);
                        }
                        else guest.state = GuestState.Seated;
                    }
                }
            }
            foreach (SaunaTable table in Tables)
            {
                if (table.service == TableServiceState.Arriving)
                {
                    if (Guests.Where(g => g.table == table.index).All(g => g.state == GuestState.Seated))
                        table.service = TableServiceState.Waiting;
                    continue;
                }
                if (table.service == TableServiceState.Departing && !Guests.Any(g => g.table == table.index)) table.service = TableServiceState.Empty;
                if (table.service != TableServiceState.Waiting && table.service != TableServiceState.Ordered) continue;
                table.patience -= dt;
                if (table.patience <= 0) { Resolve(table, false); if (State == ScreenState.GameOver) break; }
            }
        }

        public void RefreshTarget()
        {
            TargetOrderTable = null; TargetStation = -1;
            TargetRetailShelf = retailLayout != null && retailLayout.AtShelf(PlayerPosition);
            TargetWasteBin = worldView.layout.foodWasteBin != null && Vector2.Distance(PlayerPosition, worldView.layout.foodWasteBin.position) < 1.65f;
            if (!TargetWasteBin) wasteConfirmed = false;
            var priorTable = TargetTable; TargetTable = null;
            float nearest = 3f;
            foreach (SaunaTable table in Tables)
            {
                if (table.service != TableServiceState.Waiting && table.service != TableServiceState.Ordered) continue;
                float distance = Vector2.Distance(PlayerPosition, Map.tables[table.index]);
                if (distance < nearest) { nearest = distance; TargetOrderTable = table; }
            }
            if (TargetOrderTable == null && !TargetWasteBin)
            {
                float tableDistance = 3;
                foreach (var table in Tables)
                {
                    if (!CanClean(table.index)) continue;
                    float d = Vector2.Distance(PlayerPosition, Map.tables[table.index]);
                    if (d < tableDistance) { tableDistance = d; TargetTable = table; }
                }
                if (TargetTable == null)
                    for (int i = 0; i < Map.stations.Length; i++)
                        if (Vector2.Distance(PlayerPosition, Map.stations[i]) < (i == 3 ? 2.1f : 3f)) { TargetStation = i; break; }
            }
            if (retailLayout != null && retailLayout.AtStaffSide(PlayerPosition)) TargetStation = 1;
            if (priorTable != null && priorTable != TargetTable) ResetCleaning(priorTable);
        }
        public void HandleInteraction(bool down, bool held, float dt)
        {
            if (State != ScreenState.Playing) return;
            if (TargetTable != null && HasCleaningTool)
            {
                var table = TargetTable;
                if (down && table.cleaningMode < 0) { table.cleaningMode = UnityEngine.Random.Range(0, 2); Sound.Play(3); }
                if (table.cleaningMode == 0)
                {
                    table.held = held ? table.held + dt : 0;
                    if (table.held >= tuning.cleaningHoldSeconds) FinishCleaning(table);
                }
                else if (table.cleaningMode == 1 && down)
                {
                    table.presses++; Sound.Play(3);
                    if (table.presses >= tuning.cleaningPresses) FinishCleaning(table);
                }
                return;
            }
            if (!down) return;
            if (TargetRetailShelf) { OpenRetailPicking(); return; }
            if (TargetWasteBin) { InteractWasteBin(); return; }
            wasteConfirmed = false;
            if (TargetOrderTable != null)
            {
                if (TargetOrderTable.service == TableServiceState.Waiting) Accept(TargetOrderTable);
                else if (TargetOrderTable.RequiresItems) DeliverTableItems(TargetOrderTable);
                else Resolve(TargetOrderTable, true);
            }
            else if (TargetTable != null) { Say("카운터 오른쪽에서 청소 도구를 가져오세요."); Sound.Play(2); }
            else if (TargetStation >= 0)
            {
                if (TargetStation == 3)
                {
                    if (ActiveRetailOrder != null) { RejectPickup("카운터에 기다리는 손님이 있어요!"); return; }
                    if (!HasCleaningTool && (Model.inventory.Count > 0 || CarryingTableGoods)) { RejectPickup("들고 있는 물건을 먼저 내려놓아 주세요."); return; }
                    HasCleaningTool = !HasCleaningTool; Say(HasCleaningTool ? "청소 도구를 들었어요. 더러운 테이블에서 F!" : "청소 도구를 제자리에 두었어요.");
                }
                else if (TargetStation == 0) { OpenCooking(); return; }
                else if (TargetStation == 1) { InteractRetailRegister(); return; }
                else Say(new[] { "", "계산대를 확인했어요.  ·  계산 기능은 임시 체험입니다.", "잠깐 숨을 돌렸어요.  ·  휴식 기능은 임시 체험입니다." }[TargetStation]);
                Sound.Play(1);
            }
        }
        public void Accept(SaunaTable table)
        {
            if (table.service != TableServiceState.Waiting) return;
            table.quest = quests.Draw(Rating); table.service = TableServiceState.Ordered;
            table.patience = table.totalPatience = Mathf.Max(5, table.quest.deliverySeconds);
            if (table.IsTableRetailOrder)
            {
                table.itemOrder = TableItemOrder.CreateRetail(items);
                Say("테이블 잡화 주문이에요. 진열대에서 가져와 주세요.");
            }
            else if (table.RequiresItems)
            {
                for (int n = 0; n < table.partySize; n++) table.itemOrder.Add(items.RandomFood());
                Say("음식 주문을 받았어요. 조리대에서 준비해 주세요.");
            }
            else Say("주문 " + table.quest.id + " · F를 한 번 더 누르면 테이블 응대 완료");
            Sound.Play();
            OrderAccepted?.Invoke(table, table.quest);
        }
        public void Resolve(SaunaTable table, bool success)
        {
            if (table.service != TableServiceState.Waiting && table.service != TableServiceState.Ordered) return;
            table.failed = !success; table.service = TableServiceState.Departing;
            if (TargetOrderTable == table) TargetOrderTable = null;
            // Party size is visual only: one order, one timer, one result for the entire table.
            foreach (Guest guest in Guests.Where(g => g.table == table.index))
            {
                guest.state = GuestState.Leaving; guest.pathIndex = 0;
                guest.failedDeparture = !success; guest.departureReaction = success ? 0 : 2.2f;
                guest.path = Navigation.Path(guest.position, Map.entrance, guest.id + 197);
            }
            table.dirty = true;
            if (success)
            {
                Completed++; Sound.Play(1);
                if (table.itemOrder.DeliveryMistakes == 0)
                {
                    Rating = Mathf.Min(5, Rating + 5 * tuning.successPercent);
                    Say(table.IsTableRetailOrder && !table.itemOrder.IsAdditionalRequest
                        ? "추가 주문까지 맞혔어요! 테이블 응대 완료! +3%"
                        : "테이블 응대 완료! +3% · 일행이 나가면 테이블을 청소하세요.");
                }
                else Say("테이블 잡화 전달 완료! · 일행이 나가면 테이블을 청소하세요.");
            }
            else
            {
                Rating = Mathf.Max(1, Rating - 5 * tuning.failurePercent);
                Failed++; Sound.Play(2); Say("기다리던 일행이 돌아갔어요. 별점 −5%");
            }
            OrderResolved?.Invoke(table, success);
            if (table.IsTableRetailOrder && State == ScreenState.RetailPicking && !HasShelfOrder && !CarryingTableGoods)
                CloseRetailPicking();
            if (Rating <= 1.0001f) SetState(ScreenState.GameOver);
        }
        public void FinishCleaning(SaunaTable table)
        {
            if (!HasCleaningTool || !CanClean(table.index)) return;
            table.dirty = false; ResetCleaning(table); Sound.Play(1); Say("깨끗해졌어요! 새 손님을 받을 수 있어요.");
        }
        static void ResetCleaning(SaunaTable table) => table.ResetCleaning();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        public void DebugPosition(Vector2 p) { PlayerPosition = p; WorldView.Sync(); RefreshTarget(); }
        public void DebugRating(float value) => Rating = Mathf.Clamp(value, 1, 5);
        public void DebugTool(bool value) => HasCleaningTool = value;
        public void DebugTickGuests(float dt) => UpdateGuests(dt);
        public void DebugDelayArrivals(float seconds) { spawnTimer = seconds; retailSpawnTimer = seconds; }
#endif
    }
}
