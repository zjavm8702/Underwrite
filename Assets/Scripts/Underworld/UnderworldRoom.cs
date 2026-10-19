using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.UI;
using Underwrite.Utility;
using Underwrite.Village;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Underwrite.Underworld
{
    /// <summary>
    /// 걸어 다니는 지하 방(2026-10-17 사용자 결정).
    ///
    /// - 주식판 앞에서 E(또는 어디서든 Tab): 거래판(거래 화면)을 연다
    /// - 정보 타임: 시간이 멈추고 카운터 뒤 정보상이 부른다. 가서 E를 누르면 소식이 열린다
    /// - 테이블의 투자꾼: 종목이 크게 움직이면 「망했네」, 「좋아!」 같은 혼잣말을 한다. 말풍선뿐이다
    ///
    /// 거래와 시간은 <see cref="MarketScreenController"/>가 그대로 맡는다. 이 클래스는 방을 걷는 일만 한다.
    /// 정보상이 누구인지는 플레이어가 알 필요 없다 — 연출이다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UnderworldRoom : MonoBehaviour
    {
        [SerializeField] private MarketScreenController Market;

        [Tooltip("거래판을 여는 주식판 앞 자리.")]
        [SerializeField] private Transform Board;

        [Tooltip("카운터 뒤의 정보상.")]
        [SerializeField] private CharacterActor Informant;

        [Tooltip("테이블에 앉은 투자꾼들. 종목이 크게 움직이면 한 사람이 혼잣말을 한다.")]
        [SerializeField] private List<CharacterActor> Investors = new List<CharacterActor>();

        [Tooltip("투자꾼의 몸짓(Investors와 같은 순서). 혼잣말과 함께 환호하거나 머리를 감싼다.")]
        [SerializeField] private List<SeatedFigure> InvestorFigures = new List<SeatedFigure>();

        [Tooltip("여관 위층으로 올라가는 출구(벽의 문). 장이 끝난 뒤에만 올라간다.")]
        [SerializeField] private Transform Exit;

        [Tooltip("주식판 그림의 글자 칸 가운데. 분필 글씨가 여기에 맞춰 뜬다.")]
        [SerializeField] private Transform BoardTextArea;

        [Tooltip("글자 칸 크기(월드 유닛).")]
        [SerializeField] private Vector2 BoardTextSize = new Vector2(4f, 2f);

        private UI_ChalkBoard Chalk;

        private VisualElement Root;
        private Transform Player;
        private PlayerCharacterInput PlayerInput;

        // 투자꾼이 본 마지막 가격. 다음 확인 때 이만큼 움직였으면 반응한다.
        private readonly Dictionary<string, float> LastSeen = new Dictionary<string, float>();
        private float InvestorCooldown;
        private float CallTimer;

        // 다가갈 수 있는 표지(게시판 · 정보상 · 출구). 씬에서 바뀌지 않으므로 한 번만 만든다(규칙 §10).
        private Transform[] Targets;

        private CharacterActor PlayerActor
        {
            get
            {
                return Player == null ? null : Player.GetComponent<CharacterActor>();
            }
        }

        private void Update()
        {
            if (EnsureScreen() == false || Market == null || Market.Simulation == null)
            {
                return;
            }

            FindPlayer();

            var keyboard = Keyboard.current;
            var covering = Market.IsCoveringRoom;

            // 판이 열려 있으면 Tab으로 닫는다. 판을 연 채로는 걷지 않는다.
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame == true)
            {
                if (Market.IsPanelOpen == true)
                {
                    Market.ClosePanel();
                }
                else if (covering == false)
                {
                    Market.OpenPanel();
                }

                covering = Market.IsCoveringRoom;
            }

            if (PlayerInput != null && PlayerInput.enabled == covering)
            {
                PlayerInput.enabled = covering == false;
            }

            Root.style.display = covering == true ? DisplayStyle.None : DisplayStyle.Flex;
            WorldBubbles.Tick(covering);

            if (Chalk != null && BoardTextArea != null)
            {
                Chalk.Tick(Market.Simulation, BoardTextArea.position, BoardTextSize, covering, Time.unscaledDeltaTime);
            }
            CallInformant();
            ReactInvestors();

            if (covering == true)
            {
                return;
            }

            RefreshClock();

            var near = Nearest();
            RefreshPrompt(near);

            if (near == null || keyboard == null || keyboard.eKey.wasPressedThisFrame == false)
            {
                return;
            }

            if (near == Board)
            {
                Market.OpenPanel();
                return;
            }

            if (near == Exit)
            {
                // 장이 열려 있으면 올라가지 않는다 — 장 끝내기로 넘기고 나서 올라간다.
                if (Market.LeaveToVillage() == false && PlayerActor != null)
                {
                    PlayerActor.Say(StringConstants.TEXT_ROOM_EXIT_LOCKED);
                }

                return;
            }

            TalkToInformant();
        }

        private bool EnsureScreen()
        {
            if (Root != null)
            {
                return true;
            }

            var document = GetComponent<UIDocument>();
            var root = document == null ? null : document.rootVisualElement;

            if (root == null)
            {
                return false;
            }

            Root = root.Q<VisualElement>("room-root");
            GameAudio.AttachClicks(root);

            if (Root == null)
            {
                return false;
            }

            WorldBubbles.Attach(Root);
            Chalk = new UI_ChalkBoard(Root);
            return true;
        }

        private void FindPlayer()
        {
            if (Player != null)
            {
                return;
            }

            PlayerInput = FindAnyObjectByType<PlayerCharacterInput>();
            Player = PlayerInput == null ? null : PlayerInput.transform;
        }

        /// <summary>말을 걸 수 있는 거리 안에서 가장 가까운 것 — 주식판이나 정보상. 없으면 null.</summary>
        private Transform Nearest()
        {
            if (Player == null)
            {
                return null;
            }

            Transform best = null;
            var bestDistance = Constants.ROOM_REACH_DISTANCE;
            if (Targets == null)
            {
                Targets = new[] { Board, Informant == null ? null : Informant.transform, Exit };
            }

            var targets = Targets;

            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                var distance = Vector2.Distance(Player.position, targets[i].position);

                if (distance <= bestDistance)
                {
                    best = targets[i];
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void RefreshPrompt(Transform near)
        {
            if (Prompt == null)
            {
                Prompt = Root.Q<VisualElement>("room-prompt");
            }

            var prompt = Prompt;

            if (prompt == null)
            {
                return;
            }

            prompt.EnableInClassList("hidden", near == null);

            if (near == null)
            {
                return;
            }

            var place = near == Board ? StringConstants.TEXT_SOURCE_MARKET_BOARD : StringConstants.TEXT_INFORMANT;
            string key;

            if (near == Exit)
            {
                place = StringConstants.TEXT_ROOM_EXIT;
                key = Market.Simulation.Clock.Phase == MarketPhase.Phase_Closed
                    ? StringConstants.TEXT_ROOM_EXIT_KEY
                    : StringConstants.TEXT_ROOM_EXIT_LOCKED;
            }
            else if (near == Board)
            {
                key = StringConstants.TEXT_ROOM_BOARD;
            }
            else
            {
                key = Market.IsBriefingWaiting == true
                    ? StringConstants.TEXT_ROOM_INFORMANT_WAITING
                    : StringConstants.TEXT_ROOM_INFORMANT;
            }

            SetText("room-place", place);
            SetText("room-key", key);
        }

        /// <summary>영지 시계와 같은 순서로 적는다. 정보상이 부르면 그것을 먼저 알린다.</summary>
        // 시계 줄 캐시. 날짜·장·초·안내가 바뀔 때만 다시 조립한다(매 프레임 문자열 조립 금지).
        private int ClockKey = -1;

        private void RefreshClock()
        {
            var clock = Market.Simulation.Clock;
            var waiting = Market.IsBriefingWaiting == true ? 1 : 0;
            var key = ((clock.CalendarDay * 2 + (int)clock.Session) * 86400 + clock.ClockTotalSeconds) * 2 + waiting;

            if (key == ClockKey)
            {
                return;
            }

            ClockKey = key;
            var session = clock.Session == MarketSession.Session_Evening
                ? StringConstants.TEXT_SESSION_EVENING
                : StringConstants.TEXT_SESSION_MORNING;
            var text = ClockText.SeasonDay(clock.CalendarDay) + " · " + session + " "
                + ClockText.FromSeconds(clock.ClockTotalSeconds, true) + " · "
                + (Market.IsBriefingWaiting == true ? StringConstants.TEXT_ROOM_WAITING_NOTE : StringConstants.TEXT_ROOM_HINT);
            SetText("room-clock", text);
        }

        private void TalkToInformant()
        {
            if (Informant == null)
            {
                return;
            }

            if (Player != null)
            {
                Informant.FaceTowards(Player.position);
            }

            if (Market.IsBriefingWaiting == true)
            {
                Market.OpenBriefing();
                return;
            }

            Informant.Say(Random.value < 0.5f ? StringConstants.TEXT_ROOM_IDLE_1 : StringConstants.TEXT_ROOM_IDLE_2);
        }

        // 정보 타임이 오면 정보상이 손짓한다. 시간은 거래소가 멈춰 두었다.
        private void CallInformant()
        {
            if (Informant == null || Market.IsBriefingWaiting == false)
            {
                CallTimer = 0f;
                return;
            }

            CallTimer -= Time.unscaledDeltaTime;

            if (CallTimer > 0f)
            {
                return;
            }

            CallTimer = Constants.ROOM_CALL_INTERVAL_SECONDS;
            Informant.Emote(ActorEmote.Emote_Exclaim);
            Informant.Say(StringConstants.TEXT_ROOM_CALL);
        }

        /// <summary>
        /// 종목이 크게 움직였으면 투자꾼 하나가 혼잣말을 한다. 숫자를 보지 않아도 시장이 움직인 것을 느끼게.
        /// 판이 열려 있어도 값은 따라가 둔다 — 닫고 나왔을 때 지난 움직임에 뒤늦게 반응하지 않게.
        /// </summary>
        private void ReactInvestors()
        {
            InvestorCooldown -= Time.unscaledDeltaTime;
            var stocks = Market.Simulation.Stocks;
            var moved = 0f;

            for (var i = 0; i < stocks.Count; i++)
            {
                var stock = stocks[i];

                if (stock.IsTradable == false)
                {
                    continue;
                }

                float last;

                if (LastSeen.TryGetValue(stock.Id, out last) == true && last > 0f)
                {
                    var change = (stock.Price - last) / last;

                    if (Mathf.Abs(change) > Mathf.Abs(moved))
                    {
                        moved = change;
                    }
                }

                LastSeen[stock.Id] = stock.Price;
            }

            if (Market.IsCoveringRoom == true || InvestorCooldown > 0f || Investors.Count == 0
                || Mathf.Abs(moved) < Constants.ROOM_INVESTOR_MOVE_RATIO)
            {
                return;
            }

            InvestorCooldown = Constants.ROOM_INVESTOR_COOLDOWN_SECONDS;
            var index = Random.Range(0, Investors.Count);
            var investor = Investors[index];

            // 숨겨 둔 사람(다시 그리는 중)은 말하지 않는다.
            if (investor != null && investor.isActiveAndEnabled == true)
            {
                investor.Say(InvestorLine(moved > 0f));
            }

            if (index < InvestorFigures.Count && InvestorFigures[index] != null)
            {
                InvestorFigures[index].Play(moved > 0f ? SeatedMotion.Motion_Cheer : SeatedMotion.Motion_Groan);
            }
        }

        // 문구는 부를 때마다 상수에서 읽는다. 문구 표가 덮어쓴 값을 써야 한다.
        private static string InvestorLine(bool up)
        {
            var pick = Random.Range(0, 3);

            if (up == true)
            {
                if (pick == 0)
                {
                    return StringConstants.TEXT_INVESTOR_UP_1;
                }

                return pick == 1 ? StringConstants.TEXT_INVESTOR_UP_2 : StringConstants.TEXT_INVESTOR_UP_3;
            }

            if (pick == 0)
            {
                return StringConstants.TEXT_INVESTOR_DOWN_1;
            }

            return pick == 1 ? StringConstants.TEXT_INVESTOR_DOWN_2 : StringConstants.TEXT_INVESTOR_DOWN_3;
        }

        private VisualElement Prompt;
        private readonly Dictionary<string, Label> Labels = new Dictionary<string, Label>();

        // 매 프레임 불린다. 찾은 이름표를 기억한다.
        private void SetText(string name, string value)
        {
            Label label;

            if (Labels.TryGetValue(name, out label) == false)
            {
                label = Root.Q<Label>(name);

                if (label == null)
                {
                    return;
                }

                Labels.Add(name, label);
            }

            if (label != null && label.text != value)
            {
                label.text = value;
            }
        }
    }
}
