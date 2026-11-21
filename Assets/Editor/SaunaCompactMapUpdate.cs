using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SuspiciousSauna;

public static class SaunaCompactMapUpdate
{
    [MenuItem("수상한 찜질방/배치 변경/맵 24×23 압축 배치 적용")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Play를 종료한 후 배치를 변경하세요.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Sauna.unity") throw new InvalidOperationException("Sauna 씬을 먼저 여세요.");
        var layout = UnityEngine.Object.FindFirstObjectByType<SaunaSceneLayout>();
        var world = UnityEngine.Object.FindFirstObjectByType<SaunaWorldView>();
        var game = UnityEngine.Object.FindFirstObjectByType<SaunaGame>();
        const string backup = "Logs/BeforeCompactMap";
        Directory.CreateDirectory(backup); EditorSceneManager.SaveScene(scene);
        if (!File.Exists(backup + "/Sauna.unity")) File.Copy(scene.path, backup + "/Sauna.unity");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("맵 24×23 / 테이블 간격 5");
        Undo.RecordObject(layout.mapBounds, "맵 외곽 축소");
        layout.mapBounds.size = new Vector2(SaunaMap.Width, SaunaMap.Height);
        layout.mapBounds.offset = layout.mapBounds.size * .5f;
        foreach (var table in layout.tables) Move(table.transform, SaunaMap.TableCenter(table.index));
        for (int i = 0; i < layout.stations.Length; i++) Move(layout.stations[i], SaunaMap.Stations[i]);
        Move(layout.foodWasteBin, SaunaMap.Stations[0] + Vector2.right * 3.8f);
        Move(layout.entrance, SaunaMap.Entrance); Move(layout.playerSpawn, SaunaMap.Start); Move(world.player.transform, SaunaMap.Start);
        // Queue and staff anchors are children of the POS and move with the counter.
        Move(game.retailLayout.shelfInteraction.parent, SaunaMap.RetailShelf);
        Move(game.retailLayout.overflowLookPoint, layout.mapBounds.offset);
        var decor = layout.transform.Find("Decor");
        var corners = new[] { new Vector2(1.3f, SaunaMap.Height - 2.2f), new Vector2(SaunaMap.Width - 1.3f, SaunaMap.Height - 2.2f),
            new Vector2(1.1f, 1.6f), new Vector2(SaunaMap.Width - 1.1f, 1.6f) };
        if (decor != null) for (int i = 0; i < Mathf.Min(4, decor.childCount); i++) Move(decor.GetChild(i), corners[i]);
        var floor = layout.transform.Find("Floor").GetComponent<SpriteRenderer>();
        Undo.RecordObject(floor, "축소된 맵 바닥"); Undo.RecordObject(floor.transform, "바닥 크기");
        floor.sprite = FloorSprite(); Move(floor.transform, layout.mapBounds.offset);
        floor.transform.localScale = new Vector3(SaunaMap.Width / floor.sprite.bounds.size.x, SaunaMap.Height / floor.sprite.bounds.size.y, 1);
        var cameraPosition = layout.Capture().ClampCamera(SaunaMap.Start, world.cameraHalf);
        Undo.RecordObject(world.playCamera.transform, "카메라 시작 위치");
        world.playCamera.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, -10);
        EditorUtility.SetDirty(layout.mapBounds); EditorUtility.SetDirty(floor);
        Undo.CollapseUndoOperations(group); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        SceneView.lastActiveSceneView?.LookAt(new Vector3(SaunaMap.Width * .5f, SaunaMap.Height * .5f, 0), Quaternion.identity, 16, true);
        File.WriteAllText("Logs/compact-map-update.txt", "Map: 24 x 23 (previous 30 x 34)\nArea: 552 (previous 1020), reduced 45.88%\nTables: 4 x 2; spacing 5 x 5 (previous 6 x 11.5)\nPlayer speed: " + game.tuning.playerSpeed + "\nCounter, entrance, shelf, queue, bounds and camera relocated.\n");
    }
    static void Move(Transform target, Vector2 position)
    {
        Undo.RecordObject(target, "배치 이동"); target.position = new Vector3(position.x, position.y, target.position.z);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
    public static Sprite FloorSprite()
    {
        const string path = "Assets/Art/floor_compact.png";
        if (!File.Exists(path))
        {
            File.WriteAllBytes(path, SaunaArt.Get("floor").texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 20; importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
