using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// An explicit workspace request lets the already-open editor author/build without launching
// a second Unity process or closing the user's editor. No request file means no work.
[InitializeOnLoad]
public static class SaunaEditorRequests
{
    static SaunaEditorRequests() { EditorApplication.update += Poll; }
    static void Poll()
    {
        const string request = "Logs/hierarchy-request.txt";
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
        string command = File.ReadAllText(request).Trim(); File.Delete(request);
        try
        {
            if (command == "refresh")
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset("Assets/Scripts", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                AssetDatabase.ImportAsset("Assets/Editor", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                File.WriteAllText("Logs/hierarchy-result.txt", "REFRESH REQUESTED " + DateTime.UtcNow.ToString("O")); return;
            }
            if (command == "layout4x2") { SaunaLayoutUpdate.ApplyFourByTwo(); SaunaSceneAuthoring.EnsureAuthored(); SaunaProjectBuilder.Validate(); }
            else if (command == "compactmap") { SaunaCompactMapUpdate.Apply(); SaunaProjectBuilder.Validate(); }
            else if (command == "receiptlayout") SaunaSceneAuthoring.ApplyCompactReceiptLayout();
            else if (command == "build") SaunaProjectBuilder.BuildAll();
            else { SaunaSceneAuthoring.EnsureAuthored(); SaunaProjectBuilder.Validate(); }
            File.WriteAllText("Logs/hierarchy-result.txt", "SUCCESS " + command + "\n" + DateTime.UtcNow.ToString("O"));
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        catch(Exception error) { Debug.LogException(error); File.WriteAllText("Logs/hierarchy-result.txt", "FAILED " + error); }
    }
}
