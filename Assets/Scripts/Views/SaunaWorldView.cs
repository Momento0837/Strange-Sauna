using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace SuspiciousSauna
{
    [ExecuteAlways]
    public sealed class SaunaWorldView : MonoBehaviour
    {
        [Header("씬에 저장된 오브젝트")]
        public SaunaSceneLayout layout;
        public Camera playCamera, backgroundCamera;
        public SaunaCharacterView player;
        public GameObject cleaningTool;
        public Transform guestContainer;
        [Header("손님 에셋 교체")] public SaunaCharacterView[] guestPrefabs;
        public SaunaRetailCustomerView retailGuestPrefab;
        public Transform retailGuestContainer;
        public SaunaItemDefinition retailBagAppearance;
        [Min(1)] public float cameraHalf = 8.6f;
        readonly Dictionary<int, SaunaCharacterView> guests = new Dictionary<int, SaunaCharacterView>();
        readonly Dictionary<int, SaunaRetailCustomerView> retailGuests = new Dictionary<int, SaunaRetailCustomerView>();
        SaunaSessionModel model; SaunaMapData map;
        Vector2 lastPosition; bool faceLeft;
        public Camera Camera => playCamera;
        public int SpawnedGuestCount => guests.Count;
        internal static int DepthOrder(float groundY) => 500 - Mathf.RoundToInt(groundY * 10);
        public void Initialize(SaunaSessionModel session, SaunaMapData sceneMap)
        {
            model = session; map = sceneMap; lastPosition = session.playerPosition;
            if (playCamera == null || player == null || guestContainer == null || guestPrefabs == null || guestPrefabs.Length == 0)
                throw new System.InvalidOperationException("World View의 씬 오브젝트와 손님 프리팹을 연결하세요.");
            foreach (var table in layout.tables) table.ApplyDepthOrder();
            Sync();
        }
        void LateUpdate() { UpdateViewport(); if (Application.isPlaying && model != null) Sync(); }
        void UpdateViewport()
        {
            if (playCamera == null) return;
            Rect r = SaunaMap.PlayRect(Screen.width, Screen.height);
            playCamera.rect = new Rect(r.x / Screen.width, r.y / Screen.height, r.width / Screen.width, r.height / Screen.height);
        }
        public void Sync()
        {
            if (model == null) return;
            Vector2 movement = model.playerPosition - lastPosition;
            if (movement.x != 0) faceLeft = movement.x < 0;
            player.Present(model.playerPosition, movement.sqrMagnitude > .00001f, faceLeft, model.playedSeconds);
            if (player.rightHand != null) player.rightHand.Present(model.retailBag.Count > 0 ? Enumerable.Range(0, model.retailBag.Capacity).Select(i => model.retailBag[i]).FirstOrDefault(i => i != null) : model.inventory.Right);
            if (player.leftHand != null) player.leftHand.Present(model.retailBag.Count > 0 ? retailBagAppearance : model.inventory.Left);
            lastPosition = model.playerPosition;
            if (cleaningTool != null) cleaningTool.SetActive(model.hasCleaningTool);
            foreach (int id in guests.Keys.Where(id => !model.guests.Any(g => g.id == id)).ToArray())
            { Destroy(guests[id].gameObject); guests.Remove(id); }
            foreach (Guest guest in model.guests)
            {
                if (!guests.TryGetValue(guest.id, out var view))
                {
                    view = Instantiate(guestPrefabs[guest.id % guestPrefabs.Length], guestContainer);
                    view.name = guest.table < 0 ? $"Guest_{guest.id:000}_NoSeat" : $"Guest_{guest.id:000}_Table_{guest.table + 1:00}";
                    guests.Add(guest.id, view);
                }
                view.Present(guest.position, guest.state != GuestState.Seated, guest.seat < 2, model.playedSeconds + guest.id,
                    guest.failedDeparture ? .025f : .04f, guest.failedDeparture && guest.departureReaction > 0);
                if (view.guestStatus != null) view.guestStatus.Present(guest, guest.table >= 0 && guest.table < model.tables.Count ? model.tables[guest.table] : null);
            }
            foreach (var table in layout.tables) if (table.dirt != null) table.dirt.SetActive(model.tables[table.index].dirty);
            foreach (int id in retailGuests.Keys.Where(id => !model.retailCustomers.Any(c => c.id == id)).ToArray())
            { Destroy(retailGuests[id].gameObject); retailGuests.Remove(id); }
            foreach (var customer in model.retailCustomers)
            {
                if (!retailGuests.TryGetValue(customer.id, out var view))
                {
                    view = Instantiate(retailGuestPrefab, retailGuestContainer);
                    view.name = $"RetailGuest_{customer.id:000}"; retailGuests.Add(customer.id, view);
                }
                view.Present(customer, model.playedSeconds, model.retailQueue.FirstOrDefault() == customer);
            }
            bool menu = model.screen == ScreenState.Menu || (model.screen == ScreenState.Settings && model.settingsReturn == ScreenState.Menu);
            float half = Mathf.Min(cameraHalf, Mathf.Min(map.bounds.width, map.bounds.height) * .5f);
            Vector2 target = map.ClampCamera(menu ? map.playerStart : model.playerPosition, half);
            playCamera.orthographicSize = half;
            playCamera.transform.position = new Vector3(target.x, target.y, -10);
            UpdateViewport();
        }
    }
}
