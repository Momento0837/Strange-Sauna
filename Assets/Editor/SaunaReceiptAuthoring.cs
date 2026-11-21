using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SuspiciousSauna;

public static partial class SaunaSceneAuthoring
{
    [MenuItem("수상한 찜질방/영수증 크기와 반 겹침 배치 적용")]
    public static void ApplyCompactReceiptLayout()
    {
        const string path = "Assets/Prefabs/Receipt.prefab";
        const string backup = "Logs/BeforeCompactReceipts";
        Directory.CreateDirectory(backup);
        if (!File.Exists(backup + "/Receipt.prefab")) File.Copy(path, backup + "/Receipt.prefab");
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            LayoutCompactReceipt(root.GetComponent<SaunaReceiptView>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Keep existing fonts and font sizes. Bottom-align the menu so even a one-item
    // order remains visible in the exposed lower half of a stacked receipt.
    static void LayoutCompactReceipt(SaunaReceiptView view)
    {
        Box(view.rect, 0, 0, 230, 210);
        Stretch(view.tableLabel.rectTransform, 10, 10, 55, 176);
        Box(view.code.rectTransform, 0, 10, 45, 25);
        view.code.rectTransform.anchorMin = view.code.rectTransform.anchorMax = new Vector2(1, 1);
        view.code.rectTransform.anchoredPosition = new Vector2(-52, -10);
        Stretch(view.title.rectTransform, 10, 34, 10, 152);
        Stretch(view.description.rectTransform, 10, 58, 10, 64);
        view.description.alignment = TextAnchor.LowerLeft;
        var status = view.status.rectTransform;
        status.anchorMin = Vector2.zero; status.anchorMax = new Vector2(1, 0);
        status.pivot = Vector2.zero; status.anchoredPosition = new Vector2(10, 12);
        status.sizeDelta = new Vector2(-72, 38);
        var timer = (RectTransform)view.timerText.transform.parent;
        timer.sizeDelta = Vector2.one * 44;
        timer.anchoredPosition = new Vector2(-10, 12);
    }
}
