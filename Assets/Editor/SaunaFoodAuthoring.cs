using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SuspiciousSauna;

public static partial class SaunaSceneAuthoring
{
    static void UpgradeFoodService()
    {
        var controller = Object.FindFirstObjectByType<SaunaGame>();
        var layout = Object.FindFirstObjectByType<SaunaSceneLayout>();
        var world = layout.GetComponent<SaunaWorldView>();
        var ui = Object.FindFirstObjectByType<SaunaUI>();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumGothic.ttf");
        material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SaunaSprites.mat");
        var catalog = AssetDatabase.LoadAssetAtPath<SaunaItemCatalog>("Assets/Resources/SaunaItemCatalog.asset");
        if (catalog == null)
        {
            Directory.CreateDirectory("Assets/Data/Items");
            string[] ids = { "eggs", "sikhye", "iced_coffee", "banana_milk", "ramyeon", "dumplings", "hotbar", "seaweed_soup", "pork_rice" };
            string[] names = { "맥반석계란", "식혜", "아이스커피", "바나나우유", "라면", "만두", "핫바", "미역국", "제육덮밥" };
            catalog = ScriptableObject.CreateInstance<SaunaItemCatalog>(); catalog.cookingMenu = new SaunaItemDefinition[9];
            for (int i = 0; i < 9; i++)
            {
                string path = "Assets/Data/Items/" + ids[i] + ".asset";
                var item = AssetDatabase.LoadAssetAtPath<SaunaItemDefinition>(path);
                if (item == null) { item = ScriptableObject.CreateInstance<SaunaItemDefinition>(); item.id = ids[i]; item.displayName = names[i]; item.category = ItemCategory.Food; item.icon = SaunaFoodArt.Make(ids[i], i); AssetDatabase.CreateAsset(item, path); }
                catalog.cookingMenu[i] = item;
            }
            AssetDatabase.CreateAsset(catalog, "Assets/Resources/SaunaItemCatalog.asset");
        }
        if (controller.items == null) { controller.items = catalog; EditorUtility.SetDirty(controller); }
        if (ui.receiptPrefab.GetComponent<Shadow>() == null)
        {
            const string receiptPath = "Assets/Prefabs/Receipt.prefab";
            var receiptRoot = PrefabUtility.LoadPrefabContents(receiptPath);
            try { var shadow = receiptRoot.AddComponent<Shadow>(); shadow.effectColor = new Color(.12f,.2f,.17f,.38f); shadow.effectDistance = new Vector2(2,-4); PrefabUtility.SaveAsPrefabAsset(receiptRoot,receiptPath); }
            finally { PrefabUtility.UnloadPrefabContents(receiptRoot); }
        }
        if (layout.foodWasteBin != null && ui.cooking != null && world.player.rightHand != null && ui.receiptPrefab.timerText != null && ui.heldItemsText != null) return;
        if (world.player.rightHand == null)
        {
            string path = "Assets/Prefabs/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var character = root.GetComponent<SaunaCharacterView>();
                character.rightHand = Hand("RightHand", root.transform, .55f);
                character.leftHand = Hand("LeftHand", root.transform, -.55f);
                character.sweatEffect = Node("SweatEffect", root.transform);
                character.sweatEffect.transform.localPosition = new Vector3(.58f, 1.05f, 0);
                var sweat = character.sweatEffect.AddComponent<SpriteRenderer>(); sweat.sprite = SaunaFoodArt.Make("sweat", 10); sweat.sharedMaterial = material; sweat.sortingOrder = 6;
                character.sweatEffect.transform.localScale = Vector3.one * .7f; character.sweatEffect.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        if (layout.foodWasteBin == null)
        {
            var root = Node("FoodWasteBin"); var visual = Node("Visual", root.transform).AddComponent<SpriteRenderer>();
            visual.sprite = SaunaFoodArt.Make("food_waste_bin", 9); visual.sharedMaterial = material; visual.sortingOrder = -89; visual.transform.localScale = Vector3.one * 1.65f;
            Obstacle(root, new Vector2(1, 1));
            var asset = SavePrefab(root, "FoodWasteBin");
            layout.foodWasteBin = Instance(asset, layout.stations[0].parent, "FoodWasteBin", (Vector2)layout.stations[0].localPosition + Vector2.right * 3.8f).transform;
            WorldLabel(layout.foodWasteBin, "음식물", Vector2.up * 1.05f, 2);
        }
        if (ui.receiptPrefab.timerText == null) UpgradeReceipt();
        if (ui.cooking == null) ui.cooking = CookingUI(ui.transform, catalog);
        if (ui.narration == null)
        {
            var image = Image("Narration", ui.hud.transform, new Color(.12f, .23f, .2f, .92f));
            Box(image.rectTransform, 400, 60, 640, 48);
            ui.narration = image.gameObject.AddComponent<CanvasGroup>(); ui.narration.blocksRaycasts = false; ui.narration.interactable = false; ui.narration.alpha = 0;
            ui.narrationText = Label("Text", image.transform, "", 18, Cream, true); Stretch(ui.narrationText.rectTransform, 10, 4, 10, 4);
        }
        if (ui.heldItemsText == null) { ui.heldItemsText = Label("Hands", ui.statusScroll.content, "오른손  비어 있음\n왼손  비어 있음", 15, Cream); Box(ui.heldItemsText.rectTransform, 0, 445, 230, 72); }
        ui.leftRail.name = "Orders"; ui.rightRail.name = "Status";
        ui.PreviewPlay();
        EditorUtility.SetDirty(layout); EditorUtility.SetDirty(ui); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(layout.gameObject.scene); EditorSceneManager.SaveScene(layout.gameObject.scene);
    }
    static SaunaHeldItemView Hand(string name, Transform parent, float x)
    {
        var root = Node(name, parent); root.transform.localPosition = new Vector3(x, .3f, 0);
        var view = root.AddComponent<SaunaHeldItemView>(); view.icon = Node("Icon", root.transform).AddComponent<SpriteRenderer>();
        view.icon.sharedMaterial = material; view.icon.sortingOrder = 5; view.icon.gameObject.SetActive(false); return view;
    }
    static void UpgradeReceipt()
    {
        string path = "Assets/Prefabs/Receipt.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var v = root.GetComponent<SaunaReceiptView>(); root.GetComponent<Image>().raycastTarget = true;
            Box(v.rect, 0, 0, 270, 260);
            Stretch(v.tableLabel.rectTransform, 12, 12, 55, 222); v.tableLabel.fontSize = 15;
            Box(v.code.rectTransform, 215, 12, 45, 25); v.code.fontSize = 16;
            v.code.rectTransform.anchorMin = v.code.rectTransform.anchorMax = new Vector2(1, 1); v.code.rectTransform.anchoredPosition = new Vector2(-52, -12);
            Stretch(v.title.rectTransform, 12, 42, 12, 192); v.title.alignment = TextAnchor.UpperLeft; v.title.fontSize = 14;
            Stretch(v.description.rectTransform, 12, 75, 12, 82); v.description.fontSize = 16; v.description.lineSpacing = 1.2f;
            Stretch(v.status.rectTransform, 12, 192, 78, 12); v.status.alignment = TextAnchor.MiddleLeft; v.status.fontSize = 13;
            AddTimer(v.normal.transform, v.patience, false, out var timer, out var label); v.patience = timer; v.timerText = label;
            AddTimer(v.compact.transform, null, true, out timer, out label); v.compactPatience = timer; v.compactTimerText = label;
            Stretch(v.compactLabel.rectTransform, 8, 2, 45, 2); v.compactLabel.fontSize = 15; v.compactLabel.alignment = TextAnchor.MiddleLeft;
            LayoutCompactReceipt(v);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void AddTimer(Transform parent, Image existing, bool compact, out Image fill, out Text value)
    {
        var ring = SaunaFoodArt.Make("timer_ring", 11);
        var area = Rect("CircularTimer", parent); float size = compact ? 32 : 54;
        area.anchorMin = area.anchorMax = new Vector2(1, 0); area.pivot = new Vector2(1, 0); area.sizeDelta = Vector2.one * size; area.anchoredPosition = new Vector2(compact ? -5 : -12, compact ? 3 : 13);
        var track = Image("Track", area, SaunaArt.C("C5CAB7"), ring); Stretch(track.rectTransform);
        fill = existing != null ? existing : Image("Remaining", area, SaunaArt.C("4D9161"), ring);
        fill.transform.SetParent(area, false); fill.sprite = ring; Stretch(fill.rectTransform);
        fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360; fill.fillOrigin = 2; fill.fillClockwise = true;
        value = Label("Seconds", area, "100", compact ? 12 : 14, Ink, true); Stretch(value.rectTransform, 3, 3, 3, 3);
    }
    static SaunaCookingView CookingUI(Transform parent, SaunaItemCatalog catalog)
    {
        var modal = Modal("Cooking", parent, out var card); var v = modal.AddComponent<SaunaCookingView>(); v.card = card;
        var title = Label("Title", card, "음식 조리", 28, Cream, true); title.rectTransform.anchorMin = new Vector2(0,1); title.rectTransform.anchorMax = Vector2.one; title.rectTransform.pivot = new Vector2(.5f,1); title.rectTransform.anchoredPosition = new Vector2(0,-20); title.rectTransform.sizeDelta = new Vector2(-40,45);
        v.hands = Label("Hands", card, "", 14, Cream, true); v.hands.rectTransform.anchorMin = new Vector2(0,1); v.hands.rectTransform.anchorMax = Vector2.one; v.hands.rectTransform.pivot = new Vector2(.5f,1); v.hands.rectTransform.anchoredPosition = new Vector2(0,-68); v.hands.rectTransform.sizeDelta = new Vector2(-40,30);
        v.scroll = Scroll("Menu", card); Stretch((RectTransform)v.scroll.transform, 28, 112, 28, 86); v.content = v.scroll.content; Box(v.content,0,0,540,370);
        v.foods = new SaunaFoodButtonView[catalog.cookingMenu.Length];
        for (int i = 0; i < v.foods.Length; i++)
        {
            var button = Button(catalog.cookingMenu[i].id, v.content, catalog.cookingMenu[i].displayName, 0, 164);
            Box((RectTransform)button.transform, (i % 3) * 180, (i / 3) * 120, 164, 108);
            var item = button.gameObject.AddComponent<SaunaFoodButtonView>(); item.item = catalog.cookingMenu[i]; item.button = button;
            item.label = button.GetComponentInChildren<Text>(); Box(item.label.rectTransform, 4, 74, 156, 28); item.label.fontSize = 16;
            item.icon = Image("Icon", button.transform, Color.white, item.item.icon); item.icon.preserveAspect = true; Box(item.icon.rectTransform, 54, 10, 56, 56); v.foods[i] = item;
        }
        v.closeButton = Button("Close", card, "닫기  ·  ESC", 0, 594, true);
        var close = (RectTransform)v.closeButton.transform; close.anchorMin = new Vector2(0,0); close.anchorMax = new Vector2(1,0); close.pivot = new Vector2(.5f,0); close.anchoredPosition = new Vector2(0,20); close.sizeDelta = new Vector2(-56,52);
        modal.SetActive(false); return v;
    }
}
