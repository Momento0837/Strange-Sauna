using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SuspiciousSauna;

// Explicit editor migration. Normal builds never reapply positions over authored edits.
public static class SaunaLayoutUpdate
{
    [MenuItem("수상한 찜질방/배치 변경/가로 4 세로 2 적용")]
    public static void ApplyFourByTwo()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Play를 종료한 후 배치를 변경하세요.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Sauna.unity") throw new InvalidOperationException("Sauna 씬을 먼저 여세요.");
        var layout = UnityEngine.Object.FindFirstObjectByType<SaunaSceneLayout>();
        var ui = UnityEngine.Object.FindFirstObjectByType<SaunaUI>();
        var game = UnityEngine.Object.FindFirstObjectByType<SaunaGame>();
        if (layout.tables.Length < SaunaMap.TableCount || ui.bubbles.Length < SaunaMap.TableCount)
            throw new InvalidOperationException("배치할 테이블과 말풍선 참조가 부족합니다.");
        const string backup = "Logs/BeforeFourByTwo";
        Directory.CreateDirectory(backup);
        EditorSceneManager.SaveScene(scene);
        if (!File.Exists(backup + "/Sauna.unity")) File.Copy(scene.path, backup + "/Sauna.unity");
        if (!File.Exists(backup + "/SaunaTuning.asset")) File.Copy(AssetDatabase.GetAssetPath(game.tuning), backup + "/SaunaTuning.asset");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("테이블 4×2 / 이동속도 8.4");
        Undo.RecordObject(layout, "테이블 목록"); Undo.RecordObject(ui, "말풍선 목록"); Undo.RecordObject(game.tuning, "이동속도");
        var keep = layout.tables.OrderBy(t => t.index).Take(SaunaMap.TableCount).ToArray();
        foreach (var table in layout.GetComponentsInChildren<SaunaTableView>(true))
            if (!keep.Contains(table)) Undo.DestroyObjectImmediate(table.gameObject);
        for (int i = 0; i < keep.Length; i++)
        {
            var table = keep[i]; Undo.RecordObject(table.transform, "테이블 배치"); Undo.RecordObject(table, "테이블 ID");
            table.index = i; table.transform.position = SaunaMap.TableCenter(i); table.name = "Table_" + (i + 1).ToString("00");
            var label = table.transform.Find("Label/Text")?.GetComponent<Text>();
            if (label != null) { Undo.RecordObject(label, "테이블 번호"); label.text = (i + 1).ToString("00"); }
            table.RefreshNumber();
            if (table.numberLabel != null) PrefabUtility.RecordPrefabInstancePropertyModifications(table.numberLabel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(table);
            PrefabUtility.RecordPrefabInstancePropertyModifications(table.transform);
        }
        layout.tables = keep;
        var bubbles = ui.bubbles.Take(keep.Length).ToArray();
        foreach (var bubble in ui.overlay.GetComponentsInChildren<SaunaBubbleView>(true))
            if (!bubbles.Contains(bubble)) Undo.DestroyObjectImmediate(bubble.gameObject);
        ui.bubbles = bubbles;
        game.tuning.playerSpeed = 8.4f;
        EditorUtility.SetDirty(layout); EditorUtility.SetDirty(ui); EditorUtility.SetDirty(game.tuning);
        Undo.CollapseUndoOperations(group); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        var points = layout.Capture().tables;
        File.WriteAllText("Logs/layout-update.txt", "Tables: " + points.Length + "\nColumns: " + points.Select(p => p.x).Distinct().Count() + "\nRows: " + points.Select(p => p.y).Distinct().Count() + "\nPlayer speed: " + game.tuning.playerSpeed + "\n" + string.Join("\n", points.Select((p,i) => (i + 1).ToString("00") + ": " + p)));
    }
}
