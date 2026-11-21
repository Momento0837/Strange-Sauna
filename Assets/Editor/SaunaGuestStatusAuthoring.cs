using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SuspiciousSauna;

public static partial class SaunaSceneAuthoring
{
    public static void EnsureGuestStatusAuthored()
    {
        var world = Object.FindFirstObjectByType<SaunaWorldView>();
        foreach (var prefab in world.guestPrefabs)
        {
            if (prefab.guestStatus != null) continue;
            string path = AssetDatabase.GetAssetPath(prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var character = root.GetComponent<SaunaCharacterView>();
                var status = root.AddComponent<SaunaGuestStatusView>(); character.guestStatus = status;
                var canvasRoot = new GameObject("GuestStatusCanvas", typeof(RectTransform), typeof(Canvas));
                canvasRoot.transform.SetParent(root.transform, false); canvasRoot.transform.localScale = Vector3.one * .01f;
                ((RectTransform)canvasRoot.transform).sizeDelta = new Vector2(240, 240);
                var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 710;
                var frame = Image("PatienceGauge", canvasRoot.transform, Ink);
                frame.rectTransform.sizeDelta = new Vector2(104, 14); frame.rectTransform.anchoredPosition = new Vector2(0, 142);
                var track = Image("Track", frame.transform, SaunaArt.C("C5CAB7")); Stretch(track.rectTransform, 2, 2, 2, 2);
                var fill = Image("Remaining", frame.transform, SaunaArt.C("4D9161")); Stretch(fill.rectTransform, 2, 2, 2, 2);
                fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0;
                fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                status.patienceGauge = frame.gameObject; status.remaining = fill;
                var bubble = Image("DepartureBubble", canvasRoot.transform, Color.white, SaunaRetailArt.Make("speech_panel", 13));
                bubble.type = UnityEngine.UI.Image.Type.Sliced; bubble.rectTransform.sizeDelta = new Vector2(70, 70); bubble.rectTransform.anchoredPosition = new Vector2(0, 172);
                var dust = Image("Dust", bubble.transform, Color.white, SaunaRetailArt.Make("dust", 11)); dust.preserveAspect = true; Stretch(dust.rectTransform, 10, 10, 10, 10);
                AddSpeechTail(bubble.transform); status.departureBubble = bubble.gameObject;
                status.patienceGauge.SetActive(false); status.departureBubble.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
    }
}
