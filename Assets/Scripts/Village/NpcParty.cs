using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 모험단 한 무리. 앞사람을 따라 줄지어 영지의 장소들을 돌아다닌다.
    ///
    /// 설계안 §1-1에서 거래 단위는 용병단이다. 플레이어가 지하시장에서 사고파는 그 단이
    /// 영지에서 무리 지어 걷는 것을 볼 수 있어야 숫자가 사람이 된다.
    /// 다만 이들과는 관계를 맺지 않는다. 말을 걸면 짧게 대꾸할 뿐이다.
    ///
    /// 맨 앞사람만 길을 찾는다. 뒷사람은 앞사람이 지나간 자리를 밟는다.
    /// 모두가 따로 길을 찾으면 문간에서 흩어져 무리로 보이지 않는다.
    /// </summary>
    public sealed class NpcParty : MonoBehaviour
    {
        [Tooltip("무리. 첫 사람이 앞장선다.")]
        [SerializeField] private List<NpcWalker> Members = new List<NpcWalker>();

        [Tooltip("돌아다닐 곳. 장소의 문 앞 자리를 넣는다. 비어 있으면 아무 데나 간다.")]
        [SerializeField] private List<Vector2> Destinations = new List<Vector2>();

        [Tooltip("지하시장에서 이 단을 가리키는 종목. 상장 전에는 도착해야 보이고, 떠나면 사라진다.")]
        [SerializeField] private string StockId = string.Empty;

        /// <summary>이 단의 종목 아이디. 비어 있으면 시장과 묶이지 않은 단이다.</summary>
        public string Stock
        {
            get
            {
                return StockId;
            }
        }

        // 앞사람이 지나간 자리. 일정 간격마다 하나씩 남긴다.
        private readonly List<Vector2> Trail = new List<Vector2>();
        private float RestLeft;
        private bool Resting = true;
        private int LastDestination = -1;

        /// <summary>에디터 도구가 만들 때 무리와 목적지를 넣는다.</summary>
        public void Configure(List<NpcWalker> members, List<Vector2> destinations, string stockId)
        {
            Members = members;
            Destinations = destinations;
            StockId = stockId;
        }

        /// <summary>
        /// 시장의 생애에 맞춰 영지에 보이거나 사라진다(상장흐름 설계안 §3).
        /// 다음 주기에 상장될 단은 지금 영지에 와 있고, 전멸·은퇴한 단은 더 보이지 않는다.
        /// 거래소를 거치지 않고 영지만 띄웠으면(시장이 없으면) 모두 보인다.
        /// </summary>
        private void ApplyStage()
        {
            var sim = Market.BreakClock.Simulation;

            if (sim == null || string.IsNullOrEmpty(StockId) == true)
            {
                return;
            }

            var stage = sim.StageOf(StockId);
            var visible = stage == Enumeration.BandStage.Band_Arrived || stage == Enumeration.BandStage.Band_Listed;

            for (var i = 0; i < Members.Count; i++)
            {
                if (Members[i] != null)
                {
                    Members[i].gameObject.SetActive(visible);
                }
            }

            enabled = visible;
        }

        private void Start()
        {
            ApplyStage();

            // 처음에는 저마다 다른 때에 출발한다. 모든 단이 동시에 움직이면 꾸민 티가 난다.
            RestLeft = UnityEngine.Random.Range(0f, Constants.NPC_PARTY_REST_MAX);
            ResetTrail();
        }

        private void Update()
        {
            if (Members.Count == 0 || Members[0] == null)
            {
                return;
            }

            var leader = Members[0];

            if (Resting == true)
            {
                RestLeft -= Time.deltaTime;

                if (RestLeft > 0f)
                {
                    return;
                }

                Resting = leader.WalkTo(PickDestination()) == false;
                RestLeft = Resting == true ? 1f : 0f;
                return;
            }

            RecordTrail(leader.Foot);
            FollowTrail();

            if (leader.IsMoving == true)
            {
                return;
            }

            // 닿았다. 잠시 머물며 서로 마주 본다.
            Resting = true;
            RestLeft = UnityEngine.Random.Range(Constants.NPC_PARTY_REST_MIN, Constants.NPC_PARTY_REST_MAX);
            GatherAround(leader.Foot);
        }

        private Vector2 PickDestination()
        {
            // 갈 곳이 하나뿐이면 닿은 뒤 다시 갈 데가 없다. 그때는 아무 데나 걷는다.
            if (Destinations.Count < 2)
            {
                var grid = VillageNavGrid.Current;
                Vector2 point;

                if (grid != null && grid.TryRandomPoint(out point) == true)
                {
                    return point;
                }

                return Members[0].Foot;
            }

            // 방금 있던 곳으로 바로 돌아가지 않는다.
            var pick = UnityEngine.Random.Range(0, Destinations.Count);

            if (Destinations.Count > 1 && pick == LastDestination)
            {
                pick = (pick + 1) % Destinations.Count;
            }

            LastDestination = pick;

            // 문 앞 자리 바로 위에 모이면 플레이어가 들어갈 자리를 막는다. 조금 비켜 아래쪽 광장에 선다.
            var spread = UnityEngine.Random.insideUnitCircle * Constants.NPC_PARTY_SPREAD;
            return Destinations[pick] + new Vector2(spread.x, spread.y - Constants.NPC_PARTY_DOOR_GAP);
        }

        private void ResetTrail()
        {
            Trail.Clear();

            if (Members.Count > 0 && Members[0] != null)
            {
                Trail.Add(Members[0].Foot);
            }
        }

        private void RecordTrail(Vector2 foot)
        {
            if (Trail.Count == 0 || Vector2.Distance(Trail[Trail.Count - 1], foot) >= Constants.NPC_PARTY_SPACING * 0.25f)
            {
                Trail.Add(foot);
            }

            // 맨 뒷사람에게 필요한 만큼만 남긴다.
            var keep = Mathf.CeilToInt(Members.Count * 4f) + 4;

            if (Trail.Count > keep)
            {
                Trail.RemoveRange(0, Trail.Count - keep);
            }
        }

        // 뒷사람은 앞사람에서 간격만큼 떨어진 발자국을 향해 간다.
        private void FollowTrail()
        {
            for (var i = 1; i < Members.Count; i++)
            {
                if (Members[i] == null)
                {
                    continue;
                }

                var back = Mathf.Min(Trail.Count - 1, i * 4);
                Members[i].Follow(Trail[Trail.Count - 1 - back]);
            }
        }

        // 머무는 동안 일행이 앞사람 둘레에 모여 그쪽을 본다.
        private void GatherAround(Vector2 center)
        {
            for (var i = 0; i < Members.Count; i++)
            {
                if (Members[i] == null)
                {
                    continue;
                }

                Members[i].Stop();

                if (i > 0)
                {
                    Members[i].Face(center - Members[i].Foot);
                }
            }

            // 도착하면 가끔 한 사람이 들뜬다. 무리가 사람으로 보이게 한다.
            if (UnityEngine.Random.value < Constants.NPC_PARTY_CHEER_CHANCE)
            {
                var pick = Members[UnityEngine.Random.Range(0, Members.Count)];
                var actor = pick != null ? pick.GetComponent<CharacterActor>() : null;
                var npc = pick != null ? pick.GetComponent<VillageNpc>() : null;

                if (actor != null)
                {
                    actor.Play(UnityEngine.Random.value < 0.5f ? ActorMotion.Motion_Cheer : ActorMotion.Motion_Hop);

                    if (npc != null)
                    {
                        actor.Say(npc.PickLine());
                    }
                }
            }

            ResetTrail();
        }
    }
}
