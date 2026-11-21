using System.IO;
using UnityEditor;
using UnityEngine;
// Small placeholder icons are authored into real PNG assets, never rebuilt during Play.
public static class SaunaFoodArt
{
    public static Sprite Make(string id, int kind)
    {
        string path = "Assets/Art/Items/" + id + ".png";
        if (!File.Exists(path))
        {
            Directory.CreateDirectory("Assets/Art/Items");
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var p = new Color[64 * 64];
            Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
            void Rect(int x, int y, int w, int h, string color) { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) if (xx >= 0 && xx < 64 && yy >= 0 && yy < 64) p[yy * 64 + xx] = C(color); }
            void Oval(int x, int y, int rx, int ry, string color) { for (int yy = 0; yy < 64; yy++) for (int xx = 0; xx < 64; xx++) if ((xx - x) * (xx - x) / (float)(rx * rx) + (yy - y) * (yy - y) / (float)(ry * ry) <= 1) p[yy * 64 + xx] = C(color); }
            if (kind == 0)
            {
                Oval(32, 20, 28, 13, "46605B"); Oval(32, 23, 27, 12, "F2E5C9");
                Oval(22, 34, 11, 15, "9D6541"); Oval(42, 31, 11, 15, "B57D50");
                Oval(19, 38, 3, 5, "D8AD73"); Oval(39, 35, 3, 5, "E0B885");
            }
            else if (kind <= 3)
            {
                Rect(15, 10, 34, 43, "46605B"); Rect(18, 13, 28, 36, kind == 1 ? "C8B06F" : kind == 2 ? "72513C" : "F0D56F");
                Rect(12, 49, 40, 5, "F2E5C9"); Rect(20, 19, 4, 25, "FAEFD8");
                if (kind == 1) { Rect(28, 20, 4, 2, "F9F0D7"); Rect(36, 29, 4, 2, "F9F0D7"); Rect(27, 37, 4, 2, "F9F0D7"); }
                if (kind == 2) { Rect(29, 37, 7, 6, "B8CEC9"); Rect(38, 28, 6, 7, "B8CEC9"); Rect(40, 52, 3, 10, "E0B366"); }
                if (kind == 3) { Rect(21, 52, 22, 8, "F0E9C8"); Rect(24, 26, 18, 11, "FAF2D7"); Rect(29, 29, 9, 5, "DAC158"); }
            }
            else if (kind == 6)
            {
                Rect(29, 3, 6, 23, "D3AD76"); Rect(17, 21, 30, 35, "854C36"); Rect(20, 24, 24, 29, "D19551");
                for (int i = 0; i < 3; i++) Rect(23, 29 + i * 8, 17, 3, "A76C3E");
            }
            else
            {
                Oval(32, 26, 28, 18, "46605B"); Oval(32, 28, 25, 16, "E5DCC5");
                Oval(32, 35, 27, 13, "F7ECD4"); Oval(32, 35, 23, 10, kind == 4 ? "B85436" : kind == 7 ? "536B46" : kind == 8 ? "BF7446" : "8CABA1");
                if (kind == 4) { for (int i = 0; i < 4; i++) Rect(17 + i * 2, 29 + i * 4, 24, 2, "EBCD7B"); Oval(42, 38, 6, 5, "F1E9C9"); Oval(42, 38, 2, 2, "E9BA53"); }
                if (kind == 5) for (int i = 0; i < 3; i++) { Oval(18 + 14 * i, 35 + (i % 2) * 5, 8, 7, "FAEED7"); for (int j = 0; j < 3; j++) Rect(13 + 14 * i + j * 3, 36 + i % 2 * 5, 1, 4, "CEBE9D"); }
                if (kind == 7) { Rect(20, 31, 8, 6, "304F35"); Rect(34, 35, 10, 5, "304F35"); Rect(25, 40, 6, 4, "7F9258"); }
                if (kind == 8) { Oval(23, 35, 10, 7, "F8EBD0"); Rect(29, 31, 14, 6, "9F5134"); Rect(30, 39, 13, 5, "C76136"); Rect(37, 36, 7, 3, "82A270"); }
            }
            if (kind == 9) { System.Array.Clear(p, 0, p.Length); Rect(16, 9, 32, 40, "35554C"); Rect(19, 12, 26, 33, "729480"); Rect(11, 47, 42, 7, "2B4540"); Rect(25, 54, 15, 4, "46685A"); Rect(26, 24, 13, 12, "D8D6AE"); }
            if (kind == 10) { System.Array.Clear(p, 0, p.Length); Oval(23, 24, 7, 12, "8DD8E4"); Oval(46, 31, 5, 9, "8DD8E4"); Rect(21, 33, 3, 13, "8DD8E4"); Rect(45, 38, 2, 10, "8DD8E4"); }
            if (kind == 11) for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)); p[y * 64 + x] = d <= 30 && d >= 25 ? Color.white : Color.clear; }
            t.SetPixels(p); t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 64; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
