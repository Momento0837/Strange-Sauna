using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using SuspiciousSauna;

public static partial class SaunaSceneAuthoring
{
    const string Prefabs = "Assets/Prefabs/";
    static Font font;
    static Material material;
    static Color Ink => SaunaArt.C("2B4540");
    static Color Cream => SaunaArt.C("F4E7CD");
    static Color Gold => SaunaArt.C("E6B86E");
    static Sprite Art(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/Art/" + name + ".png").OfType<Sprite>().FirstOrDefault();
    static GameObject Node(string name, Transform parent = null)
    { var go = new GameObject(name); if (parent != null) go.transform.SetParent(parent, false); return go; }
    static Transform Point(string name, Transform parent, Vector2 position)
    { var t = Node(name, parent).transform; t.localPosition = position; return t; }
    static SpriteRenderer Visual(string name, Transform parent, string art, Vector2 position, Vector2 size, int order)
    {
        var go = Node(name, parent); var r = go.AddComponent<SpriteRenderer>(); r.sprite = Art(art);
        if (r.sprite == null) throw new Exception("Sprite asset missing: " + art);
        r.sharedMaterial = material; r.sortingOrder = order; go.transform.localPosition = position;
        Vector2 natural = r.sprite.bounds.size;
        go.transform.localScale = new Vector3(size.x / natural.x, size.y / natural.y, 1);
        return r;
    }
    static BoxCollider2D Obstacle(GameObject go, Vector2 size)
    { var b = go.AddComponent<BoxCollider2D>(); b.size = size; go.AddComponent<SaunaObstacle>(); return b; }
    static GameObject SavePrefab(GameObject go, string name)
    {
        string path = Prefabs + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) { UnityEngine.Object.DestroyImmediate(go); return existing; }
        var asset = PrefabUtility.SaveAsPrefabAsset(go, path); UnityEngine.Object.DestroyImmediate(go); return asset;
    }
    static GameObject Instance(GameObject prefab, Transform parent, string name, Vector2 position)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name; go.transform.localPosition = position; return go;
    }
    static GameObject Character(string name, string art, bool player)
    {
        var root = Node(name); var view = root.AddComponent<SaunaCharacterView>();
        view.sorting = root.AddComponent<SortingGroup>();
        view.body = Visual("Visual", root.transform, art, new Vector2(0, .4f), Vector2.one * (player ? 1.536f : 1.44f), 0);
        view.visual = view.body.transform;
        if (player)
        {
            Visual("SelectionRing", root.transform, "halo", new Vector2(0, -.16f), Vector2.one * 1.344f, -1);
            var tool = Visual("CleaningTool", root.transform, "clean", new Vector2(.55f, .15f), Vector2.one * .768f, 1);
            tool.gameObject.SetActive(false);
        }
        return SavePrefab(root, name);
    }
    static void WorldLabel(Transform parent, string value, Vector2 position, float width = 4)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = Vector3.one * .02f;
        var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace; c.sortingOrder = 450;
        var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(width / .02f, 30);
        var text = Label("Text", rect, value, 14, Cream); Stretch(text.rectTransform);
    }
    [MenuItem("수상한 찜질방/씬 하이어라키 구성")]
    public static void EnsureAuthored()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Sauna.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/Sauna.unity");
        if (UnityEngine.Object.FindFirstObjectByType<SaunaSceneLayout>() != null) { UpgradeFoodService(); SaunaTableBadgeAuthoring.EnsureAuthored(); EnsureRetailAuthored(); EnsureGuestStatusAuthored(); return; }
        Directory.CreateDirectory(Prefabs);
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumGothic.ttf");
        material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SaunaSprites.mat");
        if (font == null || material == null) throw new Exception("Font or material missing. Wait for asset import.");
        var controller = UnityEngine.Object.FindFirstObjectByType<SaunaGame>();
        if (controller == null) controller = Node("GameController").AddComponent<SaunaGame>();
        controller.name = "GameController";
        var world = Node("World"); var layout = world.AddComponent<SaunaSceneLayout>(); var view = world.AddComponent<SaunaWorldView>();
        view.layout = layout;
        SaunaCompactMapUpdate.FloorSprite();
        var floor = Visual("Floor", world.transform, "floor_compact", new Vector2(SaunaMap.Width, SaunaMap.Height) * .5f, new Vector2(SaunaMap.Width, SaunaMap.Height), -1000);
        layout.mapBounds = Node("MapBounds", world.transform).AddComponent<BoxCollider2D>();
        layout.mapBounds.isTrigger = true; layout.mapBounds.size = new Vector2(SaunaMap.Width, SaunaMap.Height); layout.mapBounds.offset = layout.mapBounds.size * .5f;
        var spawns = Node("SpawnPoints", world.transform).transform;
        layout.entrance = Point("Entrance", spawns, SaunaMap.Entrance);
        layout.playerSpawn = Point("PlayerSpawn", spawns, SaunaMap.Start);
        var dining = Node("DiningArea", world.transform).transform;
        var table = Node("Table"); var tv = table.AddComponent<SaunaTableView>();
        tv.tableSprite = Visual("Visual", table.transform, "table", Vector2.zero, Vector2.one * 2.76f, -100);
        Obstacle(table, new Vector2(2.24f, 1.66f));
        for (int i = 0; i < 4; i++)
        {
            tv.seats[i] = Point("Seat_" + (i + 1), table.transform, SaunaMap.Seat(0, i) - SaunaMap.TableCenter(0));
            Visual("ChairVisual", tv.seats[i], "chair", Vector2.zero, Vector2.one * .936f, -200);
        }
        tv.dirt = Visual("Dirt", table.transform, "dirt", Vector2.zero, Vector2.one * 2.16f, -99).gameObject;
        tv.dirt.SetActive(false);
        var tablePrefab = SavePrefab(table, "Table");
        layout.tables = new SaunaTableView[SaunaMap.TableCount];
        for (int i = 0; i < layout.tables.Length; i++)
        {
            var item = Instance(tablePrefab, dining, "Table_" + (i + 1).ToString("00"), SaunaMap.TableCenter(i));
            layout.tables[i] = item.GetComponent<SaunaTableView>(); layout.tables[i].index = i;
            PrefabUtility.RecordPrefabInstancePropertyModifications(layout.tables[i]);
        }
        var counterArea = Node("CounterArea", world.transform).transform;
        string[] names = { "CookingStation", "RegisterStation", "RestStation", "CleaningStation" };
        string[] icons = { "cook", "register", "rest", "clean" };
        layout.stations = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var prop = Node(names[i]);
            if (i < 3) Visual("CounterVisual", prop.transform, "counter", Vector2.zero, new Vector2(5, 3), -90);
            Visual("EquipmentVisual", prop.transform, icons[i], i < 3 ? Vector2.up * .35f : Vector2.zero, Vector2.one * 1.8f, -89);
            Obstacle(prop, i < 3 ? new Vector2(5, 1.8f) : new Vector2(1.2f, 1.2f));
            var stationPrefab = SavePrefab(prop, names[i]);
            var station = Instance(stationPrefab, counterArea, names[i], SaunaMap.Stations[i]);
            layout.stations[i] = station.transform; WorldLabel(station.transform, SaunaMap.StationNames[i], Vector2.up * 1.55f);
        }
        var decor = Node("Decor", world.transform).transform;
        var plant = Node("Plant"); Visual("Visual", plant.transform, "plant", Vector2.zero, Vector2.one * 1.68f, -50);
        var plantPrefab = SavePrefab(plant, "Plant");
        foreach (var p in new[] { new Vector2(1.3f,SaunaMap.Height-2.2f), new Vector2(SaunaMap.Width-1.3f,SaunaMap.Height-2.2f), new Vector2(1.1f,1.6f), new Vector2(SaunaMap.Width-1.1f,1.6f) })
            Instance(plantPrefab, decor, "Plant", p);
        var actors = Node("Actors").transform;
        view.player = Instance(Character("Player", "player", true), actors, "Player", SaunaMap.Start).GetComponent<SaunaCharacterView>();
        view.cleaningTool = view.player.transform.Find("CleaningTool").gameObject;
        view.guestContainer = Node("Guests", actors).transform;
        view.guestPrefabs = new SaunaCharacterView[4];
        for (int i = 0; i < 4; i++) view.guestPrefabs[i] = Character("Guest_" + (i + 1), "guest" + i, false).GetComponent<SaunaCharacterView>();
        var cameras = Node("Cameras").transform;
        view.backgroundCamera = Node("BackgroundCamera", cameras).AddComponent<Camera>();
        view.backgroundCamera.clearFlags = CameraClearFlags.SolidColor; view.backgroundCamera.backgroundColor = Ink;
        view.backgroundCamera.cullingMask = 0; view.backgroundCamera.depth = -10;
        view.playCamera = Node("PlayCamera", cameras).AddComponent<Camera>();
        view.playCamera.tag = "MainCamera"; view.playCamera.orthographic = true; view.playCamera.orthographicSize = 8.6f;
        Vector2 cameraStart = SaunaMap.ClampCamera(SaunaMap.Start);
        view.playCamera.transform.position = new Vector3(cameraStart.x, cameraStart.y, -10); view.playCamera.backgroundColor = Ink;
        view.playCamera.clearFlags = CameraClearFlags.SolidColor; view.playCamera.gameObject.AddComponent<AudioListener>();
        var audio = controller.GetComponent<SaunaAudio>() ?? controller.gameObject.AddComponent<SaunaAudio>();
        audio.music = Node("Music",controller.transform).AddComponent<AudioSource>(); audio.music.playOnAwake=false;
        audio.effects = Node("Effects",controller.transform).AddComponent<AudioSource>(); audio.effects.playOnAwake=false;
        var ui = CreateUI(view.playCamera);
        var so = new SerializedObject(controller);
        so.FindProperty("worldView").objectReferenceValue = view;
        so.FindProperty("userInterface").objectReferenceValue = ui;
        so.FindProperty("sound").objectReferenceValue = audio; so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller); EditorUtility.SetDirty(layout); EditorUtility.SetDirty(view); EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = world;
        SceneView.lastActiveSceneView?.LookAt(new Vector3(SaunaMap.Width * .5f, SaunaMap.Height * .5f, 0), Quaternion.identity, 16, true);
        UpgradeFoodService();
        SaunaTableBadgeAuthoring.EnsureAuthored();
        EnsureRetailAuthored();
        EnsureGuestStatusAuthored();
        Debug.Log("SAUNA_HIERARCHY_READY: saved World, " + layout.tables.Length + " table prefab instances, Actors, Cameras, and native Canvas UI.");
    }

    static RectTransform Rect(string name, Transform parent)
    { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform; }
    static void Stretch(RectTransform r, float left = 0, float top = 0, float right = 0, float bottom = 0)
    { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(.5f,.5f); r.offsetMin = new Vector2(left,bottom); r.offsetMax = new Vector2(-right,-top); }
    static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
    { var r = Rect(name,parent); var i = r.gameObject.AddComponent<Image>(); i.color = color; i.sprite = sprite; i.raycastTarget = false; return i; }
    static Text Label(string name, Transform parent, string value, int size, Color color, bool center = false)
    {
        var r = Rect(name,parent); var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = value; t.fontSize = size;
        t.color = color; t.raycastTarget = false; t.alignment = center ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; return t;
    }
    static void Box(RectTransform r, float x, float y, float w, float h) => SaunaUI.Box(r,x,y,w,h);
    static Button Button(string name, Transform parent, string value, float y, float width, bool primary = false)
    {
        var image = Image(name,parent,primary ? Gold : SaunaArt.C("405D53")); image.raycastTarget = true;
        Box(image.rectTransform,0,y,width,52); var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var text = Label("Label",image.transform,value,18,primary ? Ink : Cream,true); Stretch(text.rectTransform,8,4,8,4);
        return button;
    }
    static ScrollRect Scroll(string name, Transform parent)
    {
        var r = Rect(name,parent); var scroll = r.gameObject.AddComponent<ScrollRect>();
        var viewport = Rect("Viewport",r); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>(); hit.color = new Color(0,0,0,0); hit.raycastTarget = true;
        var content = Rect("Content",viewport); Box(content,0,0,250,700);
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.scrollSensitivity = 35;
        scroll.movementType = ScrollRect.MovementType.Clamped; return scroll;
    }
    static RectTransform Panel(string name, Transform parent, Color color)
    { var image = Image(name,parent,color); Stretch(image.rectTransform); return image.rectTransform; }
    static GameObject Modal(string name, Transform parent, out RectTransform card)
    {
        var veil = Panel(name,parent,new Color(.03f,.08f,.07f,.82f)); veil.GetComponent<Image>().raycastTarget = true;
        card = Panel("Card",veil,Ink); Box(card,480,255,480,390); return veil.gameObject;
    }
    static SaunaUI CreateUI(Camera playCamera)
    {
        var root = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var ui = root.AddComponent<SaunaUI>(); ui.canvas = canvas;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 800);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var eventSystem = Node("EventSystem"); eventSystem.AddComponent<EventSystem>(); eventSystem.AddComponent<StandaloneInputModule>();
        var hud = Rect("HUD",root.transform); Stretch(hud); ui.hud = hud.gameObject;
        ui.playFrame = Rect("SquarePlayBorder",hud); Box(ui.playFrame,313,43,814,814);
        var line = Image("Top",ui.playFrame,Gold); line.rectTransform.anchorMin=new Vector2(0,1); line.rectTransform.anchorMax=Vector2.one; line.rectTransform.sizeDelta=new Vector2(0,2);
        line = Image("Bottom",ui.playFrame,Gold); line.rectTransform.anchorMin = Vector2.zero; line.rectTransform.anchorMax = new Vector2(1,0); line.rectTransform.sizeDelta = new Vector2(0,2);
        line = Image("Left",ui.playFrame,Gold); line.rectTransform.anchorMin = Vector2.zero; line.rectTransform.anchorMax = new Vector2(0,1); line.rectTransform.sizeDelta = new Vector2(2,0);
        line = Image("Right",ui.playFrame,Gold); line.rectTransform.anchorMin = new Vector2(1,0); line.rectTransform.anchorMax = Vector2.one; line.rectTransform.sizeDelta = new Vector2(2,0);
        ui.leftRail = Rect("Orders",hud); Box(ui.leftRail,12,32,289,815);
        ui.receiptHeading = Label("Heading",ui.leftRail,"받은 주문  00",17,Cream); Box(ui.receiptHeading.rectTransform,0,0,280,26);
        ui.receiptsScroll = Scroll("ReceiptList",ui.leftRail); Stretch((RectTransform)ui.receiptsScroll.transform,0,35,0,0);
        ui.emptyOrders = Label("Empty",ui.receiptsScroll.viewport,"아직 받은 주문이 없어요.",15,Cream); Box(ui.emptyOrders.rectTransform,5,20,240,60);
        ui.rightRail = Rect("Status",hud); Box(ui.rightRail,1145,60,275,780);
        ui.statusScroll = Scroll("StatusList",ui.rightRail); Stretch((RectTransform)ui.statusScroll.transform);
        var content = ui.statusScroll.content; Box(content,0,0,250,470);
        var text = Label("Title",content,"수상한 찜질방",17,Cream); Stretch(text.rectTransform,0,0,0,440);
        string[] stats = { "대기 테이블", "응대 완료", "돌아간 일행" };
        Text[] values = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            text = Label("StatLabel_" + i,content,stats[i],14,Cream); Box(text.rectTransform,0,65+i*95,230,25);
            values[i] = Label("StatValue_" + i,content,"0",30,Gold); Box(values[i].rectTransform,0,94+i*95,230,48);
        }
        ui.waitingText=values[0]; ui.completedText=values[1]; ui.failedText=values[2];
        ui.toolText=Label("Tool",content,"청소 도구는 카운터 오른쪽",14,Cream); Box(ui.toolText.rectTransform,0,375,230,70);
        ui.starRow=Rect("Rating",hud); Box(ui.starRow,608,0,224,38); ui.starFills=new Image[5];
        for(int i=0;i<5;i++)
        {
            var bg=Image("Star_"+(i+1),ui.starRow,SaunaArt.C("607469"),Art("star")); Box(bg.rectTransform,i*32,2,32,32);
            var fill=Image("Fill",bg.transform,Gold,Art("star")); Stretch(fill.rectTransform); fill.type=UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillAmount=1; ui.starFills[i]=fill;
        }
        ui.ratingText=Label("Value",ui.starRow,"5.00",18,Cream); Box(ui.ratingText.rectTransform,170,6,65,28);
        ui.footer=Rect("Footer",hud); Box(ui.footer,319,859,802,37);
        ui.footerText=Label("Message",ui.footer,"WASD / 방향키  이동     F  상호작용     ESC  일시정지",17,Cream,true); Stretch(ui.footerText.rectTransform);
        ui.overlay=Rect("WorldIndicators",hud); Stretch(ui.overlay);
        var bubble=Rect("CustomerBubble",null); var bv=bubble.gameObject.AddComponent<SaunaBubbleView>(); bv.rect=bubble;
        bv.background=bubble.gameObject.AddComponent<Image>(); bv.background.color=Cream; bv.background.raycastTarget=false; Box(bubble,0,0,34,34);
        bv.label=Label("Symbol",bubble,"!",24,Ink,true); Stretch(bv.label.rectTransform);
        bv.direction=Label("Direction",bubble,"",18,Cream,true); Box(bv.direction.rectTransform,0,34,34,22);
        var bubblePrefab=SavePrefab(bubble.gameObject,"CustomerBubble");
        ui.bubbles=new SaunaBubbleView[SaunaMap.TableCount];
        for(int i=0;i<ui.bubbles.Length;i++) { var item=Instance(bubblePrefab,ui.overlay,"Bubble_Table_"+(i+1).ToString("00"),Vector2.zero); ui.bubbles[i]=item.GetComponent<SaunaBubbleView>(); item.SetActive(false); }
        var hint=Image("InteractionHint",playCamera.transform,Ink); ui.interactionHint=hint.rectTransform;
        hint.rectTransform.sizeDelta = new Vector2(240, 38);
        var hintCanvas = hint.gameObject.AddComponent<Canvas>();
        hintCanvas.renderMode = RenderMode.WorldSpace; hintCanvas.worldCamera = playCamera; hintCanvas.sortingOrder = 200;
        hint.rectTransform.localPosition = new Vector3(0, -1, 10);
        hint.rectTransform.localScale = Vector3.one * .024f;
        ui.interactionText=Label("Text",hint.transform,"F  상호작용",20,Cream,true); Stretch(ui.interactionText.rectTransform,4,2,4,5);
        ui.cleaningTrack = Image("CleaningTrack", hint.transform, Ink).rectTransform;
        ui.cleaningTrack.anchorMin = Vector2.zero; ui.cleaningTrack.anchorMax = Vector2.right;
        ui.cleaningTrack.pivot = new Vector2(.5f, 1); ui.cleaningTrack.anchoredPosition = new Vector2(0, -3);
        ui.cleaningTrack.sizeDelta = new Vector2(0, 14);
        ui.cleaningProgress = Image("CleaningProgress", ui.cleaningTrack, Gold);
        Stretch(ui.cleaningProgress.rectTransform, 0, 2, 0, 2);
        ui.cleaningProgress.rectTransform.anchorMax = Vector2.up;
        ui.cleaningTrack.gameObject.SetActive(false); hint.gameObject.SetActive(false);
        ui.receiptPrefab=CreateReceipt();
        ui.receiptPreview=Instance(ui.receiptPrefab.gameObject,ui.receiptsScroll.content,"ReceiptPreview_EditMode",Vector2.zero);
        ui.receiptPreview.tag="EditorOnly"; ui.receiptPreview.SetActive(false);

        var menu=Panel("MainMenu",root.transform,SaunaArt.C("233D37")); ui.mainMenu=menu.gameObject;
        ui.menuLeft=Rect("MenuContent",menu); Box(ui.menuLeft,144,90,420,740);
        text=Label("Eyebrow",ui.menuLeft,"밤이 깊어도, 온기는 남아있으니까.",14,Cream); Box(text.rectTransform,0,0,420,28);
        text=Label("Title",ui.menuLeft,"수상한\n찜질방",68,Cream); Box(text.rectTransform,0,45,420,180);
        text=Label("Subtitle",ui.menuLeft,"평범한 주문, 조금 이상한 손님.\n오늘도 어서 오세요.",17,Cream); Box(text.rectTransform,0,265,420,60);
        ui.menuButtonsScroll=Scroll("Buttons",ui.menuLeft); Stretch((RectTransform)ui.menuButtonsScroll.transform,0,355,0,0);
        Box(ui.menuButtonsScroll.content,0,0,400,214);
        ui.startButton=Button("Start",ui.menuButtonsScroll.content,"영업하기  →",0,400,true);
        ui.settingsButton=Button("Settings",ui.menuButtonsScroll.content,"설정",70,400);
        ui.quitButton=Button("Quit",ui.menuButtonsScroll.content,"종료",140,400);
        foreach(var button in new[]{ui.startButton,ui.settingsButton,ui.quitButton}) { var r=(RectTransform)button.transform; r.anchorMax=new Vector2(1,1); r.sizeDelta=new Vector2(0,52); }
        ui.menuArt=Panel("MenuIllustration",menu,SaunaArt.C("AAB8A6")); Box(ui.menuArt,792,169,562,562);
        var tile=Image("Floor",ui.menuArt,Color.white,Art("floor")); Stretch(tile.rectTransform);
        var sign=Image("Sign",ui.menuArt,Ink); sign.rectTransform.anchorMin=new Vector2(0,.77f); sign.rectTransform.anchorMax=Vector2.one; sign.rectTransform.offsetMin=sign.rectTransform.offsetMax=Vector2.zero;
        text=Label("Name",sign.transform,"수 상 한   찜 질 방",22,Cream,true); Stretch(text.rectTransform);
        MenuImage(ui.menuArt,"Table","table",.21f,.21f,.79f,.65f);
        MenuImage(ui.menuArt,"GuestA","guest1",.01f,.2f,.31f,.56f);
        MenuImage(ui.menuArt,"GuestB","guest2",.68f,.2f,.98f,.56f);
        MenuImage(ui.menuArt,"Player","player",.31f,.01f,.69f,.42f);

        ui.pausePanel=Modal("Pause",root.transform,out ui.pauseCard); ui.pauseContent=Rect("Content",ui.pauseCard); Box(ui.pauseContent,0,0,480,370);
        text=Label("Title",ui.pauseContent,"잠깐 쉬어가기",30,Cream,true); Box(text.rectTransform,25,30,430,50);
        ui.resumeButton=Button("Resume",ui.pauseContent,"계속 영업하기",140,410,true); ui.resumeButton.transform.localPosition+=Vector3.right*35;
        ui.pauseSettingsButton=Button("Settings",ui.pauseContent,"설정",210,410); ui.pauseSettingsButton.transform.localPosition+=Vector3.right*35;
        ui.menuButton=Button("MainMenu",ui.pauseContent,"메인메뉴",280,410); ui.menuButton.transform.localPosition+=Vector3.right*35;
        ui.settingsPanel=Modal("Settings",root.transform,out ui.settingsCard);
        text=Label("Title",ui.settingsCard,"설정",30,Cream,true); Stretch(text.rectTransform,20,20,20,440);
        ui.settingsScroll=Scroll("SettingsList",ui.settingsCard); Stretch((RectTransform)ui.settingsScroll.transform,28,85,28,85);
        ui.settingsContent=ui.settingsScroll.content; Box(ui.settingsContent,0,0,520,360);
        ui.bgmText=Label("MusicLabel",ui.settingsContent,"배경음",18,Cream); Box(ui.bgmText.rectTransform,0,0,500,30);
        ui.musicSlider=Slider("Music",ui.settingsContent,40);
        ui.sfxText=Label("EffectsLabel",ui.settingsContent,"효과음",18,Cream); Box(ui.sfxText.rectTransform,0,95,500,30);
        ui.effectsSlider=Slider("Effects",ui.settingsContent,135);
        text=Label("UIScaleLabel",ui.settingsContent,"UI 크기",18,Cream); Box(text.rectTransform,0,190,500,28);
        var drop=DefaultControls.CreateDropdown(new DefaultControls.Resources()); drop.name="UIScale"; drop.transform.SetParent(ui.settingsContent,false);
        ui.uiScaleDropdown=drop.GetComponent<Dropdown>(); ui.uiScaleDropdown.ClearOptions(); ui.uiScaleDropdown.AddOptions(new[]{"100%","200%","300%"}.ToList());
        Box((RectTransform)drop.transform,0,230,500,44); ((RectTransform)drop.transform).anchorMax=new Vector2(1,1); ((RectTransform)drop.transform).sizeDelta=new Vector2(-10,44);
        foreach(var t in drop.GetComponentsInChildren<Text>(true)) { t.font=font; t.fontSize=18; t.color=Ink; }
        ui.scaleHint=Label("ScaleHint",ui.settingsContent,"UI 크기는 메인메뉴에서 변경할 수 있어요.",13,Cream); Box(ui.scaleHint.rectTransform,0,290,500,55);
        ui.settingsBackButton=Button("Back",ui.settingsCard,"돌아가기",440,544,true);
        var back=(RectTransform)ui.settingsBackButton.transform; back.anchorMin=new Vector2(0,0); back.anchorMax=new Vector2(1,0); back.pivot=new Vector2(.5f,0); back.anchoredPosition=new Vector2(0,20); back.sizeDelta=new Vector2(-56,52);
        ui.gameOverPanel=Modal("GameOver",root.transform,out ui.gameOverCard); ui.gameOverContent=Rect("Content",ui.gameOverCard); Box(ui.gameOverContent,0,0,520,380);
        text=Label("Title",ui.gameOverContent,"영업 종료",34,Cream,true); Box(text.rectTransform,20,55,480,60);
        ui.gameOverText=Label("Summary",ui.gameOverContent,"별점 1.0",17,Cream,true); Box(ui.gameOverText.rectTransform,20,135,480,50);
        ui.restartButton=Button("Restart",ui.gameOverContent,"다시 시작",220,450,true); ui.restartButton.transform.localPosition+=Vector3.right*35;
        ui.gameOverMenuButton=Button("MainMenu",ui.gameOverContent,"메인메뉴",290,450); ui.gameOverMenuButton.transform.localPosition+=Vector3.right*35;
        ui.PreviewPlay();
        return ui;
    }
    static void MenuImage(Transform parent,string name,string art,float x0,float y0,float x1,float y1)
    {
        var image=Image(name,parent,Color.white,Art(art)); image.preserveAspect=true;
        image.rectTransform.anchorMin=new Vector2(x0,y0); image.rectTransform.anchorMax=new Vector2(x1,y1); image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero;
    }
    static Slider Slider(string name,Transform parent,float y)
    {
        var go=DefaultControls.CreateSlider(new DefaultControls.Resources()); go.name=name; go.transform.SetParent(parent,false);
        var r=(RectTransform)go.transform; Box(r,0,y,500,30); r.anchorMax=new Vector2(1,1); r.sizeDelta=new Vector2(-10,30);
        var slider=go.GetComponent<Slider>(); slider.minValue=0; slider.maxValue=1;
        foreach(var image in go.GetComponentsInChildren<Image>()) image.color=image.name=="Fill"?Gold:Cream;
        return slider;
    }
    static SaunaReceiptView CreateReceipt()
    {
        const string paperPath="Assets/Art/ReceiptPaper.png";
        if(!File.Exists(paperPath))
        {
            var texture=new Texture2D(64,128,TextureFormat.RGBA32,false);
            var pixels=new Color[64*128];
            for(int y=0;y<128;y++) for(int x=0;x<64;x++) pixels[y*64+x]=y<Mathf.Abs(x%12-6)?Color.clear:Color.white;
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(paperPath,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(paperPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(paperPath); importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single; importer.spriteBorder=new Vector4(6,8,6,6); importer.filterMode=FilterMode.Point; importer.SaveAndReimport();
        }
        var paper=Image("Receipt",null,Cream,AssetDatabase.LoadAssetAtPath<Sprite>(paperPath));
        paper.type=UnityEngine.UI.Image.Type.Sliced; Box(paper.rectTransform,0,0,270,230);
        var view=paper.gameObject.AddComponent<SaunaReceiptView>(); view.rect=paper.rectTransform;
        var normal=Rect("Normal",paper.transform); Stretch(normal); view.normal=normal.gameObject;
        view.tableLabel=Label("Table",normal,"TABLE 01",14,Ink); Box(view.tableLabel.rectTransform,12,12,240,25);
        view.code=Label("OrderCode",normal,"1",46,Ink,true); Stretch(view.code.rectTransform,12,42,12,112);
        view.title=Label("Title",normal,"손님의 부탁",14,Ink,true); Stretch(view.title.rectTransform,12,125,12,80);
        view.description=Label("Description",normal,"",12,Ink); Stretch(view.description.rectTransform,12,155,12,42);
        view.status=Label("Status",normal,"100초 · F로 완료",12,Ink,true); Stretch(view.status.rectTransform,12,200,12,5);
        view.patience=Image("Patience",normal,SaunaArt.C("6C8E76")); view.patience.type=UnityEngine.UI.Image.Type.Filled; view.patience.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        var bar=view.patience.rectTransform; bar.anchorMin=new Vector2(0,0); bar.anchorMax=new Vector2(1,0); bar.pivot=new Vector2(.5f,0); bar.anchoredPosition=new Vector2(0,35); bar.sizeDelta=new Vector2(-24,3);
        var compact=Rect("Compact",paper.transform); Stretch(compact); view.compact=compact.gameObject;
        view.compactLabel=Label("Label",compact,"TABLE 01    1",15,Ink,true); Stretch(view.compactLabel.rectTransform,4,1,4,1); compact.gameObject.SetActive(false);
        var prefab=SavePrefab(paper.gameObject,"Receipt"); return prefab.GetComponent<SaunaReceiptView>();
    }
}
