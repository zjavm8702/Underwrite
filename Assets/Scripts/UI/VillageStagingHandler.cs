using Underwrite.Characters;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEngine;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 영지의 연출 — 장면 전환 제목 카드, 시간이 흐르는 암전, 다음 장소로 데려가기(짧은 암전 + 순간이동), 문 앞 주인의 반김.
    /// 영지 화면(<see cref="VillageScreenController"/>)이 조립하고, 프롤로그·후원 안내·밤·메뉴가 쓴다(2026-10-19 정리 — 화면에서 떼어 냄).
    /// </summary>
    public sealed class VillageStagingHandler
    {
        private const string FADE = "village-fade";
        private const string FADE_TIME = "village-fade-time";
        private const string FADE_NOTE = "village-fade-note";

        private readonly VillageScreenController Host;
        private UI_TitleCard TitleCard;

        public VillageStagingHandler(VillageScreenController host)
        {
            Host = host;
        }

        /// <summary>다음 장소로 데려가는 중인가.</summary>
        public bool IsGuiding { get; private set; }

        /// <summary>제목 카드(또는 데려가기 암전)가 떠 있는가.</summary>
        public bool IsTitleOpen
        {
            get
            {
                return TitleCard != null && TitleCard.IsOpen == true;
            }
        }

        /// <summary>매 프레임. 제목 카드는 실제 시간으로 흐른다.</summary>
        public void Tick(float unscaledDelta)
        {
            if (TitleCard != null)
            {
                TitleCard.Tick(unscaledDelta);
            }
        }

        private UI_TitleCard Card
        {
            get
            {
                if (TitleCard == null)
                {
                    TitleCard = new UI_TitleCard(Host.Root);
                }

                return TitleCard;
            }
        }

        // ── 제목 카드 ─────────

        /// <summary>장면 전환 제목 카드. 가장 어두울 때 <paramref name="onDark"/>, 다 걷히면 <paramref name="onDone"/>.</summary>
        public void ShowTitle(string title, string subtitle, System.Action onDark, System.Action onDone)
        {
            Card.Play(title, subtitle, onDark, onDone);
        }

        /// <summary>이미 어두운 화면에서 시작하는 제목 카드(영지가 처음 뜰 때).</summary>
        public void ShowTitleFromDark(string title, string subtitle, System.Action onDone)
        {
            Card.PlayFromDark(title, subtitle, onDone);
        }

        // ── 데려가기 ─────────

        /// <summary>짧은 암전 동안 그 장소의 문 앞으로 옮긴다. 닿으면 문 쪽을 보고 <paramref name="onArrive"/>.</summary>
        public void GuideTo(VillageSpot spot, System.Action onArrive)
        {
            Host.StartCoroutine(Guide(spot, onArrive));
        }

        // 자동 걷기는 장애물에 걸려 어색했다(2026-10-18 사장님 — 순간이동으로).
        private System.Collections.IEnumerator Guide(VillageSpot spot, System.Action onArrive)
        {
            IsGuiding = true;
            var arrived = false;
            Card.Blink(() => WarpTo(spot), () => arrived = true);

            while (arrived == false)
            {
                yield return null;
            }

            IsGuiding = false;
            onArrive();
        }

        /// <summary>암전 없이 그 장소의 문 앞에 세운다(지하에서 올라왔을 때 등).</summary>
        public void WarpTo(VillageSpot spot)
        {
            var player = Host.PlayerTransform;

            if (player == null)
            {
                return;
            }

            var from = player.position;
            var to = spot.transform.position + (Vector3)Constants.PROLOGUE_WARP_OFFSET;
            var body = player.GetComponentInChildren<Rigidbody2D>();

            if (body != null)
            {
                body.position = to;
                body.linearVelocity = Vector2.zero;
            }

            player.position = to;

            // 카메라가 쫓아가며 미끄러지지 않게 같은 만큼 함께 옮긴다(드문 일이라 그때 찾는다).
            var camera = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();

            if (camera != null)
            {
                camera.OnTargetObjectWarped(player, to - from);
            }

            var actor = Host.PlayerActor;

            if (actor != null)
            {
                actor.Face(Vector2.up);
            }
        }

        /// <summary>장소에 닿았다 — 문 앞의 주인이 주인공을 돌아보고 반긴다.</summary>
        public void GreetAt(VillageSpot spot)
        {
            VillageNpc nearest = null;
            var best = Constants.PROLOGUE_GREET_RADIUS;
            var all = VillageNpc.All;

            for (var i = 0; i < all.Count; i++)
            {
                var distance = Vector2.Distance(all[i].transform.position, spot.transform.position);

                if (distance < best && all[i].Party == null)
                {
                    best = distance;
                    nearest = all[i];
                }
            }

            var actor = nearest == null ? null : nearest.GetComponent<CharacterActor>();
            var player = Host.PlayerTransform;

            if (actor == null || player == null)
            {
                return;
            }

            actor.FaceTowards(player.position);
            actor.Emote(ActorEmote.Emote_Exclaim);
        }

        // ── 시간이 흐르는 암전 (2026-10-17 사용자 요청) ─────────

        /// <summary>
        /// 화면이 어두워지고 시각이 넘어간 뒤 다시 밝아진다. 어두운 동안 사람들이 자리를 옮긴다.
        /// <paramref name="title"/>은 어두운 화면에 시각 대신 띄울 말 — 비우면 「몇 시 → 몇 시」.
        /// </summary>
        public void PassTime(int minutes, string title)
        {
            Host.StartCoroutine(PassTimeRoutine(minutes, title));
        }

        private System.Collections.IEnumerator PassTimeRoutine(int minutes, string title)
        {
            Host.SetApproaching(true);
            var clock = Host.Clock;
            var fade = EnsureFade();
            var fromHour = clock.BreakHour;
            var fromMinute = clock.BreakMinute;

            yield return FadeTo(fade, 1f);

            clock.SpendBreakMinutes(minutes);
            Host.SetText(FADE_TIME, title ?? string.Format(StringConstants.TEXT_TIME_PASS,
                ClockText.FromMinutes(fromHour * 60 + fromMinute), ClockText.FromMinutes(clock.BreakMinuteOfDay)));
            var dayNote = Host.CheckDayTurn();
            var passNote = string.Format(StringConstants.TEXT_TIME_PASS_NOTE, minutes / 60, minutes % 60);
            Host.SetText(FADE_NOTE, dayNote.Length > 0 ? passNote + "\n\n" + dayNote : passNote);

            // 드문 일(시간 넘기기)이라 그때 찾는다.
            foreach (var party in Object.FindObjectsByType<NpcParty>(FindObjectsInactive.Exclude))
            {
                party.Scatter();
            }

            foreach (var ambient in Object.FindObjectsByType<NpcAmbient>(FindObjectsInactive.Exclude))
            {
                ambient.Shuffle();
            }

            yield return new WaitForSeconds(Constants.TIME_PASS_HOLD);
            yield return FadeTo(fade, 0f);

            Host.SetText(FADE_TIME, string.Empty);
            Host.SetText(FADE_NOTE, string.Empty);
            Host.SetApproaching(false);
        }

        private VisualElement EnsureFade()
        {
            var fade = Host.Root.Q<VisualElement>(FADE);

            if (fade != null)
            {
                return fade;
            }

            fade = new VisualElement();
            fade.name = FADE;
            fade.AddToClassList(FADE);
            fade.pickingMode = PickingMode.Position;
            fade.style.opacity = 0f;
            fade.style.display = DisplayStyle.None;

            var time = new Label();
            time.name = FADE_TIME;
            time.AddToClassList(FADE_TIME);
            var note = new Label();
            note.name = FADE_NOTE;
            note.AddToClassList(FADE_NOTE);
            fade.Add(time);
            fade.Add(note);
            Host.Root.Add(fade);
            return fade;
        }

        private static System.Collections.IEnumerator FadeTo(VisualElement fade, float target)
        {
            fade.style.display = DisplayStyle.Flex;
            var from = fade.resolvedStyle.opacity;

            for (var t = 0f; t < Constants.TIME_PASS_FADE; t += Time.deltaTime)
            {
                fade.style.opacity = Mathf.Lerp(from, target, t / Constants.TIME_PASS_FADE);
                yield return null;
            }

            fade.style.opacity = target;

            if (target <= 0f)
            {
                fade.style.display = DisplayStyle.None;
            }
        }
    }
}
