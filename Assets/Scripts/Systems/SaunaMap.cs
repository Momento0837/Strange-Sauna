using System.Collections.Generic;
using UnityEngine;

namespace SuspiciousSauna
{
    // One geometry source is shared by collision, navigation, camera limits, and editor checks.
    public static class SaunaMap
    {
        public const int TableColumns = 4, TableRows = 2, TableCount = TableColumns * TableRows;
        public const float Width = 24, Height = 23, ViewHalf = 8.6f;
        public static readonly Vector2 Entrance = new Vector2(12, .8f);
        public static readonly Vector2 Start = new Vector2(8.8f, 16);
        public static readonly Vector2 RetailShelf = new Vector2(18, 21.8f);
        public static readonly Vector2[] Stations = {
            new Vector2(4, 18.5f), new Vector2(12, 18.5f), new Vector2(20, 18.5f), new Vector2(22, 15.8f)
        };
        public static readonly string[] StationNames = { "음식 조리", "물건 계산", "잠깐 휴식", "청소 도구" };
        public static Vector2 TableCenter(int index) => new Vector2(4.5f + index % TableColumns * 5, 4.5f + index / TableColumns * 5);
        public static Vector2 Seat(int table, int chair)
        {
            return TableCenter(table) + new Vector2(chair < 2 ? -1.63f : 1.63f, chair % 2 == 0 ? .58f : -.58f);
        }
        public static List<Rect> Obstacles()
        {
            var result = new List<Rect>();
            for (int i = 0; i < TableCount; i++) { var p = TableCenter(i); result.Add(new Rect(p.x - 1.12f, p.y - .83f, 2.24f, 1.66f)); }
            for (int i = 0; i < 3; i++) { var p = Stations[i]; result.Add(new Rect(p.x - 2.5f, p.y - .9f, 5, 1.8f)); }
            result.Add(new Rect(Stations[3] - Vector2.one * .6f, Vector2.one * 1.2f));
            return result;
        }
        public static Vector2 ClampCamera(Vector2 player)
        {
            return new Vector2(Mathf.Clamp(player.x, ViewHalf, Width - ViewHalf), Mathf.Clamp(player.y, ViewHalf, Height - ViewHalf));
        }
        public static Rect PlayRect(float width, float height)
        {
            float side = Mathf.Min(width, height) * .9f;
            return new Rect((width - side) * .5f, (height - side) * .5f, side, side);
        }
    }

    public sealed class SaunaNavigation
    {
        const float Step = .5f, Radius = .26f;
        readonly int W, H;
        readonly bool[] open;
        readonly Rect bounds;
        readonly List<Rect> obstacles;
        readonly int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        public SaunaNavigation(SaunaMapData map = null)
        {
            map = map ?? SaunaMapData.Default();
            bounds = map.bounds;
            W = Mathf.CeilToInt(bounds.width / Step); H = Mathf.CeilToInt(bounds.height / Step);
            open = new bool[W * H]; obstacles = map.obstacles;
            for (int i = 0; i < open.Length; i++) open[i] = Walkable(Point(i), Radius);
        }
        public bool Walkable(Vector2 p, float radius = Radius)
        {
            if (p.x < bounds.xMin + radius + .3f || p.x > bounds.xMax - radius - .3f || p.y < bounds.yMin + radius + .25f || p.y > bounds.yMax - radius - .5f) return false;
            foreach (Rect r in obstacles)
                if (p.x > r.xMin - radius && p.x < r.xMax + radius && p.y > r.yMin - radius && p.y < r.yMax + radius) return false;
            return true;
        }
        public Vector2 Move(Vector2 p, Vector2 delta)
        {
            // Substeps prevent tunnelling even after a long frame.
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .15f));
            delta /= steps;
            for (int i = 0; i < steps; i++)
            {
                if (Walkable(p + new Vector2(delta.x, 0), .3f)) p.x += delta.x;
                if (Walkable(p + new Vector2(0, delta.y), .3f)) p.y += delta.y;
            }
            return p;
        }
        Vector2 Point(int index) => new Vector2(bounds.xMin + index % W * Step + .25f, bounds.yMin + index / W * Step + .25f);
        int Closest(Vector2 p)
        {
            int best = 0; float distance = float.MaxValue;
            for (int i = 0; i < open.Length; i++) if (open[i])
            {
                float d = (Point(i) - p).sqrMagnitude;
                if (d < distance) { distance = d; best = i; }
            }
            return best;
        }
        public List<Vector2> Path(Vector2 from, Vector2 to, int preference)
        {
            int start = Closest(from), end = Closest(to);
            var cost = new float[open.Length]; var previous = new int[open.Length]; var closed = new bool[open.Length];
            for (int i = 0; i < cost.Length; i++) { cost[i] = float.MaxValue; previous[i] = -1; }
            cost[start] = 0;
            var queue = new MinHeap(); queue.Push(start, 0);
            while (queue.Count > 0)
            {
                int current = queue.Pop();
                if (closed[current]) continue;
                if (current == end) break;
                closed[current] = true;
                int x = current % W, y = current / W;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                    int next = ny * W + nx;
                    if (!open[next] || closed[next]) continue;
                    uint hash = unchecked((uint)(next * 374761393 + preference * 668265263));
                    float candidate = cost[current] + 1 + (hash % 101) * .003f;
                    if (candidate >= cost[next]) continue;
                    cost[next] = candidate; previous[next] = current;
                    float heuristic = Mathf.Abs(nx - end % W) + Mathf.Abs(ny - end / W);
                    queue.Push(next, candidate + heuristic);
                }
            }
            var result = new List<Vector2>();
            if (start != end && previous[end] < 0) return result;
            int cursor = end;
            while (cursor != start) { result.Add(Point(cursor)); cursor = previous[cursor]; }
            result.Add(Point(start)); result.Reverse();
            if (Walkable(to)) result.Add(to);
            return result;
        }
        sealed class MinHeap
        {
            readonly List<(int node, float priority)> items = new List<(int, float)>();
            public int Count => items.Count;
            public void Push(int node, float priority)
            {
                items.Add((node, priority)); int i = items.Count - 1;
                while (i > 0) { int parent = (i - 1) / 2; if (items[parent].priority <= priority) break; items[i] = items[parent]; i = parent; }
                items[i] = (node, priority);
            }
            public int Pop()
            {
                int node = items[0].node; var last = items[items.Count - 1]; items.RemoveAt(items.Count - 1);
                if (items.Count == 0) return node;
                int i = 0;
                while (i * 2 + 1 < items.Count)
                {
                    int child = i * 2 + 1;
                    if (child + 1 < items.Count && items[child + 1].priority < items[child].priority) child++;
                    if (items[child].priority >= last.priority) break;
                    items[i] = items[child]; i = child;
                }
                items[i] = last; return node;
            }
        }
    }
}
