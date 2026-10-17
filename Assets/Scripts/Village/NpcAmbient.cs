using Underwrite.Enumeration;
using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Village
{
    /// <summary>
    /// 영지 사람이 주인공을 알아본다. 말을 걸기 전부터 영지가 살아 있게 한다.
    ///
    /// - 서 있는 사람은 주인공이 가까이 오면 돌아보고, 처음 알아볼 때 머리 위에 표시를 띄운다
    /// - 노점상은 주인공이 근처에 있으면 이따금 손님을 부른다
    /// </summary>
    [RequireComponent(typeof(CharacterActor))]
    [RequireComponent(typeof(VillageNpc))]
    public sealed class NpcAmbient : MonoBehaviour
    {
        /// <summary>주인공. 영지 화면이 걸어 둔다.</summary>
        public static Transform Player { get; set; }

        private CharacterActor Actor;
        private VillageNpc Npc;
        private NpcWalker Walker;
        private float NoticeReadyAt;
        private float NextCall;
        private bool WasNear;

        private Vector2 Home;
        private bool HomeKnown;

        /// <summary>
        /// 시간이 흘렀다. 서 있는 사람은 집 근처 다른 자리로, 모험단은 단째로 다른 곳으로(NpcParty.Scatter).
        /// 노점상은 좌판을 지킨다.
        /// </summary>
        public void Shuffle()
        {
            if (Npc.Kind != NpcRole.Role_Resident || Walker == null)
            {
                return;
            }

            if (HomeKnown == false)
            {
                Home = Walker.Foot;
                HomeKnown = true;
            }

            var grid = VillageNavGrid.Current;
            var foot = Home + UnityEngine.Random.insideUnitCircle * Constants.ACTOR_SHUFFLE_RADIUS;
            Walker.PlaceFoot(grid != null ? grid.NearestWalkable(foot) : foot);
            Actor.Face(UnityEngine.Random.insideUnitCircle);
            WasNear = false;
        }

        private void Awake()
        {
            Actor = GetComponent<CharacterActor>();
            Npc = GetComponent<VillageNpc>();
            Walker = GetComponent<NpcWalker>();
            NextCall = Time.time + UnityEngine.Random.Range(Constants.VENDOR_CALL_MIN, Constants.VENDOR_CALL_MAX);
        }

        private void Update()
        {
            if (Player == null || Actor.IsPerforming == true)
            {
                return;
            }

            var walking = Walker != null && Walker.IsMoving == true;
            var distance = Vector2.Distance(Player.position, transform.position);
            var near = distance < Constants.ACTOR_NOTICE_RANGE;

            if (near == true && walking == false)
            {
                Actor.FaceTowards(Player.position);

                // 막 다가왔을 때만 알아보는 표시를 띄운다. 계속 띄우면 산만하다.
                if (WasNear == false && Time.time >= NoticeReadyAt)
                {
                    Actor.Emote(Npc.Kind == NpcRole.Role_Vendor ? ActorEmote.Emote_Coin : ActorEmote.Emote_Exclaim);
                    NoticeReadyAt = Time.time + Constants.ACTOR_NOTICE_COOLDOWN;
                }
            }

            WasNear = near;

            if (Npc.Kind == NpcRole.Role_Vendor && distance < Constants.VENDOR_CALL_RANGE && Time.time >= NextCall)
            {
                Actor.Say(Npc.PickLine());
                NextCall = Time.time + UnityEngine.Random.Range(Constants.VENDOR_CALL_MIN, Constants.VENDOR_CALL_MAX);
            }
        }
    }
}
