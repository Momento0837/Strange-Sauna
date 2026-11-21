using System;
using System.Linq;
using UnityEngine;
namespace SuspiciousSauna
{
    public sealed class SaunaSceneLayout : MonoBehaviour
    {
        public Transform entrance, playerSpawn;
        public Transform foodWasteBin;
        public BoxCollider2D mapBounds;
        public SaunaTableView[] tables;
        [Tooltip("조리, 계산, 휴식, 청소 도구 순서")] public Transform[] stations;
        public SaunaMapData Capture()
        {
            if (entrance == null || playerSpawn == null || mapBounds == null || tables == null || tables.Length == 0 || tables.Any(t => t == null) || stations == null || stations.Length != 4)
                throw new InvalidOperationException("Scene Layout의 맵, 테이블, 4개 카운터 참조를 확인하세요.");
            var ordered = tables.OrderBy(t => t.index).ToArray();
            var center = mapBounds.transform.TransformPoint(mapBounds.offset);
            var size = Vector2.Scale(mapBounds.size, mapBounds.transform.lossyScale);
            var m = new SaunaMapData { bounds = new Rect((Vector2)center - size * .5f, size),
                entrance = entrance.position, playerStart = playerSpawn.position, tables = new Vector2[ordered.Length],
                seats = new Vector2[ordered.Length][], stations = stations.Select(s => (Vector2)s.position).ToArray(),
                obstacles = GetComponentsInChildren<SaunaObstacle>().Select(o => o.WorldRect()).ToList() };
            for (int i = 0; i < ordered.Length; i++)
            {
                if (ordered[i].index != i || ordered[i].seats.Length != 4 || ordered[i].seats.Any(s => s == null))
                    throw new InvalidOperationException("테이블 ID는 0부터 연속되어야 하며, 좌석은 각각 4개여야 합니다.");
                m.tables[i] = ordered[i].transform.position; m.seats[i] = ordered[i].seats.Select(s => (Vector2)s.position).ToArray();
            }
            return m;
        }
    }
}
