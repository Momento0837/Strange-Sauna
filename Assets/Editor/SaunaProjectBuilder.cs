using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SuspiciousSauna;

public static class SaunaProjectBuilder
{
    [MenuItem("수상한 찜질방/프로젝트 준비 및 검사")]
    public static void Prepare()
    {
        Directory.CreateDirectory("Assets/Resources"); Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Art");
        var quests = AssetDatabase.LoadAssetAtPath<QuestCatalog>("Assets/Resources/QuestCatalog.asset");
        if (quests == null) { quests = ScriptableObject.CreateInstance<QuestCatalog>(); quests.FillDefaults(); AssetDatabase.CreateAsset(quests, "Assets/Resources/QuestCatalog.asset"); }
        var tuning = AssetDatabase.LoadAssetAtPath<SaunaTuning>("Assets/Resources/SaunaTuning.asset");
        if (tuning == null) { tuning = ScriptableObject.CreateInstance<SaunaTuning>(); AssetDatabase.CreateAsset(tuning, "Assets/Resources/SaunaTuning.asset"); }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
        if (pipeline == null) throw new Exception("URP asset missing");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
        QualitySettings.vSyncCount = 1;
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SaunaSprites.mat");
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new Exception("2D URP sprite shader missing");
            mat = new Material(shader); AssetDatabase.CreateAsset(mat, "Assets/Art/SaunaSprites.mat");
        }
        // Export the authored textures as editable reference assets too.
        foreach (string name in new[] { "floor", "table", "chair", "counter", "cook", "register", "rest", "clean", "plant", "dirt", "halo", "star", "player", "guest0", "guest1", "guest2", "guest3" })
        {
            string path = "Assets/Art/" + name + ".png";
            if (!File.Exists(path)) File.WriteAllBytes(path, SaunaArt.Get(name).texture.EncodeToPNG());
        }
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles("Assets/Art", "*.png"))
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;
            importer.textureType = TextureImporterType.Sprite; importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        const string scenePath = "Assets/Scenes/Sauna.unity";
        if (!File.Exists(scenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("수상한 찜질방 · 게임").AddComponent<SaunaGame>();
            game.quests = quests; game.tuning = tuning; game.spriteMaterial = mat;
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        else if (EditorSceneManager.GetActiveScene().path != scenePath)
        {
            if (EditorSceneManager.GetActiveScene().isDirty) throw new Exception("현재 씬을 저장한 후 빌드하세요.");
            EditorSceneManager.OpenScene(scenePath);
        }
        SaunaSceneAuthoring.EnsureAuthored();
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        PlayerSettings.companyName = "Momen Studio"; PlayerSettings.productName = "수상한 찜질방";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
        Validate();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("SAUNA_PREPARE_OK · Unity " + Application.unityVersion + " · URP 2D");
    }

    [MenuItem("수상한 찜질방/검사 실행")]
    public static void Validate()
    {
        int checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("VALIDATION FAILED: " + label); checks++; }
        Check(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset, "URP active");
        var quests = AssetDatabase.LoadAssetAtPath<QuestCatalog>("Assets/Resources/QuestCatalog.asset");
        Check(quests.numbers.Length == 20 && quests.letters.Length == 16, "36 quest slots");
        Check(quests.numbers.Select(q => q.id).SequenceEqual(Enumerable.Range(1, 20).Select(i => i.ToString())), "number IDs");
        Check(quests.letters.Select(q => q.id).SequenceEqual(Enumerable.Range(0, 16).Select(i => ((char)('a' + i)).ToString())), "letter IDs");
        Check(QuestCatalog.NumberChance(3.1f) == .7f && QuestCatalog.NumberChance(3) == .5f && QuestCatalog.NumberChance(2.1f) == .5f && QuestCatalog.NumberChance(2) == .2f && QuestCatalog.NumberChance(1.1f) == .2f, "rating thresholds");
        foreach (float rating in new[] { 5f, 3.1f, 3f, 2.1f, 2f, 1.1f })
        {
            UnityEngine.Random.InitState(12345); int number = 0;
            for (int n = 0; n < 10000; n++) if (int.TryParse(quests.Draw(rating).id, out _)) number++;
            Check(Mathf.Abs(number / 10000f - QuestCatalog.NumberChance(rating)) < .025f, "probability " + rating);
        }
        var layout = UnityEngine.Object.FindFirstObjectByType<SaunaSceneLayout>();
        Check(layout != null, "serialized scene layout exists");
        var map = layout.Capture();
        var nav = new SaunaNavigation(map);
        Check(map.tables.Length == SaunaMap.TableCount && layout.GetComponentsInChildren<SaunaTableView>(true).Length == SaunaMap.TableCount, "8 actual table objects and matching scene references");
        for (int table = 0; table < map.tables.Length; table++) for (int seat = 0; seat < 4; seat++)
        {
            var destination = map.seats[table][seat];
            var path = nav.Path(map.entrance, destination, table * 4 + seat);
            Check(path.Count > 0 && Vector2.Distance(path[path.Count - 1], destination) < .01f, "entry path " + table + "/" + seat);
            Check(path.All(p => nav.Walkable(p)), "path obstacle clearance " + table + "/" + seat);
            var leave = nav.Path(destination, map.entrance, table * 4 + seat + 197);
            Check(leave.Count > 0 && Vector2.Distance(leave[leave.Count - 1], map.entrance) < .01f, "exit path " + table + "/" + seat);
        }
        for (float y = map.bounds.yMin + 1; y < map.bounds.yMax - 2; y += .5f) { Check(nav.Walkable(new Vector2(map.bounds.xMin + 1, y)), "left boundary passage"); Check(nav.Walkable(new Vector2(map.bounds.xMax - 1, y)), "right boundary passage"); }
        foreach (Vector2 size in new[] { new Vector2(1440, 900), new Vector2(900, 1440), new Vector2(900, 900), new Vector2(2560, 1080) })
        {
            Rect rect = SaunaMap.PlayRect(size.x, size.y);
            Check(Mathf.Approximately(rect.width, rect.height) && Mathf.Approximately(rect.width, Mathf.Min(size.x, size.y) * .9f), "90% square " + size);
        }
        foreach (Vector2 p in new[] { map.bounds.min, map.bounds.max, map.bounds.center, new Vector2(map.bounds.xMin + 1, map.bounds.yMax - 1) })
        {
            Vector2 c = map.ClampCamera(p, SaunaMap.ViewHalf);
            Check(c.x - SaunaMap.ViewHalf >= map.bounds.xMin && c.x + SaunaMap.ViewHalf <= map.bounds.xMax + .001f && c.y - SaunaMap.ViewHalf >= map.bounds.yMin && c.y + SaunaMap.ViewHalf <= map.bounds.yMax + .001f, "camera bounds");
        }
        Check(Vector2.Distance(map.ClampCamera(map.bounds.center, SaunaMap.ViewHalf), map.bounds.center) < .001f, "camera re-centers");
        Check(!nav.Walkable(map.tables[0]), "solid authored tables");
        Check(layout.tables.All(t => PrefabUtility.IsPartOfPrefabInstance(t)), "8 editable table prefab instances");
        var view = UnityEngine.Object.FindFirstObjectByType<SaunaWorldView>();
        Check(view.player != null && view.playCamera != null && view.backgroundCamera != null, "player and cameras saved before Play");
        Check(view.guestPrefabs.Length == 4 && view.guestPrefabs.All(p => AssetDatabase.Contains(p)), "guest prefabs are assets");
        Check(layout.tables.All(t => AssetDatabase.Contains(t.tableSprite.sprite)), "table sprites reference imported image assets");
        var ui = UnityEngine.Object.FindFirstObjectByType<SaunaUI>();
        Check(ui != null && ui.canvas != null && ui.startButton != null && ui.uiScaleDropdown != null, "native Canvas controls saved in hierarchy");
        Check(ui.bubbles.Length == map.tables.Length && ui.overlay.GetComponentsInChildren<SaunaBubbleView>(true).Length == map.tables.Length, "one saved indicator per table, no stale indicators");
        Check(ui.receiptPrefab != null && AssetDatabase.Contains(ui.receiptPrefab), "receipt is replaceable prefab");
        Check(ui.cooking != null && ui.cooking.foods.Length == 9 && ui.cooking.foods.All(f => f.item != null && f.item.icon != null), "nine authored food buttons and item assets");
        Check(view.player.rightHand != null && view.player.leftHand != null && view.player.sweatEffect != null, "authored hand anchors and sweat effect");
        Check(layout.foodWasteBin != null && PrefabUtility.IsPartOfPrefabInstance(layout.foodWasteBin), "authored waste bin prefab");
        Check(ui.receiptPrefab.timerText != null && ui.receiptPrefab.patience.fillMethod == UnityEngine.UI.Image.FillMethod.Radial360, "radial receipt timer");
        var catalog = AssetDatabase.LoadAssetAtPath<SaunaItemCatalog>("Assets/Resources/SaunaItemCatalog.asset");
        Check(catalog.cookingMenu.Length == 9 && catalog.cookingMenu.Select(i => i.id).Distinct().Count() == 9, "nine unique menu items");
        var inventory = new HandInventory();
        var futureItem = ScriptableObject.CreateInstance<SaunaItemDefinition>(); futureItem.category = ItemCategory.Hygiene;
        try { Check(inventory.TryAdd(futureItem) && inventory.Right == futureItem, "inventory accepts future non-food items"); }
        finally { UnityEngine.Object.DestroyImmediate(futureItem); }
        inventory.Clear(); inventory.TryAdd(catalog.cookingMenu[0]); inventory.TryAdd(catalog.cookingMenu[0]);
        var duplicate = new TableFoodOrder(); duplicate.Add(catalog.cookingMenu[0]);
        Check(duplicate.Deliver(inventory) == 1 && inventory.Right == null && inventory.Left != null, "delivery consumes only outstanding quantity and preserves other hand");
        Check(SaunaReceiptView.TimerColor(.51f) != SaunaReceiptView.TimerColor(.50f) && SaunaReceiptView.TimerColor(.50f) == SaunaReceiptView.TimerColor(.31f) && SaunaReceiptView.TimerColor(.30f) == SaunaReceiptView.TimerColor(.01f), "timer color thresholds 51 50 31 30 1 percent");
        var original = layout.tables[0].transform.position;
        try
        {
            layout.tables[0].transform.position += Vector3.up * .5f;
            var moved = layout.Capture();
            Check(Vector2.Distance(moved.tables[0], map.tables[0] + Vector2.up * .5f) < .001f, "moving table updates gameplay position");
            Check(Vector2.Distance(moved.seats[0][0], map.seats[0][0] + Vector2.up * .5f) < .001f, "moving table carries seat anchors");
            Check(!new SaunaNavigation(moved).Walkable(moved.tables[0]), "moving table updates navigation obstacle");
        }
        finally { layout.tables[0].transform.position = original; }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/editor-validation.txt", checks + " checks passed.\nUnity " + Application.unityVersion + "\nURP 2D active; " + (map.tables.Length * 4) + " seats have entering and leaving paths; thresholds and distributions valid; camera and viewport valid.\n");
        Debug.Log("SAUNA_VALIDATION_OK " + checks + " checks");
        SaunaSceneAuthoring.ValidateRetailAuthoring(UnityEngine.Object.FindFirstObjectByType<SaunaGame>());
    }

    [MenuItem("수상한 찜질방/Windows 빌드")]
    public static void Build() => BuildPlayer(false);

    [MenuItem("수상한 찜질방/테스트용 빌드")]
    public static void BuildQA() => BuildPlayer(true);

    public static void BuildAll()
    {
        BuildPlayer(true);
        BuildPlayer(false);
    }

    static void BuildPlayer(bool development)
    {
        Prepare();
        string folder = development ? "Builds/QA" : "Builds/Windows";
        Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Sauna.unity" },
            locationPathName = folder + "/수상한 찜질방.exe", target = BuildTarget.StandaloneWindows64,
            // Local diagnostic flags are in our code; Unity network debugging stays disabled.
            options = BuildOptions.None
        });
        File.WriteAllText(development ? "Logs/qa-build-result.txt" : "Logs/build-result.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\nBytes: " + report.summary.totalSize);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
        File.Copy("Assets/Fonts/OFL.txt", folder + "/NanumGothic-LICENSE.txt", true);
        Debug.Log("SAUNA_BUILD_OK");
    }
}
