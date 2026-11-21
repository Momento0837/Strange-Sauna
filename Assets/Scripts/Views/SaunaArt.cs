using System.Collections.Generic;
using UnityEngine;

namespace SuspiciousSauna
{
    // Original, code-authored pixel art. No downloaded art or external asset dependencies.
    public static class SaunaArt
    {
        public static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public static Material Material;
        public static Sprite Get(string name)
        {
            if (sprites.TryGetValue(name, out var sprite)) return sprite;
            var p = new Pixels(name == "floor" ? Mathf.RoundToInt(SaunaMap.Width * 20) : name == "counter" ? 160 : 96, name == "floor" ? Mathf.RoundToInt(SaunaMap.Height * 20) : 96);
            switch (name)
            {
                case "floor":
                    for (int y = 0; y < p.H; y++) for (int x = 0; x < p.W; x++)
                    {
                        bool counter = y >= 290;
                        string col = counter ? ((x / 20 + y / 20) % 2 == 0 ? "C7BA9E" : "BDAF93") : ((x / 13 + y / 13) % 2 == 0 ? "C9CECA" : "A5AEAB");
                        if (x % (counter ? 20 : 13) == 0 || y % (counter ? 20 : 13) == 0) col = counter ? "B2A58C" : "929F9B";
                        p.Set(x, y, C(col));
                    }
                    p.Box(0, 0, p.W, 9, "465E5C"); p.Box(0, 0, 9, p.H, "465E5C"); p.Box(p.W - 9, 0, 9, p.H, "465E5C");
                    p.Box(0, p.H - 30, p.W, 30, "456562"); p.Box(9, p.H - 30, p.W - 18, 5, "E6C997");
                    p.Box(9, 293, p.W - 18, 4, "6E7770"); p.Box(9, 298, p.W - 18, 3, "E1D5B9");
                    p.Box(p.W / 2 - 30, 0, 60, 10, "DCCAA8");
                    p.Box(p.W / 2 - 28, 13, 56, 25, "637B70"); p.Box(p.W / 2 - 22, 18, 44, 2, "B3C0A2");
                    break;
                case "table":
                    p.Ellipse(8, 6, 80, 64, "72857B");
                    p.Box(16, 10, 10, 26, "654F43"); p.Box(69, 10, 10, 26, "654F43");
                    p.Round(7, 23, 82, 65, 8, "594F44"); p.Round(10, 28, 76, 59, 6, "C39868");
                    p.Round(12, 34, 72, 51, 4, "E1BB83");
                    p.Box(16, 46, 64, 2, "CEAA75"); p.Box(16, 69, 64, 2, "CEAA75");
                    p.Box(19, 37, 12, 1, "EED1A0"); p.Box(65, 75, 12, 1, "EED1A0");
                    p.Round(35, 50, 26, 21, 3, "F0DEC1"); p.Box(39, 53, 18, 14, "F9E9CA");
                    p.Ellipse(60, 36, 12, 12, "8A6C4E"); p.Ellipse(61, 39, 10, 9, "F8E8C8"); p.Ellipse(64, 41, 5, 5, "A1B2A2");
                    break;
                case "chair":
                    p.Ellipse(19, 12, 60, 45, "788C80"); p.Round(22, 22, 52, 52, 9, "526A63");
                    p.Round(22, 29, 52, 50, 8, "8AA395"); p.Round(28, 36, 40, 36, 6, "A5B8A2");
                    p.Box(30, 65, 36, 3, "BDC9AD"); break;
                case "counter":
                    p.Round(1, 6, 158, 76, 7, "6A6050"); p.Box(5, 10, 150, 36, "8B6E51");
                    for (int x = 10; x < 150; x += 19) p.Box(x, 12, 2, 30, "A58560");
                    p.Round(0, 43, 160, 48, 5, "405D58"); p.Round(4, 49, 152, 39, 4, "608078");
                    p.Box(7, 83, 146, 3, "9BAF94"); break;
                case "cook":
                    p.Round(10, 13, 76, 55, 5, "D4D1B7"); p.Box(15, 20, 66, 40, "5F7268");
                    p.Ellipse(21, 25, 51, 48, "354E49"); p.Ellipse(25, 34, 43, 35, "C8D0B6"); p.Ellipse(30, 38, 33, 25, "AC7451");
                    p.Box(66, 48, 21, 7, "384C47"); p.Box(37, 70, 4, 12, "EBE5C9"); p.Box(49, 76, 4, 11, "EBE5C9"); break;
                case "register":
                    p.Round(12, 12, 72, 30, 4, "283F3C"); p.Round(22, 35, 53, 43, 5, "364F4C");
                    p.Box(27, 43, 43, 29, "ACCFAD"); p.Box(33, 49, 28, 4, "527E69");
                    for (int x = 22; x < 75; x += 13) p.Box(x, 22, 8, 7, "D6D3B6"); break;
                case "rest":
                    p.Round(5, 10, 85, 65, 9, "6D7663"); p.Round(10, 21, 75, 48, 8, "A1AB83");
                    p.Round(14, 50, 67, 29, 7, "BBC395"); p.Box(44, 25, 3, 39, "869871");
                    p.Round(12, 27, 14, 35, 3, "CFD1A2"); p.Round(70, 27, 13, 35, 3, "CFD1A2"); break;
                case "clean":
                    p.Box(59, 25, 5, 64, "DBB574"); p.Round(38, 17, 43, 22, 5, "9DB8AE");
                    p.Round(10, 8, 40, 40, 7, "527D79"); p.Ellipse(10, 36, 40, 18, "C3D5C2"); p.Ellipse(15, 39, 30, 10, "7AB6B1");
                    p.Box(23, 16, 5, 19, "77A39A"); break;
                case "plant":
                    p.Round(29, 8, 38, 36, 6, "AF7C58"); p.Box(34, 14, 28, 5, "CA9970");
                    p.Ellipse(17, 39, 40, 39, "527964"); p.Ellipse(41, 37, 37, 40, "689178");
                    p.Ellipse(31, 58, 36, 32, "81A284"); p.Box(46, 30, 5, 44, "A4B591"); break;
                case "dirt":
                    p.Ellipse(13, 18, 38, 19, "8B795E"); p.Ellipse(49, 45, 30, 16, "8B795E");
                    p.Box(63, 18, 8, 5, "71674F"); p.Box(26, 64, 9, 5, "71674F");
                    p.Box(20, 39, 15, 8, "EDE3C8"); p.Box(23, 45, 8, 3, "FAEDD2"); break;
                case "halo": p.Ellipse(4, 30, 88, 36, "E8C985"); p.Ellipse(12, 35, 72, 26, "98AA90"); break;
                case "star":
                    for (int y = 0; y < 96; y++) for (int x = 0; x < 96; x++)
                    {
                        float a = Mathf.Atan2(y - 48, x - 48) - Mathf.PI / 2;
                        float r = Mathf.Sqrt((x - 48) * (x - 48) + (y - 48) * (y - 48));
                        float wave = Mathf.Repeat(a / (Mathf.PI * .4f) + .5f, 1);
                        float limit = Mathf.Lerp(44, 19, Mathf.Abs(wave - .5f) * 2);
                        if (r < limit) p.Set(x, y, Color.white);
                    }
                    break;
                default: DrawCharacter(p, name); break;
            }
            Texture2D texture = p.Texture(name);
            float units = name == "floor" ? 20 : name == "counter" ? 32 : 40;
            sprite = Sprite.Create(texture, new Rect(0, 0, p.W, p.H), new Vector2(.5f, .5f), units);
            sprite.name = name; sprites[name] = sprite; return sprite;
        }

        static void DrawCharacter(Pixels p, string name)
        {
            bool player = name == "player";
            string shirt = player ? "D58D52" : name == "guest1" ? "86A4AD" : name == "guest2" ? "BD9CB0" : name == "guest3" ? "91A47A" : "D1B57D";
            p.Ellipse(22, 6, 52, 17, "748C81");
            p.Round(31, 13, 13, 23, 4, "D7AD87"); p.Round(53, 13, 13, 23, 4, "D7AD87");
            p.Round(29, 12, 17, 7, 2, "4B5752"); p.Round(52, 12, 17, 7, 2, "4B5752");
            p.Round(23, 27, 50, 36, 9, shirt); p.Round(19, 31, 11, 22, 4, "E8C6A0"); p.Round(66, 31, 11, 22, 4, "E8C6A0");
            if (player) { p.Round(31, 27, 35, 25, 3, "405F58"); p.Box(39, 38, 18, 7, "739184"); p.Box(34, 48, 4, 11, "F4DCB3"); p.Box(59, 48, 4, 11, "F4DCB3"); }
            else { p.Box(30, 28, 37, 5, "E8D8B8"); p.Box(46, 45, 3, 12, "F0E0BF"); }
            p.Round(27, 54, 43, 31, 11, "684E40"); p.Round(29, 52, 39, 29, 10, "F0CC9F");
            p.Box(37, 63, 4, 5, "4B5147"); p.Box(56, 63, 4, 5, "4B5147"); p.Box(46, 57, 8, 2, "BC8770");
            p.Box(31, 59, 5, 3, "E7AD89"); p.Box(61, 59, 5, 3, "E7AD89");
            if (player)
            {
                p.Round(28, 75, 42, 13, 5, "F2E0BD"); p.Ellipse(21, 73, 20, 19, "F2E0BD"); p.Ellipse(58, 73, 20, 19, "F2E0BD");
                p.Ellipse(26, 77, 10, 10, "CDBD99"); p.Ellipse(63, 77, 10, 10, "CDBD99");
            }
            else
            {
                p.Round(27, 75, 43, 13, 5, "E6DDC4"); p.Box(30, 76, 37, 3, "C0C4AC");
                p.Round(24, 73, 14, 18, 5, "E6DDC4"); p.Round(60, 73, 14, 18, 5, "E6DDC4");
            }
        }
        public static SpriteRenderer Place(string spriteName, Vector2 pos, Transform parent, float scale = 1, int order = 0)
        {
            var go = new GameObject(spriteName); go.transform.SetParent(parent, false); go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.transform.localScale = Vector3.one * scale;
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = Get(spriteName); r.sortingOrder = order;
            if (Material != null) r.sharedMaterial = Material;
            return r;
        }
        sealed class Pixels
        {
            public readonly int W, H; readonly Color[] pixels;
            public Pixels(int w, int h) { W = w; H = h; pixels = new Color[w * h]; }
            public void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < W && y < H) pixels[y * W + x] = c; }
            public void Box(int x, int y, int w, int h, string color)
            { Color c = C(color); for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Set(i, j, c); }
            public void Ellipse(int x, int y, int w, int h, string color)
            {
                Color c = C(color);
                for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++)
                    if (Mathf.Pow((i + .5f - x - w * .5f) / (w * .5f), 2) + Mathf.Pow((j + .5f - y - h * .5f) / (h * .5f), 2) <= 1) Set(i, j, c);
            }
            public void Round(int x, int y, int w, int h, int radius, string color)
            {
                Box(x + radius, y, w - 2 * radius, h, color); Box(x, y + radius, w, h - 2 * radius, color);
                Ellipse(x, y, radius * 2, radius * 2, color); Ellipse(x + w - radius * 2, y, radius * 2, radius * 2, color);
                Ellipse(x, y + h - radius * 2, radius * 2, radius * 2, color); Ellipse(x + w - radius * 2, y + h - radius * 2, radius * 2, radius * 2, color);
            }
            public Texture2D Texture(string name)
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                tex.SetPixels(pixels); tex.Apply(); return tex;
            }
        }
    }
}
