using System.Collections.Generic;
using Underwrite.Global;
using Underwrite.Utility;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// NPC 한 사람을 걷게 한다. 길을 받아 따라가거나, 한 점을 향해 곧장 간다.
    ///
    /// 물리 몸을 쓰지 않고 자리를 직접 옮긴다. 영지에 수십 명을 세우면 서로 밀치다 문간에 끼어
    /// 움직이지 않게 된다. 길은 격자가 이미 막힌 곳을 피해 찾았으므로 충돌을 다시 볼 필요가 없다.
    ///
    /// 모든 좌표는 **발밑** 기준이다. 스프라이트 피벗이 Center라서 루트는 몸 한가운데에 있다.
    /// </summary>
    public sealed class NpcWalker : MonoBehaviour
    {
        private static readonly int DirectionXHash = Animator.StringToHash(StringConstants.ANIM_PARAM_DIRECTION_X);
        private static readonly int DirectionYHash = Animator.StringToHash(StringConstants.ANIM_PARAM_DIRECTION_Y);
        private static readonly int SpeedHash = Animator.StringToHash(StringConstants.ANIM_PARAM_SPEED);

        // 이보다 짧은 방향은 방향이 없는 것으로 본다.
        private const float FACE_EPSILON = 0.0001f;

        // 이만큼 가까우면 닿은 것으로 본다.
        private const float ARRIVE_DISTANCE = 0.08f;

        [Tooltip("루트에서 발바닥까지의 세로 거리(월드 유닛).")]
        [SerializeField] private float FootOffset = Constants.PROTAGONIST_FOOT_OFFSET;

        [Tooltip("걷는 속도의 배율. 사람마다 조금씩 달라야 행렬처럼 보이지 않는다.")]
        [SerializeField] private float SpeedScale = 1f;

        private readonly List<Vector2> Path = new List<Vector2>();
        private Animator Body;
        private DirectionalSprites Frames;
        private int PathIndex;
        private bool HasDirectTarget;
        private Vector2 DirectTarget;

        /// <summary>발이 닿은 자리.</summary>
        public Vector2 Foot
        {
            get
            {
                return (Vector2)transform.position + new Vector2(0f, FootOffset);
            }
        }

        /// <summary>지금 걷는 중인가.</summary>
        public bool IsMoving
        {
            get
            {
                return HasDirectTarget == true || PathIndex < Path.Count;
            }
        }

        public void Configure(float footOffset, float speedScale)
        {
            FootOffset = footOffset;
            SpeedScale = speedScale;
        }

        /// <summary>발을 그 자리에 놓는다. 걷지 않고 옮긴다.</summary>
        public void PlaceFoot(Vector2 foot)
        {
            transform.position = new Vector3(foot.x, foot.y - FootOffset, transform.position.z);
        }

        /// <summary>격자로 길을 찾아 걷기 시작한다. 길이 없으면 false.</summary>
        public bool WalkTo(Vector2 foot)
        {
            var grid = VillageNavGrid.Current;
            HasDirectTarget = false;

            if (grid == null || grid.FindPath(Foot, foot, Path) == false)
            {
                Path.Clear();
                PathIndex = 0;
                return false;
            }

            PathIndex = 0;
            return true;
        }

        /// <summary>
        /// 길을 찾지 않고 한 점을 향해 곧장 간다. 앞사람 발자국을 밟는 일행이 쓴다.
        /// 발자국은 이미 걸을 수 있는 자리이므로 길을 다시 찾을 필요가 없다.
        /// </summary>
        public void Follow(Vector2 foot)
        {
            Path.Clear();
            PathIndex = 0;
            HasDirectTarget = true;
            DirectTarget = foot;
        }

        public void Stop()
        {
            Path.Clear();
            PathIndex = 0;
            HasDirectTarget = false;
        }

        /// <summary>그쪽을 바라본다. 서서 이야기할 때 서로 마주 보게 한다.</summary>
        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude < FACE_EPSILON)
            {
                return;
            }

            SetDirection(direction);
        }

        private void Awake()
        {
            Body = GetComponentInChildren<Animator>();
            Frames = GetComponent<DirectionalSprites>();

            if (Body == null && Frames == null)
            {
                UnderwriteLog.Warning("Animator가 없어 걷는 모습을 보일 수 없습니다.", context: this);
            }
        }

        private void Start()
        {
            // 세운 자리가 벽이나 좌판 안쪽이면 가장 가까운 걸을 수 있는 칸으로 옮긴다.
            // 노점 자리처럼 그림을 보고 어림으로 찍은 위치가 있기 때문이다.
            var grid = VillageNavGrid.Current;

            if (grid != null)
            {
                PlaceFoot(grid.NearestWalkable(Foot));
            }
        }

        private void Update()
        {
            Vector2 target;

            if (HasDirectTarget == true)
            {
                target = DirectTarget;
            }
            else if (PathIndex < Path.Count)
            {
                target = Path[PathIndex];
            }
            else
            {
                SetSpeed(0f);
                return;
            }

            var foot = Foot;
            var toTarget = target - foot;
            var distance = toTarget.magnitude;

            if (distance <= ARRIVE_DISTANCE)
            {
                if (HasDirectTarget == true)
                {
                    HasDirectTarget = false;
                }
                else
                {
                    PathIndex++;
                }

                SetSpeed(0f);
                return;
            }

            var speed = Constants.NPC_WALK_SPEED * SpeedScale;
            var step = Mathf.Min(distance, speed * Time.deltaTime);
            var move = toTarget / distance * step;
            transform.position += new Vector3(move.x, move.y, 0f);

            SetDirection(toTarget);
            SetSpeed(speed);
        }

        private void SetDirection(Vector2 direction)
        {
            if (Frames != null)
            {
                Frames.SetDirection(direction);
            }

            if (Body == null)
            {
                return;
            }

            // 8방향으로 맞춘다. 주인공과 같은 규칙이다.
            var angle = Mathf.Atan2(direction.y, direction.x);
            var snap = Mathf.PI / 4f;
            var snapped = Mathf.Round(angle / snap) * snap;
            Body.SetFloat(DirectionXHash, Mathf.Cos(snapped));
            Body.SetFloat(DirectionYHash, Mathf.Sin(snapped));
        }

        private void SetSpeed(float speed)
        {
            if (Frames != null)
            {
                Frames.SetSpeed(speed);
            }

            if (Body == null)
            {
                return;
            }

            Body.SetFloat(SpeedHash, speed);
        }
    }
}
