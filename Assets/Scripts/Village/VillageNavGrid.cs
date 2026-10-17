using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Utility;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 영지의 걸을 수 있는 땅을 격자로 읽어 길을 찾는다.
    ///
    /// 지도마다 손으로 순찰 경로를 찍으면 그림이 바뀔 때마다 다시 찍어야 한다.
    /// 그래서 씬이 열릴 때 막힌 충돌체(건물 윤곽, 사물 밑동, 바깥 벽)를 그대로 읽어 격자를 만든다.
    /// v2 한 장짜리 지도든 파츠 지도든 충돌체만 맞으면 같은 코드로 걷는다.
    ///
    /// 격자는 Awake에서 한 번 만든다. 움직이는 물체(주인공 등 Rigidbody가 달린 것)는 막힘으로 보지 않는다.
    /// </summary>
    public sealed class VillageNavGrid : MonoBehaviour
    {
        // 8방향 이웃. 대각선은 √2 비용이다.
        private static readonly int[] NEIGHBOR_X = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] NEIGHBOR_Y = { 0, 0, 1, -1, 1, -1, 1, -1 };
        private const float DIAGONAL_COST = 1.4142136f;

        [Tooltip("격자가 덮는 넓이(월드 유닛). 가운데가 원점이다. 바닥 그림의 크기와 같게 둔다.")]
        [SerializeField] private Vector2 Size = new Vector2(53.3f, 30f);

        private readonly Collider2D[] OverlapBuffer = new Collider2D[16];

        private bool[] Walkable;
        private int Width;
        private int Height;
        private Vector2 Origin;

        // 길 찾기 작업 공간. 매번 새로 만들지 않는다.
        private float[] Cost;
        private int[] CameFrom;
        private int[] Visit;
        private int VisitStamp;
        private readonly List<int> Open = new List<int>();

        /// <summary>지금 씬의 격자. 씬마다 하나만 둔다.</summary>
        public static VillageNavGrid Current { get; private set; }

        /// <summary>에디터 도구가 만들 때 바닥 크기를 넣는다.</summary>
        public void Configure(Vector2 size)
        {
            Size = size;
        }

        private void Awake()
        {
            Current = this;
            Build();
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Current, this) == true)
            {
                Current = null;
            }
        }

        /// <summary>충돌체를 읽어 격자를 다시 만든다.</summary>
        public void Build()
        {
            var cell = Constants.VILLAGE_NAV_CELL;
            Width = Mathf.Max(1, Mathf.CeilToInt(Size.x / cell));
            Height = Mathf.Max(1, Mathf.CeilToInt(Size.y / cell));
            Origin = (Vector2)transform.position - Size * 0.5f;

            Walkable = new bool[Width * Height];
            Cost = new float[Width * Height];
            CameFrom = new int[Width * Height];
            Visit = new int[Width * Height];

            var probe = Vector2.one * (cell + Constants.VILLAGE_NAV_CLEARANCE);
            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            var open = 0;

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var blocked = false;
                    var count = Physics2D.OverlapBox(CellCenter(x, y), probe, 0f, filter, OverlapBuffer);

                    for (var i = 0; i < count; i++)
                    {
                        var hit = OverlapBuffer[i];

                        // 움직이는 몸은 길을 막지 않는다. 주인공이 선 자리가 영원히 막히면 안 된다.
                        if (hit.attachedRigidbody != null && hit.attachedRigidbody.bodyType != RigidbodyType2D.Static)
                        {
                            continue;
                        }

                        blocked = true;
                        break;
                    }

                    Walkable[Index(x, y)] = blocked == false;

                    if (blocked == false)
                    {
                        open++;
                    }
                }
            }

            UnderwriteLog.Info($"길 찾기 격자 {Width}×{Height}, 걸을 수 있는 칸 {open}개.");
        }

        /// <summary>걸을 수 있는 아무 자리. 못 찾으면 false.</summary>
        public bool TryRandomPoint(out Vector2 point)
        {
            for (var attempt = 0; attempt < 64; attempt++)
            {
                var x = UnityEngine.Random.Range(0, Width);
                var y = UnityEngine.Random.Range(0, Height);

                if (Walkable[Index(x, y)] == true)
                {
                    point = CellCenter(x, y);
                    return true;
                }
            }

            point = Vector2.zero;
            return false;
        }

        /// <summary>그 자리에서 가장 가까운 걸을 수 있는 칸의 가운데. 근처에 없으면 원래 자리를 돌려준다.</summary>
        public Vector2 NearestWalkable(Vector2 at)
        {
            var cx = ToCellX(at.x);
            var cy = ToCellY(at.y);

            for (var radius = 0; radius < 12; radius++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        {
                            continue;
                        }

                        var x = cx + dx;
                        var y = cy + dy;

                        if (InBounds(x, y) == true && Walkable[Index(x, y)] == true)
                        {
                            return CellCenter(x, y);
                        }
                    }
                }
            }

            return at;
        }

        /// <summary>
        /// 두 자리 사이의 길을 찾아 <paramref name="path"/>에 채운다. 출발점은 넣지 않는다.
        /// 장애물이 없는 구간은 줄여서, 칸마다 꺾이지 않고 곧게 걷게 한다.
        /// 길이 없으면 false.
        /// </summary>
        public bool FindPath(Vector2 from, Vector2 to, List<Vector2> path)
        {
            path.Clear();
            from = NearestWalkable(from);
            to = NearestWalkable(to);

            var start = Index(ToCellX(from.x), ToCellY(from.y));
            var goal = Index(ToCellX(to.x), ToCellY(to.y));

            if (Walkable[start] == false || Walkable[goal] == false)
            {
                return false;
            }

            VisitStamp++;
            Open.Clear();
            Touch(start, 0f, -1);
            Open.Add(start);

            var gx = goal % Width;
            var gy = goal / Width;
            var found = false;

            while (Open.Count > 0)
            {
                var current = PopBest(gx, gy);

                if (current == goal)
                {
                    found = true;
                    break;
                }

                var x = current % Width;
                var y = current / Width;

                for (var n = 0; n < NEIGHBOR_X.Length; n++)
                {
                    var nx = x + NEIGHBOR_X[n];
                    var ny = y + NEIGHBOR_Y[n];

                    if (InBounds(nx, ny) == false || Walkable[Index(nx, ny)] == false)
                    {
                        continue;
                    }

                    var diagonal = NEIGHBOR_X[n] != 0 && NEIGHBOR_Y[n] != 0;

                    // 모서리를 비스듬히 뚫고 지나가지 않는다.
                    if (diagonal == true
                        && (Walkable[Index(nx, y)] == false || Walkable[Index(x, ny)] == false))
                    {
                        continue;
                    }

                    var next = Index(nx, ny);
                    var cost = Cost[current] + (diagonal == true ? DIAGONAL_COST : 1f);

                    if (Visit[next] == VisitStamp && Cost[next] <= cost)
                    {
                        continue;
                    }

                    var fresh = Visit[next] != VisitStamp;
                    Touch(next, cost, current);

                    if (fresh == true)
                    {
                        Open.Add(next);
                    }
                }
            }

            if (found == false)
            {
                return false;
            }

            // 거꾸로 따라가며 칸을 모은다.
            var cells = new List<int>();

            for (var at = goal; at != -1; at = CameFrom[at])
            {
                cells.Add(at);
            }

            cells.Reverse();
            Smooth(cells, path);
            return true;
        }

        private void Touch(int cell, float cost, int cameFrom)
        {
            Visit[cell] = VisitStamp;
            Cost[cell] = cost;
            CameFrom[cell] = cameFrom;
        }

        // 열린 목록에서 가장 나은 칸을 꺼낸다. 영지 격자는 수천 칸이고 길 찾기는 몇 초에 한 번이라
        // 선형 탐색으로 충분하다. 느려지면 이진 힙으로 바꾼다.
        private int PopBest(int gx, int gy)
        {
            var bestAt = 0;
            var bestScore = float.MaxValue;

            for (var i = 0; i < Open.Count; i++)
            {
                var cell = Open[i];
                var dx = Mathf.Abs(cell % Width - gx);
                var dy = Mathf.Abs(cell / Width - gy);
                var heuristic = Mathf.Max(dx, dy) + (DIAGONAL_COST - 1f) * Mathf.Min(dx, dy);
                var score = Cost[cell] + heuristic;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestAt = i;
                }
            }

            var best = Open[bestAt];
            Open[bestAt] = Open[Open.Count - 1];
            Open.RemoveAt(Open.Count - 1);
            return best;
        }

        // 앞의 점에서 곧게 보이는 가장 먼 점까지 건너뛴다.
        private void Smooth(List<int> cells, List<Vector2> path)
        {
            var anchor = 0;

            while (anchor < cells.Count - 1)
            {
                var far = anchor + 1;

                for (var i = cells.Count - 1; i > anchor + 1; i--)
                {
                    if (LineClear(CellCenter(cells[anchor]), CellCenter(cells[i])) == true)
                    {
                        far = i;
                        break;
                    }
                }

                path.Add(CellCenter(cells[far]));
                anchor = far;
            }
        }

        private bool LineClear(Vector2 a, Vector2 b)
        {
            var step = Constants.VILLAGE_NAV_CELL * 0.5f;
            var distance = Vector2.Distance(a, b);
            var samples = Mathf.CeilToInt(distance / step);

            for (var i = 1; i < samples; i++)
            {
                var p = Vector2.Lerp(a, b, (float)i / samples);
                var x = ToCellX(p.x);
                var y = ToCellY(p.y);

                if (InBounds(x, y) == false || Walkable[Index(x, y)] == false)
                {
                    return false;
                }
            }

            return true;
        }

        private int Index(int x, int y)
        {
            return y * Width + x;
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        private int ToCellX(float worldX)
        {
            return Mathf.Clamp(Mathf.FloorToInt((worldX - Origin.x) / Constants.VILLAGE_NAV_CELL), 0, Width - 1);
        }

        private int ToCellY(float worldY)
        {
            return Mathf.Clamp(Mathf.FloorToInt((worldY - Origin.y) / Constants.VILLAGE_NAV_CELL), 0, Height - 1);
        }

        private Vector2 CellCenter(int x, int y)
        {
            return Origin + new Vector2((x + 0.5f) * Constants.VILLAGE_NAV_CELL, (y + 0.5f) * Constants.VILLAGE_NAV_CELL);
        }

        private Vector2 CellCenter(int cell)
        {
            return CellCenter(cell % Width, cell / Width);
        }
    }
}
