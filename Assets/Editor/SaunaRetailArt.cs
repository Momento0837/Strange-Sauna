using System.IO;
using UnityEditor;
using UnityEngine;

// Placeholder pixel art extends the existing item style and is saved as replaceable PNGs.
public static class SaunaRetailArt
{
    public static Sprite Make(string id, int kind)
    {
        string path = "Assets/Art/Retail/" + id + ".png";
        if (!File.Exists(path))
        {
            Directory.CreateDirectory("Assets/Art/Retail");
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
            void R(int x, int y, int w, int h, string color)
            { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) if (xx >= 0 && xx < 64 && yy >= 0 && yy < 64) pixels[yy * 64 + xx] = C(color); }
            void O(int x, int y, int rx, int ry, string color)
            { for (int yy = 0; yy < 64; yy++) for (int xx = 0; xx < 64; xx++) if ((xx - x) * (xx - x) / (float)(rx * rx) + (yy - y) * (yy - y) / (float)(ry * ry) <= 1) pixels[yy * 64 + xx] = C(color); }
            if (kind == 0)
            { R(12, 8, 7, 46, "33534E"); R(14, 10, 3, 32, "90C5BF"); R(10, 43, 13, 14, "F4E7CD"); R(28, 13, 23, 37, "33534E"); R(31, 16, 17, 31, "EAE4CC"); R(31, 29, 17, 9, "87BEB7"); R(34, 7, 11, 8, "729A95"); }
            else if (kind >= 1 && kind <= 3)
            {
                string body = kind == 1 ? "83B4C8" : kind == 2 ? "E9C375" : "C59FBD";
                R(13, 8, 38, 48, "33534E"); R(16, 11, 32, 42, body); R(16, 49, 32, 4, "F4E7CD"); R(16, 11, 32, 3, "F4E7CD");
                O(32, 31, 10, 12, "F4E7CD");
                if (kind == 1) { R(26, 36, 12, 3, body); R(26, 29, 3, 9, body); R(26, 29, 12, 3, body); R(35, 23, 3, 9, body); R(26, 23, 12, 3, body); }
                if (kind == 2) { O(32, 31, 6, 7, body); R(33, 27, 8, 9, "F4E7CD"); }
                if (kind == 3) { O(30, 29, 5, 5, body); O(35, 35, 3, 3, body); }
            }
            else if (kind == 4)
            { R(27, 8, 10, 33, "33534E"); R(30, 10, 4, 31, "8AB7B7"); R(10, 40, 44, 16, "33534E"); R(13, 43, 38, 10, "C7D9D0"); R(16, 47, 32, 2, "6B8D87"); }
            else if (kind == 5)
            { R(12, 7, 40, 50, "33534E"); R(15, 10, 34, 44, "C4D5B8"); O(32, 32, 13, 18, "F8EFDA"); O(26, 35, 3, 2, "33534E"); O(38, 35, 3, 2, "33534E"); R(28, 22, 8, 2, "BC9988"); }
            else if (kind == 6)
            { R(10, 10, 44, 42, "33534E"); R(13, 13, 38, 36, "C4B357"); for (int y = 15; y < 48; y += 6) for (int x = 15; x < 50; x += 6) R(x, y, 2, 2, "E6D18B"); R(13, 42, 38, 3, "8C863D"); }
            else if (kind == 7)
            { O(32, 27, 25, 17, "33534E"); O(32, 30, 22, 15, "D6AB75"); O(32, 34, 20, 9, "E8C79A"); R(24, 31, 16, 3, "B78959"); O(48, 48, 5, 5, "E2ECE2"); O(18, 51, 3, 3, "E2ECE2"); }
            else if (kind == 8)
            { O(23, 35, 17, 19, "33534E"); O(42, 36, 15, 17, "33534E"); O(23, 36, 14, 16, "7EAF98"); O(42, 37, 12, 14, "A3C9AC"); O(32, 20, 13, 12, "7EAF98"); R(19, 38, 3, 10, "C4D9BC"); }
            else if (kind == 9)
            { O(32, 46, 13, 13, "33534E"); O(32, 46, 8, 9, "E5BB76"); R(10, 8, 44, 38, "33534E"); R(13, 11, 38, 32, "D7B578"); R(18, 16, 28, 22, "EDD6A6"); }
            else if (kind == 10)
            { R(27, 19, 5, 32, "33534E"); R(31, 45, 18, 6, "33534E"); R(44, 27, 5, 23, "33534E"); O(21, 18, 10, 7, "33534E"); O(39, 25, 9, 6, "33534E"); }
            else if (kind == 11)
            { O(22, 27, 14, 12, "99A69A"); O(36, 35, 17, 15, "99A69A"); O(48, 25, 11, 10, "7E8D83"); O(21, 33, 10, 7, "BCC3AD"); O(36, 40, 9, 6, "BCC3AD"); R(25, 26, 9, 3, "5D7066"); R(38, 31, 8, 3, "5D7066"); O(7, 16, 4, 4, "99A69A"); }
            else if (kind == 12)
            {
                R(2, 3, 60, 58, "33534E"); R(5, 6, 54, 52, "9F8059");
                for (int row = 0; row < 2; row++) for (int col = 0; col < 4; col++) { R(9 + col * 12, 12 + row * 25, 8, 15, new[] { "83B4C8", "E9C375", "C59FBD", "C4D5B8" }[col]); R(9 + col * 12, 23 + row * 25, 8, 3, "F4E7CD"); }
                R(5, 30, 54, 4, "E4BF83"); R(5, 6, 54, 4, "E4BF83");
            }
            else if (kind == 13)
            {
                foreach (int x in new[] { 10, 54 }) foreach (int y in new[] { 10, 54 }) O(x, y, 8, 8, "33534E");
                R(10, 2, 44, 60, "33534E"); R(2, 10, 60, 44, "33534E");
                foreach (int x in new[] { 10, 54 }) foreach (int y in new[] { 10, 54 }) O(x, y, 6, 6, "F4E7CD");
                R(10, 4, 44, 56, "F4E7CD"); R(4, 10, 56, 44, "F4E7CD");
            }
            else if (kind == 14)
            {
                for (int y = 0; y < 32; y++) R(32 - y, y, y * 2 + 1, 1, "33534E");
                for (int y = 4; y < 32; y++) R(35 - y, y, y * 2 - 5, 1, "F4E7CD");
            }
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64; importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            if (kind == 13) { importer.spriteBorder = Vector4.one * 10; importer.SaveAndReimport(); }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
