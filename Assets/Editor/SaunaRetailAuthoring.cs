using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SuspiciousSauna;

public static partial class SaunaSceneAuthoring
{
    public static void EnsureRetailAuthored()
    {
        var controller = Object.FindFirstObjectByType<SaunaGame>();
        var world = Object.FindFirstObjectByType<SaunaWorldView>();
        var ui = Object.FindFirstObjectByType<SaunaUI>();
        if (controller.retailLayout != null && ui.retailPicker != null && world.retailGuestPrefab != null && ui.receiptPrefab.retailLines != null) { PolishRetailPresentation(world); return; }
        const string backup = "Logs/BeforeRetail"; Directory.CreateDirectory(backup);
        EditorSceneManager.SaveScene(controller.gameObject.scene);
        if (!File.Exists(backup + "/Sauna.unity")) File.Copy(controller.gameObject.scene.path, backup + "/Sauna.unity");
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumGothic.ttf");
        material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SaunaSprites.mat");
        var catalog = controller.items;
        if (catalog.retailMenu == null || catalog.retailMenu.Length == 0)
        {
            string[] ids = { "toothbrush_set", "shampoo", "conditioner", "body_wash", "razor", "face_mask", "scrub_towel", "scrub_soap", "gua_sha" };
            string[] names = { "일회용 칫솔/치약", "샴푸 소포장", "린스 소포장", "바디워시 소포장", "일회용 면도기", "마스크팩", "때밀이 타올", "때비누", "괄사" };
            catalog.retailMenu = new SaunaItemDefinition[9];
            for (int i = 0; i < 9; i++) catalog.retailMenu[i] = RetailItem(ids[i], names[i], i);
            EditorUtility.SetDirty(catalog);
        }
        world.retailBagAppearance = RetailItem("shopping_bag", "잡화 봉투", 9);
        var counter = world.layout.stations[1];
        if (controller.retailLayout == null)
        {
            var layout = counter.gameObject.AddComponent<SaunaRetailLayout>(); controller.retailLayout = layout;
            layout.staffPoint = Point("StaffInteraction", counter, new Vector2(0, 2));
            layout.exitSidePoint = Point("ExitSide", counter, new Vector2(-3.3f, -2.3f));
            var queue = Point("CustomerQueue", counter, Vector2.zero);
            layout.queueSpots = new Transform[3];
            for (int i = 0; i < 3; i++) layout.queueSpots[i] = Point("Queue_" + (i + 1), queue, new Vector2(0, -2.25f - 1.65f * i));
            layout.overflowLookPoint = Point("OverflowLookPoint", world.layout.transform, world.layout.mapBounds.bounds.center);
            var shelfRoot = Node("RetailShelf");
            var visual = Node("Visual", shelfRoot.transform).AddComponent<SpriteRenderer>();
            visual.sprite = SaunaRetailArt.Make("retail_shelf", 12); visual.sharedMaterial = material; visual.sortingOrder = -85;
            visual.transform.localScale = new Vector3(3.1f, 1.7f, 1);
            Obstacle(shelfRoot, new Vector2(3.1f, .8f));
            var shelf = Instance(SavePrefab(shelfRoot, "RetailShelf"), counter.parent, "RetailShelf", SaunaMap.RetailShelf);
            layout.shelfInteraction = Point("ShelfInteraction", shelf.transform, new Vector2(0, -1.15f));
            WorldLabel(shelf.transform, "잡화 진열대", new Vector2(0, .8f), 3);
            var label = counter.Find("Label/Text")?.GetComponent<Text>(); if (label != null) label.text = "포스기";
            WorldLabel(layout.staffPoint, "응대 위치", new Vector2(-.85f, .15f), 2);
            // Floor marks guide the queue without adding navigation obstacles.
            for (int i = 0; i < 3; i++)
            {
                var mark = Node("QueueMark", layout.queueSpots[i]).AddComponent<SpriteRenderer>();
                mark.sprite = Art("halo"); mark.sharedMaterial = material; mark.color = new Color(1, .88f, .6f, .32f); mark.sortingOrder = -500;
                mark.transform.localScale = Vector3.one * .8f;
            }
            EditorUtility.SetDirty(layout);
        }
        if (world.retailGuestContainer == null) world.retailGuestContainer = Node("RetailGuests", world.guestContainer.parent).transform;
        if (world.retailGuestPrefab == null) world.retailGuestPrefab = CreateRetailGuest(world.guestPrefabs[1]);
        if (ui.receiptPrefab.retailLines == null) AddRetailReceipt();
        if (ui.retailPicker == null) ui.retailPicker = CreateRetailPicker(ui.transform, catalog);
        if (ui.retailStatus == null)
        {
            ui.retailStatus = Label("RetailQueue", ui.statusScroll.content, "포스기  0 / 3", 15, Cream); Box(ui.retailStatus.rectTransform, 0, 570, 230, 40);
            ui.retailBagRow = Rect("RetailBag", ui.statusScroll.content); Box(ui.retailBagRow, 0, 480, 230, 62);
            ui.retailBagItems = new SaunaItemQuantityView[5];
            for (int i = 0; i < 5; i++) { ui.retailBagItems[i] = ItemQuantity("Slot_" + (i + 1), ui.retailBagRow, Cream); Box((RectTransform)ui.retailBagItems[i].transform, i * 44, 0, 42, 54); }
        }
        if (ui.retailIndicator == null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CustomerBubble.prefab");
            ui.retailIndicator = Instance(asset, ui.hud.transform, "RetailOffscreenIndicator", Vector2.zero).GetComponent<SaunaBubbleView>();
            ui.retailIndicator.gameObject.SetActive(false);
        }
        EditorUtility.SetDirty(controller); EditorUtility.SetDirty(world); EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(controller.tuning);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(controller.gameObject.scene); EditorSceneManager.SaveScene(controller.gameObject.scene);
        ValidateRetailAuthoring(controller);
        PolishRetailPresentation(world);
    }
    static SaunaItemDefinition RetailItem(string id, string name, int art)
    {
        string path = "Assets/Data/Items/" + id + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<SaunaItemDefinition>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<SaunaItemDefinition>(); item.id = id; item.displayName = name;
            item.category = art == 9 ? ItemCategory.Miscellaneous : ItemCategory.Hygiene; item.icon = SaunaRetailArt.Make(id, art);
            AssetDatabase.CreateAsset(item, path);
        }
        return item;
    }
    static SaunaItemQuantityView ItemQuantity(string name, Transform parent, Color color)
    {
        var root = Rect(name, parent); var view = root.gameObject.AddComponent<SaunaItemQuantityView>();
        view.icon = Image("Icon", root, Color.white); view.icon.preserveAspect = true; Stretch(view.icon.rectTransform, 5, 0, 5, 18);
        view.quantity = Label("Quantity", root, "×1", 14, color, true);
        view.quantity.rectTransform.anchorMin = Vector2.zero; view.quantity.rectTransform.anchorMax = new Vector2(1, 0);
        view.quantity.rectTransform.pivot = new Vector2(.5f, 0); view.quantity.rectTransform.sizeDelta = new Vector2(0, 20);
        return view;
    }
    static SaunaRetailCustomerView CreateRetailGuest(SaunaCharacterView source)
    {
        var root = Object.Instantiate(source.gameObject); root.name = "RetailCustomer";
        var view = root.AddComponent<SaunaRetailCustomerView>(); view.character = root.GetComponent<SaunaCharacterView>();
        var canvasRoot = new GameObject("SpeechCanvas", typeof(RectTransform), typeof(Canvas)); canvasRoot.transform.SetParent(root.transform, false);
        canvasRoot.transform.localPosition = new Vector3(0, 2, 0); canvasRoot.transform.localScale = Vector3.one * .022f;
        var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 700;
        ((RectTransform)canvasRoot.transform).sizeDelta = new Vector2(280, 68);
        var bubble = Image("SpeechBubble", canvasRoot.transform, Color.white, SaunaRetailArt.Make("speech_panel", 13)); bubble.type = UnityEngine.UI.Image.Type.Sliced;
        view.bubble = bubble.rectTransform; view.bubble.sizeDelta = new Vector2(280, 68);
        var contents = Rect("OrderIcons", view.bubble); Stretch(contents); view.orderContents = contents.gameObject;
        view.items = new SaunaItemQuantityView[5];
        for (int i = 0; i < 5; i++) view.items[i] = ItemQuantity("Item_" + (i + 1), contents, Ink);
        view.reaction = Image("Reaction", view.bubble, Color.white); Stretch(view.reaction.rectTransform, 10, 10, 10, 10); view.reaction.preserveAspect = true;
        view.music = SaunaRetailArt.Make("music", 10); view.dust = SaunaRetailArt.Make("dust", 11);
        view.waiting = Label("Waiting", view.bubble, "!", 34, Ink, true); Stretch(view.waiting.rectTransform);
        AddSpeechTail(view.bubble);
        return SavePrefab(root, "RetailCustomer").GetComponent<SaunaRetailCustomerView>();
    }
    static void AddRetailReceipt()
    {
        const string path = "Assets/Prefabs/Receipt.prefab"; var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<SaunaReceiptView>(); var rows = Rect("RetailItems", view.normal.transform);
            Stretch(rows, 10, 84, 10, 64); view.retailLines = rows.gameObject; view.retailItems = new SaunaItemQuantityView[3];
            for (int i = 0; i < 3; i++) { view.retailItems[i] = ItemQuantity("Item_" + (i + 1), rows, Ink); Box((RectTransform)view.retailItems[i].transform, i * 68, 0, 60, 60); }
            rows.gameObject.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static SaunaRetailPickerView CreateRetailPicker(Transform parent, SaunaItemCatalog catalog)
    {
        var modal = Modal("RetailPicking", parent, out var card); var view = modal.AddComponent<SaunaRetailPickerView>(); view.card = card;
        var title = Label("Title", card, "잡화 담기", 28, Cream, true);
        title.rectTransform.anchorMin = new Vector2(0, 1); title.rectTransform.anchorMax = Vector2.one; title.rectTransform.pivot = new Vector2(.5f, 1);
        title.rectTransform.anchoredPosition = new Vector2(0, -20); title.rectTransform.sizeDelta = new Vector2(-40, 45);
        view.capacity = Label("Capacity", card, "담은 잡화 0 / 5", 14, Cream, true);
        view.capacity.rectTransform.anchorMin = new Vector2(0, 1); view.capacity.rectTransform.anchorMax = Vector2.one; view.capacity.rectTransform.pivot = new Vector2(.5f, 1);
        view.capacity.rectTransform.anchoredPosition = new Vector2(0, -68); view.capacity.rectTransform.sizeDelta = new Vector2(-48, 40);
        view.scroll = Scroll("Items", card); Stretch((RectTransform)view.scroll.transform, 28, 116, 28, 210); view.content = view.scroll.content;
        view.choices = new SaunaRetailChoice[catalog.retailMenu.Length];
        for (int i = 0; i < view.choices.Length; i++)
        {
            var item = catalog.retailMenu[i]; var button = Button(item.id, view.content, item.displayName, 0, 164);
            Box((RectTransform)button.transform, i % 3 * 180, i / 3 * 120, 164, 108);
            var text = button.GetComponentInChildren<Text>(); Box(text.rectTransform, 4, 74, 156, 28); text.fontSize = 15;
            var icon = Image("Icon", button.transform, Color.white, item.icon); icon.preserveAspect = true; Box(icon.rectTransform, 54, 8, 56, 56);
            view.choices[i] = new SaunaRetailChoice { item = item, button = button };
        }
        view.bagRow = Rect("BagSlots", card); view.bagButtons = new Button[5]; view.bagIcons = new Image[5]; view.bagNumbers = new Text[5];
        for (int i = 0; i < 5; i++)
        {
            var button = Button("Slot_" + (i + 1), view.bagRow, (i + 1).ToString(), 0, 56); Box((RectTransform)button.transform, i * 64, 0, 56, 56);
            view.bagButtons[i] = button; view.bagNumbers[i] = button.GetComponentInChildren<Text>(); view.bagNumbers[i].alignment = TextAnchor.LowerRight;
            view.bagIcons[i] = Image("Item", button.transform, Color.white); view.bagIcons[i].preserveAspect = true; Stretch(view.bagIcons[i].rectTransform, 6, 4, 6, 8);
            view.bagIcons[i].gameObject.SetActive(false);
        }
        view.closeButton = Button("Close", card, "닫기  ·  ESC", 0, 594, true);
        var close = (RectTransform)view.closeButton.transform; close.anchorMin = Vector2.zero; close.anchorMax = new Vector2(1, 0); close.pivot = new Vector2(.5f, 0);
        close.anchoredPosition = new Vector2(0, 20); close.sizeDelta = new Vector2(-56, 52);
        modal.SetActive(false); return view;
    }
    static void AddSpeechTail(Transform parent)
    {
        if (parent.Find("Tail") != null) return;
        var tail = Image("Tail", parent, Color.white, SaunaRetailArt.Make("speech_tail", 14));
        tail.rectTransform.anchorMin = tail.rectTransform.anchorMax = new Vector2(.5f, 0); tail.rectTransform.pivot = new Vector2(.5f, 1);
        tail.rectTransform.sizeDelta = new Vector2(24, 24); tail.rectTransform.anchoredPosition = new Vector2(0, 12);
        tail.transform.SetAsFirstSibling();
    }
    static void PolishRetailPresentation(SaunaWorldView world)
    {
        var existing = world.retailGuestPrefab.bubble.GetComponent<Image>().sprite;
        if (existing == null || existing.name != "TableNumberBadge") return;
        const string path = "Assets/Prefabs/RetailCustomer.prefab"; var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<SaunaRetailCustomerView>(); var paper = view.bubble.GetComponent<Image>();
            paper.sprite = SaunaRetailArt.Make("speech_panel", 13); paper.color = Color.white; paper.type = UnityEngine.UI.Image.Type.Sliced;
            AddSpeechTail(view.bubble); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    public static void ValidateRetailAuthoring(SaunaGame game)
    {
        var layout = game.retailLayout; var nav = new SaunaNavigation(Object.FindFirstObjectByType<SaunaSceneLayout>().Capture());
        var map = Object.FindFirstObjectByType<SaunaSceneLayout>().Capture();
        var targets = layout.queueSpots.Select(p => (Vector2)p.position).Concat(new[] { (Vector2)layout.staffPoint.position, (Vector2)layout.shelfInteraction.position, (Vector2)layout.overflowLookPoint.position, (Vector2)layout.exitSidePoint.position });
        foreach (var target in targets)
        {
            var path = nav.Path(map.entrance, target, 17);
            if (!nav.Walkable(target) || path.Count == 0 || Vector2.Distance(path.Last(), target) > .01f || path.Any(p => !nav.Walkable(p)))
                throw new System.InvalidOperationException("잡화 이동 경로/상호작용 위치를 확인하세요: " + target);
        }
        File.WriteAllText("Logs/retail-authoring-validation.txt", "Retail anchors and paths valid; 9 item assets, 3 queue slots, staff-side POS and shelf authored.");
    }
}
