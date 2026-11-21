using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SuspiciousSauna;

public static class SaunaTableBadgeAuthoring
{
    const string PrefabPath = "Assets/Prefabs/Table.prefab";
    const string SpritePath = "Assets/Art/TableNumberBadge.png";
    public static void EnsureAuthored()
    {
        var layout = Object.FindFirstObjectByType<SaunaSceneLayout>();
        if (layout == null) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var prefabView = prefab.GetComponent<SaunaTableView>();
        bool needsBadge = prefabView.numberBadge == null || prefabView.numberLabel == null;
        bool needsScene = System.Array.Exists(layout.tables, t => t.transform.Find("Label/Text") != null || t.numberLabel == null);
        if (!needsBadge && !needsScene) return;
        const string backup = "Logs/BeforeTableBadges";
        Directory.CreateDirectory(backup);
        if (!File.Exists(backup + "/Table.prefab")) File.Copy(PrefabPath, backup + "/Table.prefab");
        EditorSceneManager.SaveScene(layout.gameObject.scene);
        if (!File.Exists(backup + "/Sauna.unity")) File.Copy(layout.gameObject.scene.path, backup + "/Sauna.unity");
        if (needsBadge)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<SaunaTableView>();
                var badge = new GameObject("NumberBadge"); badge.transform.SetParent(root.transform, false);
                badge.transform.localPosition = new Vector3(1.12f, 1.28f, 0);
                badge.transform.localScale = Vector3.one * .8f;
                view.numberBadge = badge.AddComponent<SpriteRenderer>();
                view.numberBadge.sprite = CircleSprite();
                view.numberBadge.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SaunaSprites.mat");
                view.numberBadge.sortingOrder = 650;
                var labelRoot = new GameObject("NumberCanvas", typeof(RectTransform), typeof(Canvas));
                labelRoot.transform.SetParent(badge.transform, false); labelRoot.transform.localScale = Vector3.one * .01f;
                var canvas = labelRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 651;
                ((RectTransform)labelRoot.transform).sizeDelta = new Vector2(100, 100);
                var label = new GameObject("Number", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                label.transform.SetParent(labelRoot.transform, false);
                var rect = (RectTransform)label.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                view.numberLabel = label.GetComponent<Text>();
                view.numberLabel.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NanumGothic.ttf");
                view.numberLabel.fontSize = 52; view.numberLabel.fontStyle = FontStyle.Bold;
                view.numberLabel.alignment = TextAnchor.MiddleCenter; view.numberLabel.color = SaunaArt.C("2B4540");
                view.numberLabel.raycastTarget = false; view.numberLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                view.numberLabel.verticalOverflow = VerticalWrapMode.Overflow; view.RefreshNumber();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var table in layout.tables)
        {
            var oldLabel = table.transform.Find("Label/Text");
            if (oldLabel != null) Undo.DestroyObjectImmediate(oldLabel.parent.gameObject);
            table.RefreshNumber();
            if (table.numberLabel == null) throw new System.InvalidOperationException(table.name + " 번호판 프리팹 참조를 확인하세요.");
            EditorUtility.SetDirty(table.numberLabel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(table.numberLabel);
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(layout.gameObject.scene); EditorSceneManager.SaveScene(layout.gameObject.scene);
    }
    static Sprite CircleSprite()
    {
        if (!File.Exists(SpritePath))
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Color border = SaunaArt.C("2B4540"), fill = SaunaArt.C("F4E7CD");
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * 64);
                var color = Color.Lerp(fill, border, Mathf.Clamp01(distance - 57));
                color.a = Mathf.Clamp01(62 - distance); pixels[y * size + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(SpritePath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128; importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
