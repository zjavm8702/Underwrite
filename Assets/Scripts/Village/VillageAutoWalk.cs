using System;
using System.Collections.Generic;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 주인공을 정해진 곳까지 자동으로 걷게 한다(2026-10-18 사장님 — 프롤로그 순찰은 강제 진행, 다음 장소로 데려간다).
    ///
    /// 길은 격자 A*로 찾는다. 칸마다 발 크기 상자를 대 보아 벽·울타리·건물 충돌체에 걸리면 막힌 칸이다.
    /// 찾은 길은 막힘 없이 곧게 갈 수 있는 점까지 건너뛰어 줄인다. 걷는 동안은 입력 대신 이것이 물리 속도와
    /// 방향(애니메이터)을 넣는다. 오래 진척이 없으면 목적지로 옮겨 놓는다 — 연출이 멈춰 서는 것보다 낫다.
    /// </summary>
    public sealed class VillageAutoWalk : MonoBehaviour
    {
        private static readonly int DirectionX = Animator.StringToHash("DirectionX");
        private static readonly int DirectionY = Animator.StringToHash("DirectionY");
        private static readonly int Speed = Animator.StringToHash("Speed");

        private Rigidbody2D Body;
        private Animator Anim;
        private Collider2D Self;
        private readonly List<Vector2> Path = new List<Vector2>();
        private int Next;
        private Action Arrived;
        private float StuckTime;
        private float LastDistance;

        public bool IsWalking
        {
            get
            {
                return Arrived != null;
            }
        }

        private void Awake()
        {
            Body = GetComponentInChildren<Rigidbody2D>();
            Anim = GetComponentInChildren<Animator>();
            Self = GetComponentInChildren<Collider2D>();
        }

        /// <summary><paramref name="target"/>까지 걷는다. 닿으면 <paramref name="onArrive"/>.</summary>
        public void WalkTo(Vector2 target, Action onArrive)
        {
            Path.Clear();
            Path.AddRange(FindPath(Body.position, target, Self));
            Next = 0;
            Arrived = onArrive;
            StuckTime = 0f;
            LastDistance = float.MaxValue;
        }

        private void FixedUpdate()
        {
            if (Arrived == null)
            {
                return;
            }

            if (Next >= Path.Count)
            {
                Finish();
                return;
            }

            var to = Path[Next] - Body.position;
            var distance = to.magnitude;

            if (distance <= Constants.AUTOWALK_ARRIVE_DISTANCE)
            {
                Next++;
                StuckTime = 0f;
                LastDistance = float.MaxValue;
                return;
            }

            // 진척이 없으면(무언가에 끼었다) 잠시 뒤 그 점으로 옮겨 놓는다.
            StuckTime = distance < LastDistance - 0.01f ? 0f : StuckTime + Time.fixedDeltaTime;
            LastDistance = Mathf.Min(LastDistance, distance);

            if (StuckTime > Constants.AUTOWALK_STUCK_SECONDS)
            {
                Body.position = Path[Next];
                Next++;
                StuckTime = 0f;
                LastDistance = float.MaxValue;
                return;
            }

            var velocity = to / distance * Constants.AUTOWALK_SPEED;
            Body.linearVelocity = velocity;

            // 입력과 같은 8방향으로 몸을 돌린다.
            var step = Mathf.PI / 4f;
            var snapped = Mathf.Round(Mathf.Atan2(velocity.y, velocity.x) / step) * step;
            Anim.SetFloat(DirectionX, Mathf.Cos(snapped));
            Anim.SetFloat(DirectionY, Mathf.Sin(snapped));
            Anim.SetFloat(Speed, velocity.magnitude);
        }

        private void Finish()
        {
            Body.linearVelocity = Vector2.zero;
            Anim.SetFloat(Speed, 0f);
            var done = Arrived;
            Arrived = null;

            if (done != null)
            {
                done();
            }
        }

        // ── 길찾기 ─────────────────────────────

        /// <summary>
        /// 격자 A*(8방향, 모서리 끼어들기 없음). 길이 없으면 목적지 한 점만 돌려준다(걷다가 끼면 옮겨 놓는다).
        /// 순찰 정거장마다 한 번 돈다(저빈도).
        /// </summary>
        public static List<Vector2> FindPath(Vector2 from, Vector2 to, Collider2D self)
        {
            var cell = Constants.AUTOWALK_CELL;
            var min = Vector2.Min(from, to) - Vector2.one * Constants.AUTOWALK_MARGIN;
            var max = Vector2.Max(from, to) + Vector2.one * Constants.AUTOWALK_MARGIN;
            var width = Mathf.CeilToInt((max.x - min.x) / cell) + 1;
            var height = Mathf.CeilToInt((max.y - min.y) / cell) + 1;
            var blocked = new bool[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    blocked[x, y] = IsBlocked(min + new Vector2(x, y) * cell, self);
                }
            }

            var start = ToCell(from, min, cell, width, height);
            var goal = ToCell(to, min, cell, width, height);
            blocked[start.x, start.y] = false;
            blocked[goal.x, goal.y] = false;

            var cost = new Dictionary<Vector2Int, float> { { start, 0f } };
            var came = new Dictionary<Vector2Int, Vector2Int>();
            var open = new List<Vector2Int> { start };

            while (open.Count > 0)
            {
                var best = 0;

                for (var i = 1; i < open.Count; i++)
                {
                    if (cost[open[i]] + Heuristic(open[i], goal) < cost[open[best]] + Heuristic(open[best], goal))
                    {
                        best = i;
                    }
                }

                var current = open[best];
                open.RemoveAt(best);

                if (current == goal)
                {
                    return Smooth(Rebuild(came, current, min, cell, to), self);
                }

                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        var next = new Vector2Int(current.x + dx, current.y + dy);

                        if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || blocked[next.x, next.y] == true)
                        {
                            continue;
                        }

                        // 대각선은 양옆이 다 트여 있어야 지난다. 모서리를 스치며 끼지 않게.
                        if (dx != 0 && dy != 0 && (blocked[current.x + dx, current.y] == true || blocked[current.x, current.y + dy] == true))
                        {
                            continue;
                        }

                        var step = cost[current] + (dx != 0 && dy != 0 ? 1.414f : 1f);
                        float known;

                        if (cost.TryGetValue(next, out known) == true && known <= step)
                        {
                            continue;
                        }

                        cost[next] = step;
                        came[next] = current;

                        if (open.Contains(next) == false)
                        {
                            open.Add(next);
                        }
                    }
                }
            }

            return new List<Vector2> { to };
        }

        private static bool IsBlocked(Vector2 point, Collider2D self)
        {
            var hits = Physics2D.OverlapBoxAll(point, Constants.AUTOWALK_FOOT, 0f);

            foreach (var hit in hits)
            {
                if (hit != self && hit.isTrigger == false && hit.attachedRigidbody == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2Int ToCell(Vector2 point, Vector2 min, float cell, int width, int height)
        {
            return new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt((point.x - min.x) / cell), 0, width - 1),
                Mathf.Clamp(Mathf.RoundToInt((point.y - min.y) / cell), 0, height - 1));
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Vector2Int.Distance(a, b);
        }

        private static List<Vector2> Rebuild(Dictionary<Vector2Int, Vector2Int> came, Vector2Int end, Vector2 min,
            float cell, Vector2 to)
        {
            var path = new List<Vector2> { to };
            var current = end;

            while (came.ContainsKey(current) == true)
            {
                current = came[current];
                path.Add(min + new Vector2(current.x, current.y) * cell);
            }

            path.Reverse();

            // 첫 점은 지금 선 자리라 빼도 된다.
            if (path.Count > 1)
            {
                path.RemoveAt(0);
            }

            return path;
        }

        // 막힘 없이 곧게 닿는 가장 먼 점으로 건너뛴다. 격자 계단 걸음을 없앤다.
        private static List<Vector2> Smooth(List<Vector2> path, Collider2D self)
        {
            if (path.Count <= 2)
            {
                return path;
            }

            var result = new List<Vector2> { path[0] };
            var anchor = path[0];
            var index = 1;

            while (index < path.Count - 1)
            {
                if (IsClear(anchor, path[index + 1], self) == false)
                {
                    anchor = path[index];
                    result.Add(anchor);
                }

                index++;
            }

            result.Add(path[path.Count - 1]);
            return result;
        }

        private static bool IsClear(Vector2 from, Vector2 to, Collider2D self)
        {
            var direction = to - from;
            var hits = Physics2D.BoxCastAll(from, Constants.AUTOWALK_FOOT, 0f, direction.normalized, direction.magnitude);

            foreach (var hit in hits)
            {
                if (hit.collider != self && hit.collider.isTrigger == false && hit.collider.attachedRigidbody == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
