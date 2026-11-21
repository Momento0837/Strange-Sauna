using System.Collections.Generic;
using UnityEngine;
namespace SuspiciousSauna
{
    public sealed class SaunaMapData
    {
        public Rect bounds;
        public Vector2 entrance, playerStart;
        public Vector2[] tables, stations;
        public Vector2[][] seats;
        public List<Rect> obstacles;
        public static SaunaMapData Default()
        {
            var m = new SaunaMapData { bounds = new Rect(0, 0, SaunaMap.Width, SaunaMap.Height), entrance = SaunaMap.Entrance,
                playerStart = SaunaMap.Start, stations = (Vector2[])SaunaMap.Stations.Clone(),
                tables = new Vector2[SaunaMap.TableCount], seats = new Vector2[SaunaMap.TableCount][], obstacles = SaunaMap.Obstacles() };
            for (int i = 0; i < m.tables.Length; i++) { m.tables[i] = SaunaMap.TableCenter(i); m.seats[i] = new Vector2[4];
                for (int j = 0; j < 4; j++) m.seats[i][j] = SaunaMap.Seat(i, j); }
            return m;
        }
        public Vector2 ClampCamera(Vector2 p, float half) => new Vector2(
            bounds.width <= half * 2 ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + half, bounds.xMax - half),
            bounds.height <= half * 2 ? bounds.center.y : Mathf.Clamp(p.y, bounds.yMin + half, bounds.yMax - half));
    }
}
