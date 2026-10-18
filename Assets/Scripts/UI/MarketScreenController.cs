using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using Underwrite.Data;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Patronage;
using Underwrite.Save;
using Underwrite.Story;
using Underwrite.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Underwrite.UI
{
    /// <summary>
    /// 거래 화면. UXML·USS를 시뮬레이션에 연결한다.
    ///
    /// 시안: Outputs/MarketUI_v1/Modern_v1/
    /// 이 클래스에는 시장 규칙을 넣지 않는다. 판단은 전부 <see cref="MarketSimulation"/>에 있다.
    /// 여기서 하는 일은 값을 읽어 쓰고, 입력을 시뮬레이션에 넘기는 것뿐이다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MarketScreenController : MonoBehaviour
    {
        private const float MAX_DELTA_SECONDS = 0.1f;
        private const int QUANTITY_STEP = 1;
        private const int QUANTITY_MIN = 1;
        private const int QUANTITY_MAX = 999;
        private const float CHART_LINE_WIDTH = 3f;
        private const float CHART_DOT_RADIUS = 5f;
        private const float CHART_MARKER_MARGIN = 40f;
        private const float CHART_AXIS_WIDTH = 76f;
        private const float CHART_PLOT_LEFT = 10f;
        private const float CHART_RANGE_PAD = 0.12f;
        private const float CHART_MIN_SPAN_RATIO = 0.03f;
        private const int CHART_AXIS_MAX = 7;
        private const float CHART_LABEL_EDGE = 26f;
        private const int INFO_MAX_ROWS = 20;
        private const int RATIO_FIT_STEPS = 4;
        private const float TOAST_SECONDS = 2.8f;
        private const int TOAST_MAX = 3;
        private const float TOAST_SLIDE_SECONDS = 0.22f;
        private const float TOAST_SLIDE_FROM = 36f;

        private enum InfoFilter
        {
            /// <summary>지금 가진 종목의 소식만. 내 돈에 영향을 주는 것부터 읽게 한다.</summary>
            Filter_Mine,
            Filter_All,
            Filter_Private,

            /// <summary>모험단 도감. 영지에서 조사한 만큼 채워진다.</summary>
            Filter_Bands
        }

        [Header("장세")]
        [SerializeField] private int Seed = 20261016;
        [SerializeField] private bool IsInspectionDay;
        [SerializeField] private MarketSentiment StartingSentiment = MarketSentiment.Sentiment_Calm;

        [Header("지하 방")]
        [Tooltip("걸어 다니는 지하 방 씬이면 켠다. 거래판은 닫힌 채로 시작하고(주식판·Tab으로 연다), "
            + "정보 타임은 정보상에게 가서 말을 걸어야 열린다(2026-10-17 사용자 결정).")]
        [SerializeField] private bool RoomMode;

        [Header("공용 UI")]
        [Tooltip("확인이 필요한 일에 띄우는 팝업 한 벌. 비워 두면 팝업 없이 쪽지만 뜬다.")]
        [SerializeField] private VisualTreeAsset PopupTree;

        [Tooltip("대화상자 한 벌. 정보 타임 연출이 이것을 쓴다.")]
        [SerializeField] private VisualTreeAsset DialogueTree;

        [Header("데이터")]
        [Tooltip("표에서 읽어 온다. 비워 두면 코드에 박힌 프로토타입 값을 쓴다.")]
        [SerializeField] private GameDataSet Data;

        [Header("진행")]
        [SerializeField] private bool AutoPauseOnOutcome = true;

        /// <summary>덮어씌울 수 있는 테마. 색 토큰과 배경만 바꾸고 레이아웃은 건드리지 않는다.</summary>
        private enum ScreenTheme
        {
            Theme_Light,
            Theme_Underground,
            Theme_Parchment
        }

        [Header("테마")]
        [Tooltip("F2로 재생 중 돌려 가며 비교할 수 있다. 색과 배경만 바뀌고 레이아웃은 같다.")]
        [SerializeField] private ScreenTheme Theme = ScreenTheme.Theme_Parchment;
        [SerializeField] private StyleSheet UndergroundTheme;
        [SerializeField] private StyleSheet ParchmentTheme;

        private MarketSimulation Sim;
        private VisualElement Root;
        private readonly StringBuilder Builder = new StringBuilder();
        private readonly List<VisualElement> StockRows = new List<VisualElement>();
        private readonly List<string> RowStockIds = new List<string>();
        private readonly HashSet<string> Favourites = new HashSet<string>();

        private string SelectedId = "";
        private OrderSide Side = OrderSide.Side_Buy;
        private int Quantity = 10;
        private InfoFilter Filter = InfoFilter.Filter_All;
        private int InfoHeldShown = -1;
        private int InfoCountShown = -1;
        private int ChartTickShown = -1;
        private int BookTickShown = -1;
        private int PendingCountShown = -1;
        private string Message = "";

        private bool InfoCollapsed = true;
        private bool HoldingsCollapsed = true;
        private int SceneIndex = -1;

        // 지하 방에서 거래판이 열려 있는가. 방 모드가 아니면 늘 열린 것으로 본다.
        private bool PanelOpen;
        private bool PanelApplied;
        private UI_Popup Popup;
        private UI_Dialogue Dialogue;
        private UI_Tutorial Tutorial;
        private bool PausedForTutorial;
        private int HoldingsSeqShown = -1;
        private OrderType Type = OrderType.Type_Market;
        private float LimitPrice;
        private bool LimitTouched;
        private readonly List<BookLevel> Asks = new List<BookLevel>();
        private readonly List<BookLevel> Bids = new List<BookLevel>();

        private void Awake()
        {
            EnsureSimulation();
        }

        /// <summary>
        /// 시뮬레이션을 만든다. 이미 있으면 아무것도 하지 않는다.
        ///
        /// Awake에서만 만들면 두 경우에 비어 버린다.
        /// 하나는 재생 중 재컴파일로 도메인이 리로드될 때다. Sim은 직렬화되지 않아 사라진다.
        /// 다른 하나는 UIDocument와의 초기화 순서가 뒤집힐 때다.
        /// 그래서 Update와 OnEnable에서도 불러 스스로 복구하게 둔다.
        /// </summary>
        private void EnsureSimulation()
        {
            if (Sim != null)
            {
                return;
            }

            // 영지에 다녀왔다. 이미 돌고 있는 시장을 이어 받는다. 새로 만들면 모든 것이 처음으로 돌아간다.
            if (BreakClock.Simulation != null)
            {
                Sim = BreakClock.Simulation;
                BreakClock.Current = Sim.Clock;
                Sim.PlayerAtMarket = true;
                SelectedId = Sim.Stocks.Count > 0 ? Sim.Stocks[0].Id : null;
                ResetViewState();
                ApplyCollapse();
                return;
            }

            // 표의 수치를 먼저 덮어쓴다. 시뮬레이션을 만든 뒤에 하면 시작 자금이 이미 정해져 있다.
            if (Data != null)
            {
                Data.Apply();
            }

            // 마을에서 먼저 하루를 시작했으면 그 시계가 남아 있다. 그 다음 장부터 연다.
            var villageClock = BreakClock.Current;

            Sim = MarketFactory.Create(Data, Seed, StartingSentiment);
            BreakClock.Simulation = Sim;

            // 마을에서 시작한 판이면(인트로 → 여관 → 창고) 영지 시계를 그대로 이어받는다. 날짜·시각·건너뛴 장까지.
            // 예전에는 늘 1일째 오후장을 새로 열어, 영지에서 오후장을 건너뛰고 다음 날 아침에 내려오면
            // 지난 오후장이 다시 열렸다(2026-10-17 사용자 보고). 장은 다음 Update의 OpenIfDue가 연다 —
            // 건너뛴 장을 돌리고, 영지에서 번 돈·캔 소식을 넣는 것도 거기서 한 번에 한다.
            if (villageClock != null && villageClock.IsBreak == true)
            {
                Sim.AdoptClock(MarketClock.Restore(villageClock.Capture()));
            }
            else
            {
                // 계절 첫날에는 오전장이 없다. 첫 장은 1일째 오후장(재개장)이다.
                Sim.OpenSession(IsInspectionDay, MarketSession.Session_Evening);
            }

            // 영지도 같은 시계를 본다. 각자 들면 시각이 어긋난다.
            BreakClock.Current = Sim.Clock;
            Sim.PlayerAtMarket = true;

            SelectedId = Sim.Stocks[0].Id;
            ResetViewState();
        }

        private void OnEnable()
        {
            EnsureSimulation();

            var document = GetComponent<UIDocument>();
            Root = document.rootVisualElement;

            if (Root == null)
            {
                UnderwriteLog.Error("UIDocument에 rootVisualElement가 없습니다. Source Asset을 확인하세요.", context: this);
                return;
            }

            ApplyTheme();
            BuildStockRows();
            BindButtons();
            GameAudio.AttachClicks(Root);

            // 테마 전환은 UI Toolkit의 키 이벤트로 받는다.
            // 이 프로젝트는 새 Input System을 쓰므로 UnityEngine.Input을 쓰면 예외가 난다.
            Root.focusable = true;
            Root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
            Root.Focus();

            Refresh();
        }

        private void Update()
        {
            EnsureSimulation();

            if (Root == null)
            {
                // UIDocument가 늦게 준비되는 경우가 있다. 준비되면 그때 화면을 만든다.
                var document = GetComponent<UIDocument>();
                Root = document == null ? null : document.rootVisualElement;

                if (Root == null)
                {
                    return;
                }

                ApplyTheme();
                BuildStockRows();
                BindButtons();
                Root.focusable = true;
                Root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
                Root.Focus();
                ResetViewState();
            }

            var delta = Mathf.Min(Time.unscaledDeltaTime, MAX_DELTA_SECONDS);

            // 틱보다 잦은 호가 흔들림. 틱 가격과 별개로 돈다.
            Sim.MicroStep(delta);

            var ticks = Sim.Clock.Advance(delta);

            var wasOpen = Sim.Clock.Phase == MarketPhase.Phase_Open;

            if (ticks > 0)
            {
                Sim.Step(ticks);

                // 틱을 돌다가 장이 저절로 닫힐 수 있다. 넘기기 단추를 안 눌러도 주기는 흘러야 한다.
                if (wasOpen == true && Sim.Clock.Phase == MarketPhase.Phase_Closed)
                {
                    RollCycle();
                }

                if (AutoPauseOnOutcome == true && Sim.HasPendingAlert == true
                    && Sim.HasBriefing == false)
                {
                    Sim.Clock.SetPaused(true);
                    Message = StringConstants.TEXT_AUTO_PAUSED;
                }
            }

            // 팝업 한 벌은 늦게 붙어도 된다. 화면을 만들 때 한 번만 시도하면
            // 그 순간 자산이 준비되지 않았을 때 영영 붙지 않는다.
            BuildPopup();

            // 지하 방에서는 거래판을 처음 열 때 안내한다. 닫힌 판 위에 안내를 띄우면 가리킬 것이 없다.
            if (IsPanelOpen == true)
            {
                StartTutorialOnce();
            }

            OpenIfDue();
            RefreshQuest();

            // 보고는 틱과 무관하게 연다. 틱이 멈춘 뒤에 걸려 있으면 영영 안 열린다.
            if (Sim.HasBriefing == true && SceneIndex < 0 && (Tutorial == null || Tutorial.IsOpen == false))
            {
                // 정보 타임이 오면 1배로 돌린다. 빠르게 흘리다 소식이 지나가면 반응할 틈이 없다.
                Sim.Clock.SetSpeedScale(1f);

                if (RoomMode == true)
                {
                    WaitForInformant();
                }
                else
                {
                    OpenScene();
                }
            }

            ApplyPanel();

            Refresh();
        }

        /// <summary>
        /// 거래소에서 끝나는 할 일(첫 거래 등)의 완료 알림. 보상은 지갑에 바로 들어간다.
        /// 할 일 목록은 거래소에 띄우지 않는다 — 영지에서만 본다(2026-10-17 사용자 요청).
        /// </summary>
        private void RefreshQuest()
        {
            var done = Story.QuestLog.Tick();

            if (done != null)
            {
                if (done.Reward > 0f)
                {
                    Sim.Wallet.Earn(done.Reward);
                }

                ShowToast(StringConstants.TEXT_QUEST_TAB, done.Reward > 0f
                    ? string.Format(StringConstants.TEXT_QUEST_DONE_REWARD, done.Title, done.Reward)
                    : string.Format(StringConstants.TEXT_QUEST_DONE, done.Title), Side, true);
            }
        }

        /// <summary>
        /// 영지에서 다음 장 직전까지 시간을 보내고 들어왔으면 그 장을 바로 연다.
        /// 「오후장 열기」를 따로 누르게 하면 영지의 시계와 거래소가 어긋나 보인다(2026-10-17 사용자 보고).
        /// 오후장이 끝난 밤이면 열지 않는다 — 다음 날은 영지에서 잠자리에 들어야 온다.
        /// </summary>
        private void OpenIfDue()
        {
            var clock = Sim.Clock;

            if (clock.Phase != MarketPhase.Phase_Closed || clock.IsBreak == false
                || clock.BreakMinutesLeft > Constants.BREAK_SKIP_MARGIN_MINUTES
                || (Tutorial != null && Tutorial.IsOpen == true))
            {
                return;
            }

            SkipOrOpen();
        }

        /// <summary>
        /// 처음 들어온 거래소를 한 곳씩 비추며 설명한다. 그동안 시간을 멈춘다.
        /// 건너뛰어도 다시 나오지 않는다. 새 게임(인트로)에서 다시 나온다.
        /// </summary>
        private void StartTutorialOnce()
        {
            if (Tutorial == null)
            {
                Tutorial = new UI_Tutorial(Root, StringConstants.TEXT_TUTORIAL_NEXT, StringConstants.TEXT_TUTORIAL_DONE,
                    StringConstants.TEXT_TUTORIAL_SKIP);
            }

            Tutorial.Tick();

            if (Story.StoryProgress.MarketTutorialDone == true || Tutorial.IsOpen == true || SceneIndex >= 0)
            {
                return;
            }

            Story.StoryProgress.MarketTutorialDone = true;
            PausedForTutorial = Sim.Clock.IsPaused == false;
            Sim.Clock.SetPaused(true);

            Tutorial.Play(new List<TutorialStep>
            {
                new TutorialStep(null, StringConstants.TEXT_TUT_MARKET_1_TITLE, StringConstants.TEXT_TUT_MARKET_1_BODY),
                new TutorialStep("stock-list", StringConstants.TEXT_TUT_MARKET_2_TITLE,
                    StringConstants.TEXT_TUT_MARKET_2_BODY),
                new TutorialStep("chart", StringConstants.TEXT_TUT_MARKET_3_TITLE, StringConstants.TEXT_TUT_MARKET_3_BODY),
                new TutorialStep("book-list", StringConstants.TEXT_TUT_MARKET_4_TITLE,
                    StringConstants.TEXT_TUT_MARKET_4_BODY),
                new TutorialStep("submit-button", StringConstants.TEXT_TUT_MARKET_5_TITLE,
                    StringConstants.TEXT_TUT_MARKET_5_BODY),
                new TutorialStep("asset-row", StringConstants.TEXT_TUT_MARKET_6_TITLE,
                    StringConstants.TEXT_TUT_MARKET_6_BODY),
                new TutorialStep("info-popup", StringConstants.TEXT_TUT_MARKET_7_TITLE,
                    StringConstants.TEXT_TUT_MARKET_7_BODY),
                new TutorialStep("header-status", StringConstants.TEXT_TUT_MARKET_8_TITLE,
                    StringConstants.TEXT_TUT_MARKET_8_BODY)
            }, () =>
            {
                if (PausedForTutorial == true)
                {
                    Sim.Clock.SetPaused(false);
                }
            });
        }

        /// <summary>
        /// 어두운 테마를 덮어씌운다. 색 토큰만 바꾸는 시트라 나중에 붙이기만 하면 된다.
        /// 재생 중 F2로 끄고 켜며 비교할 수 있다.
        /// </summary>
        private void ApplyTheme()
        {
            SetSheet(UndergroundTheme, Theme == ScreenTheme.Theme_Underground);
            SetSheet(ParchmentTheme, Theme == ScreenTheme.Theme_Parchment);
        }

        private void SetSheet(StyleSheet sheet, bool on)
        {
            if (sheet == null)
            {
                return;
            }

            var has = Root.styleSheets.Contains(sheet);

            if (on == true && has == false)
            {
                Root.styleSheets.Add(sheet);
                return;
            }

            if (on == false && has == true)
            {
                Root.styleSheets.Remove(sheet);
            }
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.F2)
            {
                return;
            }

            ToggleTheme();
            evt.StopPropagation();
        }

        /// <summary>재생 중 F2로 테마를 돌려 가며 비교한다.</summary>
        public void ToggleTheme()
        {
            switch (Theme)
            {
                case ScreenTheme.Theme_Light:
                {
                    Theme = ScreenTheme.Theme_Underground;
                    break;
                }
                case ScreenTheme.Theme_Underground:
                {
                    Theme = ScreenTheme.Theme_Parchment;
                    break;
                }
                default:
                {
                    Theme = ScreenTheme.Theme_Light;
                    break;
                }
            }

            ApplyTheme();
        }

        // ── 구성 ──────────────────────────────────

        /// <summary>
        /// 눈여겨보는 종목을 목록 위로 올린다.
        /// 상장이 늘면 목록이 길어지는데, 그때 매번 찾아 내려가는 것은 일이 된다.
        /// </summary>
        private void ToggleFavourite(string stockId)
        {
            if (Favourites.Remove(stockId) == false)
            {
                Favourites.Add(stockId);
            }

            BuildStockRows();
        }

        /// <summary>팝업 한 벌을 화면에 붙인다. 띄우는 것은 필요할 때다.</summary>
        private void BuildPopup()
        {
            if (PopupTree != null && Popup == null)
            {
                Popup = new UI_Popup(Root, PopupTree);
            }

            if (DialogueTree != null && Dialogue == null)
            {
                Dialogue = new UI_Dialogue(Root, DialogueTree);
            }
        }

        private void BuildStockRows()
        {
            var list = Root.Q<VisualElement>("stock-list");

            if (list == null)
            {
                return;
            }

            list.Clear();
            StockRows.Clear();
            RowStockIds.Clear();

            // 즐겨찾기가 위로 온다. 그 안에서는 상장 순서를 그대로 둔다.
            var stocks = new List<AdventurerStock>(Sim.Stocks);
            stocks.Sort(CompareByFavourite);

            for (var i = 0; i < stocks.Count; i++)
            {
                var stock = stocks[i];

                // 떠난 종목은 목록에서 뺀다. 청산은 쪽지로 알렸고 정보 기록에도 남는다.
                // 40주기를 돌면 떠난 종목이 수십 개가 되는데 그걸 다 쌓아 둘 수는 없다.
                if (stock.IsTradable == false)
                {
                    continue;
                }

                var row = new VisualElement();
                row.AddToClassList("stock-row");

                var top = new VisualElement();
                top.AddToClassList("stock-line");

                var star = new Button();
                star.AddToClassList("star");
                star.EnableInClassList("star--on", Favourites.Contains(stock.Id));
                var starId = stock.Id;
                star.clicked += () => ToggleFavourite(starId);

                var name = new Label(stock.DisplayName);
                name.AddToClassList("stock-name");
                var price = new Label("0.0");
                price.AddToClassList("stock-price");
                price.name = "price";
                var change = new Label("0.0%");
                change.AddToClassList("stock-change");
                change.name = "change";

                top.Add(star);
                top.Add(name);
                top.Add(price);
                top.Add(change);

                var sub = new Label(StringConstants.TEXT_STOCK_HOLD_EMPTY);
                sub.AddToClassList("t-note");
                sub.name = "sub";

                row.Add(top);
                row.Add(sub);

                // 떠나기로 공고된 종목은 그 사실을 행에 적는다. 설계안 §5가 사전 확인을 요구한다.
                if (stock.NoticedDelistCycle > 0)
                {
                    var line = new VisualElement();
                    line.AddToClassList("notice-line");

                    var mark = new VisualElement();
                    mark.AddToClassList("icon");
                    mark.AddToClassList("icon--warning");

                    var notice = new Label(string.Format(StringConstants.TEXT_DELIST_ROW, stock.LiquidationRatio * 100f));
                    notice.AddToClassList("stock-notice");

                    line.Add(mark);
                    line.Add(notice);
                    row.Add(line);
                }

                var id = stock.Id;
                row.RegisterCallback<ClickEvent>(evt => SelectStock(id));

                list.Add(row);
                StockRows.Add(row);
                RowStockIds.Add(stock.Id);
            }

            // 다음 계절에 상장될 단. 이미 영지에 와 있다 — 상장 전에 캐 두면 공모가에 산다(상장흐름 설계안 §3).
            // 목록 아래 빈자리를 채우는 것이기도 하다(2026-10-18 교차 검수).
            var arrived = Sim.ArrivedNames();

            if (arrived.Count == 0)
            {
                return;
            }

            var head = new Label(StringConstants.TEXT_ARRIVALS_HEAD);
            head.AddToClassList("arrivals-head");
            list.Add(head);

            for (var i = 0; i < arrived.Count; i++)
            {
                var line = new Label(string.Format(StringConstants.TEXT_ARRIVAL_ROW, arrived[i]));
                line.AddToClassList("arrival-row");
                list.Add(line);
            }
        }

        private void BindButtons()
        {
            Click("pause-button", () => Sim.Clock.SetPaused(Sim.Clock.IsPaused == false));
            Click("speed-1", () => Sim.Clock.SetSpeedScale(1f));
            Click("speed-4", () => Sim.Clock.SetSpeedScale(4f));
            Click("speed-cycle", CycleSpeed);
            Click("skip-button", SkipOrOpen);
            Click("place-button", () => Sim.PlayerAtMarket = Sim.PlayerAtMarket == false);
            Click("village-button", GoToVillage);

            Click("side-buy", () => Side = OrderSide.Side_Buy);
            Click("side-sell", () => Side = OrderSide.Side_Sell);

            Click("type-market", () => Type = OrderType.Type_Market);
            Click("type-limit", () => SwitchToLimit());
            Click("limit-minus", () => NudgeLimit(-1));
            Click("limit-plus", () => NudgeLimit(1));

            Click("qty-minus", () => Quantity = Mathf.Max(QUANTITY_MIN, Quantity - QUANTITY_STEP));
            Click("qty-plus", () => Quantity = Mathf.Min(QUANTITY_MAX, Quantity + QUANTITY_STEP));

            BindQuantityField();
            BindLimitField();

            Click("ratio-25", () => ApplyRatio(0.25f));
            Click("ratio-50", () => ApplyRatio(0.5f));
            Click("ratio-75", () => ApplyRatio(0.75f));
            Click("ratio-100", () => ApplyRatio(1f));

            var head = Root.Q<VisualElement>("info-head");

            if (head != null)
            {
                // 필터 버튼을 누를 때는 접히지 않아야 한다. 버튼이 이벤트를 먼저 먹는다.
                head.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is Button)
                    {
                        return;
                    }

                    InfoCollapsed = InfoCollapsed == false;
                });
            }

            var holdingsHead = Root.Q<VisualElement>("holdings-head");

            if (holdingsHead != null)
            {
                holdingsHead.RegisterCallback<ClickEvent>(evt =>
                {
                    HoldingsCollapsed = HoldingsCollapsed == false;
                    HoldingsSeqShown = -1;
                });
            }

            Click("filter-mine", () => SetFilter(InfoFilter.Filter_Mine));
            Click("filter-all", () => SetFilter(InfoFilter.Filter_All));
            Click("filter-private", () => SetFilter(InfoFilter.Filter_Private));
            Click("filter-bands", () => SetFilter(InfoFilter.Filter_Bands));

            Click("submit-button", Submit);

            var chart = Root.Q<VisualElement>("chart");

            if (chart != null)
            {
                chart.generateVisualContent += DrawChart;
            }
        }

        private void Click(string elementName, System.Action action)
        {
            var button = Root.Q<Button>(elementName);

            if (button == null)
            {
                UnderwriteLog.Warning($"UXML에 '{elementName}' 버튼이 없습니다.", context: this);
                return;
            }

            button.clicked += action;
        }

        /// <summary>즐겨찾기를 위로 올린다. 같은 무리 안에서는 순서를 바꾸지 않는다.</summary>
        private int CompareByFavourite(AdventurerStock left, AdventurerStock right)
        {
            var leftStar = Favourites.Contains(left.Id) ? 0 : 1;
            var rightStar = Favourites.Contains(right.Id) ? 0 : 1;

            if (leftStar != rightStar)
            {
                return leftStar.CompareTo(rightStar);
            }

            return StockOrderOf(left).CompareTo(StockOrderOf(right));
        }

        /// <summary>상장 순서. IReadOnlyList에는 IndexOf가 없어 직접 센다.</summary>
        private int StockOrderOf(AdventurerStock stock)
        {
            for (var i = 0; i < Sim.Stocks.Count; i++)
            {
                if (ReferenceEquals(Sim.Stocks[i], stock) == true)
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private void SelectStock(string id)
        {
            SelectedId = id;
            InfoCountShown = -1;
            BookTickShown = -1;
            LimitTouched = false;
        }

        private void SetFilter(InfoFilter filter)
        {
            Filter = filter;
            InfoCountShown = -1;
        }

        /// <summary>
        /// 장중이면 남은 틱을 전부 돌려 폐장시키고, 폐장 상태면 다음 장을 연다.
        /// 기획서 §11의 "사건이 적은 기간은 넘긴다"를 배속 대신 이 버튼으로 처리한다.
        /// 16배속은 숫자가 눈에 남지 않아 뺐다.
        /// </summary>
        private void SkipOrOpen()
        {
            if (Sim.Clock.Phase == MarketPhase.Phase_Closed)
            {
                Sim.Clock.EndBreak();
                ReportSkippedSessions();
                Sim.OpenNextSession(IsInspectionDay);
                CheckPatronageDeadlines();

                var brought = Sim.ApplyGatherSession();

                if (brought > 0)
                {
                    ShowToast(StringConstants.TEXT_TOAST_BROUGHT_TITLE, string.Format(StringConstants.TEXT_TOAST_BROUGHT_BODY, brought),
                        Side, true);
                }

                GatherSession.BeginBreak(Sim.NextBriefingSlot, Sim.Wallet.AvailableCash);
                ResetViewState();
                ShowReopening();
                Message = string.Format(StringConstants.TEXT_SESSION_OPENED,
                    SessionName(Sim.Clock.Session));
                return;
            }

            var before = Sim.InfoLog.Count;
            var filled = Sim.SkipToClose();
            var news = Sim.InfoLog.Count - before;
            RollCycle();
            ResetViewState();

            Message = filled > 0
                ? string.Format(StringConstants.TEXT_SESSION_SKIPPED_FILLED, news, filled)
                : string.Format(StringConstants.TEXT_SESSION_SKIPPED, news);
        }

        /// <summary>
        /// 영지에서 쉬기로 한 장을 치르고, 그동안 무슨 일이 있었는지 알린다.
        ///
        /// 쉰 대가는 숫자로 보여야 한다. 그러지 않으면 쉬는 것이 공짜처럼 느껴지고,
        /// 플레이어가 고르는 일이 아니게 된다. 자산이 얼마나 움직였는지를 적는다.
        /// </summary>
        private void ReportSkippedSessions()
        {
            if (Sim.Clock.SkippedSessions <= 0)
            {
                return;
            }

            var beforeValue = Sim.Wallet.TotalValue(Sim.StockLookup);
            var beforeNews = Sim.InfoLog.Count;

            var sessions = Sim.RunSkippedSessions(IsInspectionDay);

            if (sessions <= 0)
            {
                return;
            }

            var moved = Sim.Wallet.TotalValue(Sim.StockLookup) - beforeValue;
            var news = Sim.InfoLog.Count - beforeNews;
            var sign = moved >= 0f ? "+" : string.Empty;

            ShowToast(StringConstants.TEXT_TOAST_AWAY_TITLE,
                string.Format(StringConstants.TEXT_TOAST_AWAY_BODY, sessions, sign, moved, news),
                Side, true);
        }

        /// <summary>장이 끝났음을 시장에 알린다. 계절 넘김의 소식은 재개장 때 <see cref="ShowReopening"/>이 띄운다.</summary>
        private void RollCycle()
        {
            Sim.EndSession();
            CheckBankruptcy();
        }

        /// <summary>
        /// 재개장 장이 열렸으면 원정 결과의 갭과 계절 넘김의 소식(상장·상장 폐지·도착)을 쪽지로 띄운다.
        /// 상장과 상장 폐지는 놓치면 안 되는 소식이라 흘려보내지 않는다.
        /// </summary>
        private void ShowReopening()
        {
            if (Sim.JustReopened == false)
            {
                return;
            }

            var title = ClockText.SeasonDay(Sim.Clock.Day);
            ShowToast(title, string.Format(StringConstants.TEXT_REOPEN_GAP, Sim.ReopenGapUp, Sim.ReopenGapDown),
                Side, true);

            // 계절 계획으로 준비해 둔 것. 새 단이 막 도착했으니 지금 푼다.
            var prepared = SeasonPlan.ApplyAtReopening(Sim);

            for (var i = 0; i < prepared.Count; i++)
            {
                ShowToast(StringConstants.TEXT_PLAN_RESULT_TITLE, prepared[i], Side, true);
            }

            var liquidation = string.Empty;

            for (var i = 0; i < Sim.CycleNotices.Count; i++)
            {
                var notice = Sim.CycleNotices[i];

                // 청산은 돈을 잃는 사건이다. 흘려보내지 않고 확인을 받는다.
                if (notice.Contains(StringConstants.TEXT_NOTICE_SOLD_MARK) == true)
                {
                    liquidation = liquidation.Length == 0 ? notice : liquidation + "\n" + notice;
                    continue;
                }

                ShowToast(title, notice, Side, true);
            }

            if (liquidation.Length > 0 && Popup != null)
            {
                Sim.Clock.SetPaused(true);
                Popup.Show(title, StringConstants.TEXT_POPUP_DELIST_TITLE, liquidation, StringConstants.TEXT_POPUP_OK, null,
                    () => Sim.Clock.SetPaused(false), null);
            }

            if (Sim.CycleNotices.Count > 0)
            {
                BuildStockRows();
            }
        }

        /// <summary>
        /// 영지로 나간다. 장중이면 그 장을 마감하고 나간다.
        ///
        /// 기획서 §11의 하루는 장 → 영지 → 장 → 취침으로 돈다.
        /// 장중에 나갈 수 있으면 가격이 흐르는 동안 정보를 캐게 되어 두 층이 겹친다.
        /// </summary>
        private void GoToVillage()
        {
            // 장중에 나가면 그 장은 끝까지 흘려보낸다. 장중에 영지를 돌면 두 층이 겹치므로
            // 나가는 순간 장을 마감한다(2026-10-17 사용자 결정 — 단추는 「끝내기」와 「나가기」 둘뿐).
            if (Sim.Clock.Phase != MarketPhase.Phase_Closed)
            {
                SkipOrOpen();
            }

            // 이미 영지의 시간이 흐르던 중이면(영지 → 거래소 → 영지) 그대로 잇는다.
            // 새로 시작하면 시각이 장 끝으로 되돌아가고, 영지에서 번 돈이 장부에서 사라진다.
            if (Sim.Clock.IsBreak == false)
            {
                GatherSession.BeginBreak(Sim.NextBriefingSlot, Sim.Wallet.AvailableCash);
                Sim.Clock.BeginBreak();
            }

            SceneManager.LoadScene(SceneRoutes.VILLAGE);
        }

        /// <summary>
        /// 날이 바뀌었으면 어제로 끝난 용사단 사정의 기한을 확인한다.
        /// 영지에서 잠자리에 들지 않고 거래소에서만 날을 넘겨도 기한은 흐른다. 이미 확인한 날은 다시 세지 않는다.
        /// </summary>
        private void CheckPatronageDeadlines()
        {
            if (Sim.Clock.Session != MarketSession.Session_Morning)
            {
                return;
            }

            if (PatronageLedger.EndOfDay(Sim.Clock.Day - 1) > 0)
            {
                ShowToast(StringConstants.TEXT_HERO_PARTY, StringConstants.TEXT_DAY_END_FAILED, Side, false);
            }

            ApplyCollapse();
        }

        /// <summary>
        /// 용사단이 사정을 놓친 대가(부상·죽음)를 용사단 종목에 반영한다. 영지에서 놓쳤어도 거래소에 들어오는 순간 반영된다.
        /// </summary>
        private void ApplyCollapse()
        {
            if (PatronageLedger.PendingIndexShock == 0f)
            {
                return;
            }

            var note = Sim.ShockIndex(Constants.HERO_STOCK_ID, PatronageLedger.PendingIndexShock);
            PatronageLedger.ClearIndexShock();

            if (note != null)
            {
                ShowToast(StringConstants.TEXT_HERO_PARTY, note, Side, false);
                ResetViewState();
            }
        }

        /// <summary>
        /// 장이 끝났는데 가진 것이 바닥이면 파산이다(2026-10-18 인터뷰). 첫 번째는 정보상이 재기 자금을 쥐여 준다 —
        /// 파산할 수 있다는 것을 몸으로 알게 하려는 것이다. 두 번째는 게임오버다.
        /// </summary>
        private void CheckBankruptcy()
        {
            if (Sim.IsBankrupt == false || Popup == null)
            {
                return;
            }

            Sim.Clock.SetPaused(true);

            if (StoryProgress.Bankruptcies == 0)
            {
                StoryProgress.Bankruptcies = 1;
                Sim.Rescue(Constants.BANKRUPT_RESCUE_CASH);
                Popup.Show(StringConstants.TEXT_BANKRUPT_TAG, StringConstants.TEXT_BANKRUPT_RESCUE_TITLE,
                    string.Format(StringConstants.TEXT_BANKRUPT_RESCUE_BODY, Constants.BANKRUPT_RESCUE_CASH),
                    StringConstants.TEXT_BANKRUPT_RESCUE_OK, null, () => Sim.Clock.SetPaused(false), null);
                return;
            }

            // 판이 끝났다. 이어할 것이 없다 — 다음은 처음부터다.
            SaveSystem.Delete();
            Popup.Show(StringConstants.TEXT_BANKRUPT_TAG, StringConstants.TEXT_BANKRUPT_END_TITLE,
                StringConstants.TEXT_BANKRUPT_END_BODY, StringConstants.TEXT_BANKRUPT_END_OK, null,
                () => SceneManager.LoadScene(SceneRoutes.INTRO), null);
        }

        /// <summary>「지하시장 나가기」는 언제나 누를 수 있다. 장중이면 그 장을 마감하고 나간다.</summary>
        private void RefreshVillageButton()
        {
            var button = Root.Q<Button>("village-button");

            if (button == null)
            {
                return;
            }

            button.text = StringConstants.TEXT_LEAVE_MARKET;
            button.SetEnabled(true);
            button.EnableInClassList("btn--disabled", false);
        }

        /// <summary>화면이 다시 그려지도록 캐시를 비운다.</summary>
        private void ResetViewState()
        {
            InfoCountShown = -1;
            ChartTickShown = -1;
            BookTickShown = -1;
            PendingCountShown = -1;
            HoldingsSeqShown = -1;
        }

        private void RefreshSkipButton()
        {
            var skip = Root.Q<Button>("skip-button");

            if (skip == null)
            {
                return;
            }

            // 장이 끝나면 지하세계는 닫힌다. 다음 장은 영지에서 시간을 보낸 뒤 내려와야 열린다
            // (2026-10-17 사용자 결정). 닫힌 동안에는 「지하시장 나가기」만 남긴다.
            var closed = Sim.Clock.Phase == MarketPhase.Phase_Closed;
            skip.EnableInClassList("hidden", closed);

            if (Sim.Clock.Phase == MarketPhase.Phase_Closed)
            {
                skip.text = string.Format(StringConstants.TEXT_SESSION_OPEN_NEXT, StringConstants.TEXT_SESSION_EVENING);
                return;
            }

            skip.text = string.Format(StringConstants.TEXT_SESSION_END, SessionName(Sim.Clock.Session));
        }

        /// <summary>
        /// 배속을 돌린다. 1배 → 2배 → 4배 → 1배. 한 장이 약 9분 → 4분 30초 → 2분 15초가 된다.
        /// 장중에 영지를 돌아다니는 대신 고른 방법이다(2026-10-17). 두 시간 축(장 압축 / 영지 1:1)을 섞지 않는다.
        /// </summary>
        private void CycleSpeed()
        {
            var next = Sim.Clock.SpeedScale >= 4f ? 1f : Sim.Clock.SpeedScale * 2f;
            Sim.Clock.SetSpeedScale(next);
        }

        private static string SessionName(MarketSession session)
        {
            return session == MarketSession.Session_Evening
                ? StringConstants.TEXT_SESSION_EVENING
                : StringConstants.TEXT_SESSION_MORNING;
        }

        private void Submit()
        {
            var verb = Side == OrderSide.Side_Buy
                ? StringConstants.TEXT_SIDE_BUY
                : StringConstants.TEXT_SIDE_SELL;

            if (Type == OrderType.Type_Limit)
            {
                var queued = Sim.SubmitLimitOrder(SelectedId, Side, Quantity, LimitPrice);

                if (queued.Result == OrderResult.Result_Queued)
                {
                    Message = string.Format(StringConstants.TEXT_ORDER_QUEUED_MESSAGE, queued.FillPrice, queued.Quantity, verb);
                    ShowToast(string.Format(StringConstants.TEXT_ORDER_QUEUED_TITLE, verb),
                        string.Format(StringConstants.TEXT_ORDER_QUEUED_BODY, StockName(SelectedId), queued.Quantity,
                            queued.FillPrice), Side, true);
                    PendingCountShown = -1;
                    return;
                }

                Message = queued.Message;
                ShowToast(StringConstants.TEXT_ORDER_BLOCKED_TITLE, queued.Message, Side, false);
                return;
            }

            var receipt = Sim.SubmitOrder(SelectedId, Side, Quantity);

            if (receipt.Result == OrderResult.Result_Filled)
            {
                GameAudio.Play(SoundEffect.Sound_Coins);
            }

            if (receipt.Result == OrderResult.Result_Filled)
            {
                Message = string.Format(StringConstants.TEXT_ORDER_FILLED_MESSAGE, verb, receipt.Quantity, receipt.FillPrice,
                    receipt.TotalAmount);
                ShowToast(string.Format(StringConstants.TEXT_ORDER_FILLED_TITLE, verb),
                    string.Format(StringConstants.TEXT_ORDER_FILLED_BODY, StockName(SelectedId), receipt.Quantity,
                        receipt.FillPrice, receipt.TotalAmount), Side, true);
                return;
            }

            Message = receipt.Message;
            ShowToast(StringConstants.TEXT_ORDER_BLOCKED_TITLE, receipt.Message, Side, false);
        }

        /// <summary>
        /// 수량은 직접 친다. +/− 는 보조다.
        /// 지우는 중에는 빈 칸이 되므로 그때 되돌리면 글자를 칠 수 없다. 칸을 떠날 때 바로잡는다.
        /// </summary>
        private void BindQuantityField()
        {
            var field = Root.Q<TextField>("qty-value");

            if (field == null)
            {
                return;
            }

            field.isDelayed = false;

            field.RegisterValueChangedCallback(evt =>
            {
                int typed;

                if (int.TryParse(evt.newValue, out typed) == false)
                {
                    return;
                }

                Quantity = Mathf.Clamp(typed, QUANTITY_MIN, QUANTITY_MAX);
            });

            field.RegisterCallback<BlurEvent>(evt => field.SetValueWithoutNotify(Quantity.ToString()));
        }

        /// <summary>지정가도 직접 친다. 칸을 떠날 때 호가 단위로 맞춘다.</summary>
        private void BindLimitField()
        {
            var field = Root.Q<TextField>("limit-value");

            if (field == null)
            {
                return;
            }

            field.isDelayed = false;

            field.RegisterValueChangedCallback(evt =>
            {
                float typed;

                if (float.TryParse(evt.newValue, out typed) == false)
                {
                    return;
                }

                if (typed <= 0f)
                {
                    return;
                }

                LimitPrice = typed;
                LimitTouched = true;
                Type = OrderType.Type_Limit;
            });

            field.RegisterCallback<BlurEvent>(evt =>
            {
                AdventurerStock stock;

                if (Sim.StockLookup.TryGetValue(SelectedId, out stock) == true)
                {
                    LimitPrice = OrderBook.SnapToTick(LimitPrice, stock.Price);
                }

                field.SetValueWithoutNotify(LimitPrice.ToString("N1"));
            });
        }

        /// <summary>
        /// 입력칸의 글자를 코드에서 덮어쓴다.
        /// 치고 있는 칸을 건드리면 커서가 튀므로 포커스를 가진 칸은 두고 본다.
        /// </summary>
        private void SetFieldValue(string elementName, string value)
        {
            var field = Root.Q<TextField>(elementName);

            if (field == null)
            {
                return;
            }

            if (HasFocus(field) == true)
            {
                return;
            }

            if (field.value == value)
            {
                return;
            }

            field.SetValueWithoutNotify(value);
        }

        private static bool HasFocus(VisualElement element)
        {
            if (element.focusController == null)
            {
                return false;
            }

            var focused = element.focusController.focusedElement as VisualElement;

            if (focused == null)
            {
                return false;
            }

            if (ReferenceEquals(focused, element) == true)
            {
                return true;
            }

            return element.Contains(focused);
        }

        /// <summary>
        /// 현금이나 보유 수량의 몇 할로 수량을 채운다.
        /// 매수는 충격 때문에 체결가가 현재가보다 비싸므로 한 번 더 줄여 잡는다.
        /// 모자라게 잡는 것은 괜찮지만 넘치면 주문이 거절된다.
        /// </summary>
        private void ApplyRatio(float ratio)
        {
            AdventurerStock stock;

            if (Sim.StockLookup.TryGetValue(SelectedId, out stock) == false)
            {
                return;
            }

            if (Side == OrderSide.Side_Sell)
            {
                var owned = Sim.Wallet.AvailableQuantityOf(stock.Id);
                Quantity = Mathf.Clamp(Mathf.FloorToInt(owned * ratio), QUANTITY_MIN, QUANTITY_MAX);
                return;
            }

            var budget = Sim.Wallet.AvailableCash * ratio;
            var unit = Type == OrderType.Type_Limit ? LimitPrice : stock.LivePrice;

            if (unit <= 0f)
            {
                return;
            }

            var quantity = Mathf.FloorToInt(budget / unit);

            // 시장가는 주문이 가격을 밀어 올린다. 예상 체결가로 다시 재어 넘치면 줄인다.
            if (Type == OrderType.Type_Market)
            {
                for (var i = 0; i < RATIO_FIT_STEPS && quantity > 0; i++)
                {
                    float fill;
                    float last;
                    OrderBook.Sweep(stock, Sim.MicroSeq, Side, quantity, out fill, out last);

                    if (fill * quantity <= budget)
                    {
                        break;
                    }

                    quantity = Mathf.FloorToInt(budget / fill);
                }
            }

            Quantity = Mathf.Clamp(quantity, QUANTITY_MIN, QUANTITY_MAX);
        }

        /// <summary>지정가로 바꿀 때 아직 손대지 않았으면 현재가를 기본값으로 채운다.</summary>
        private void SwitchToLimit()
        {
            Type = OrderType.Type_Limit;

            if (LimitTouched == true)
            {
                return;
            }

            AdventurerStock stock;

            if (Sim.StockLookup.TryGetValue(SelectedId, out stock) == true)
            {
                LimitPrice = OrderBook.SnapToTick(stock.Price, stock.Price);
                LimitTouched = true;
            }
        }

        private void NudgeLimit(int steps)
        {
            AdventurerStock stock;

            if (Sim.StockLookup.TryGetValue(SelectedId, out stock) == false)
            {
                return;
            }

            var step = OrderBook.TickSize(stock.Price);
            LimitPrice = OrderBook.SnapToTick(LimitPrice + step * steps, stock.Price);
            LimitTouched = true;
            Type = OrderType.Type_Limit;
        }

        /// <summary>호가를 누르면 그 가격이 지정가로 들어간다. 증권사 앱의 표준 동작이다.</summary>
        private void PickBookPrice(float price)
        {
            LimitPrice = price;
            LimitTouched = true;
            Type = OrderType.Type_Limit;
        }

        // ── 갱신 ──────────────────────────────────

        private void Refresh()
        {
            RefreshHeader();
            RefreshAssets();
            RefreshStocks();
            RefreshChart();
            RefreshBook();
            RefreshOrder();
            RefreshPending();
            RefreshInfo();
            RefreshHoldings();
        }

        private void RefreshHeader()
        {
            var phase = Root.Q<Label>("phase-chip");

            if (phase != null)
            {
                phase.text = PhaseText(Sim.Clock.Phase);
                phase.EnableInClassList("status-chip--halt", Sim.Clock.Phase == MarketPhase.Phase_Halted);
            }

            var label = Sim.Clock.Session == MarketSession.Session_Evening
                ? StringConstants.TEXT_SESSION_EVENING
                : StringConstants.TEXT_SESSION_MORNING;
            // 장이 닫혀 영지의 시간이 흐르는 동안에는 영지 시각을 보인다. 장 끝 시각에 멈춰 있으면
            // 영지에서 시간을 넘기고 와도 갱신이 안 된 것처럼 보인다(2026-10-17 사용자 보고).
            var seconds = Sim.Clock.Phase == MarketPhase.Phase_Closed && Sim.Clock.IsBreak == true
                ? Sim.Clock.BreakMinuteOfDay * 60 + Sim.Clock.BreakSecond
                : Sim.Clock.ClockTotalSeconds;
            SetText("clock-text",
                // 며칠째인지 맨 앞에 — 영지 시계와 같은 순서·같은 말(2026-10-17 사용자 요청).
                ClockText.SeasonDay(Sim.Clock.CalendarDay)
                + string.Format(StringConstants.TEXT_CLOCK_SESSION, label, ClockText.FromSeconds(seconds, true))
                + string.Format(StringConstants.TEXT_CLOCK_TICKS, Sim.Clock.Tick, Sim.Clock.TicksPerSession));
            SetText("sentiment-text", string.Format(StringConstants.TEXT_SENTIMENT, SentimentText(Sim.Sentiment)));

            var pause = Root.Q<Button>("pause-button");

            if (pause != null)
            {
                pause.text = Sim.Clock.IsPaused == true ? StringConstants.TEXT_RESUME : StringConstants.TEXT_PAUSE;
            }

            var place = Root.Q<Button>("place-button");

            if (place != null)
            {
                place.text = Sim.PlayerAtMarket == true ? StringConstants.TEXT_PLACE_MARKET : StringConstants.TEXT_PLACE_MOVING;
            }

            SetButtonText("speed-cycle", string.Format(StringConstants.TEXT_SPEED, Mathf.RoundToInt(Sim.Clock.SpeedScale)));
            Select("speed-cycle", Sim.Clock.SpeedScale > 1f);
            Select("speed-1", Mathf.Approximately(Sim.Clock.SpeedScale, 1f));
            Select("speed-4", Mathf.Approximately(Sim.Clock.SpeedScale, 4f));
            RefreshSkipButton();
            RefreshVillageButton();

            var banner = Root.Q<VisualElement>("halt-banner");

            if (banner != null)
            {
                banner.EnableInClassList("hidden", Sim.Clock.Phase != MarketPhase.Phase_Halted);
            }
        }

        private void RefreshAssets()
        {
            SetText("asset-cash", Sim.Wallet.Cash.ToString("N0"));
            SetText("asset-holdings", Sim.Wallet.HoldingsValue(Sim.StockLookup).ToString("N0"));
            SetText("asset-total", Sim.Wallet.TotalValue(Sim.StockLookup).ToString("N0"));
            SetText("asset-realized", Sim.Wallet.RealizedProfit.ToString("N0"));
        }

        private void RefreshStocks()
        {
            // 목록에서 떠난 종목을 걸러내므로 인덱스가 Sim.Stocks와 어긋난다. 종목 id로 찾는다.
            for (var i = 0; i < RowStockIds.Count && i < StockRows.Count; i++)
            {
                AdventurerStock stock;

                if (Sim.StockLookup.TryGetValue(RowStockIds[i], out stock) == false)
                {
                    continue;
                }

                var row = StockRows[i];

                row.EnableInClassList("stock-row--selected", stock.Id == SelectedId);

                var price = row.Q<Label>("price");

                if (price != null)
                {
                    price.text = stock.LivePrice.ToString("N1");
                }

                var change = row.Q<Label>("change");

                if (change != null)
                {
                    ApplyChange(change, stock.SessionChangeRatio);
                }

                var sub = row.Q<Label>("sub");

                if (sub != null)
                {
                    Builder.Length = 0;
                    Builder.Append(StringConstants.TEXT_STOCK_HOLD_PREFIX).Append(Sim.Wallet.QuantityOf(stock.Id))
                        .Append(StringConstants.TEXT_STOCK_HOLD_MIDDLE);
                    Builder.Append(Sim.Wallet.AverageCostOf(stock.Id).ToString("N1"));
                    sub.text = Builder.ToString();
                }
            }
        }

        /// <summary>등락 표시. 색만으로 구분하지 않고 부호와 화살표를 함께 쓴다.</summary>
        private static void ApplyChange(Label label, float ratio)
        {
            var percent = ratio * 100f;

            label.RemoveFromClassList("rise");
            label.RemoveFromClassList("fall");
            label.RemoveFromClassList("flat");

            if (percent > 0.05f)
            {
                label.AddToClassList("rise");
                label.text = $"▲ +{percent:N1}%";
                return;
            }

            if (percent < -0.05f)
            {
                label.AddToClassList("fall");
                label.text = $"▼ −{Mathf.Abs(percent):N1}%";
                return;
            }

            label.AddToClassList("flat");
            label.text = "0.0%";
        }

        private void RefreshChart()
        {
            AdventurerStock stock;

            if (Sim.StockLookup.TryGetValue(SelectedId, out stock) == false)
            {
                return;
            }

            SetText("chart-name", stock.DisplayName);
            SetText("chart-price", stock.LivePrice.ToString("N1"));

            var halted = Sim.Clock.Phase == MarketPhase.Phase_Halted;
            SetText("chart-footer", halted == true
                ? StringConstants.TEXT_CHART_HALTED
                : string.Format(StringConstants.TEXT_CHART_FOOTER, Sim.Clock.TicksPerSession));

            var change = Root.Q<Label>("chart-change");

            if (change != null)
            {
                ApplyChange(change, stock.SessionChangeRatio);
            }

            // 매 프레임이 아니라 미세 호가가 바뀔 때만 다시 그린다.
            // 틱마다 한 번이면 7.5초에 점 하나라 선이 멈춰 있는 것처럼 보인다.
            // generateVisualContent 안에서는 트리를 건드리지 않으므로 여기서만 요청한다.
            if (ChartTickShown == Sim.MicroSeq)
            {
                return;
            }

            ChartTickShown = Sim.MicroSeq;
            var chart = Root.Q<VisualElement>("chart");

            if (chart != null)
            {
                chart.MarkDirtyRepaint();
            }

            RefreshChartMarkers(stock);
        }

        /// <summary>
        /// 차트 위에 얹는 글자를 모두 다시 앉힌다.
        /// 오른쪽에 가격 눈금과 현재가 꼬리표, 선 위에 저·시작·고 라벨이다.
        /// 값도 자리도 매 틱 바뀌므로 좌표를 여기서 계산해 절대 배치로 준다.
        /// </summary>
        private void RefreshChartMarkers(AdventurerStock stock)
        {
            var chart = Root.Q<VisualElement>("chart");

            if (chart == null)
            {
                return;
            }

            var width = chart.contentRect.width;
            var height = chart.contentRect.height;
            var history = stock.PriceHistory;

            if (width <= 0f || height <= 0f || history.Count < 1)
            {
                HideMarker(chart, "marker-low");
                HideMarker(chart, "marker-open");
                HideMarker(chart, "marker-high");
                HideMarker(chart, "price-tag");
                HideAxisLabels(chart, 0);
                return;
            }

            var scale = BuildScale(stock);
            var min = scale.Min;
            var span = scale.Span;
            var plotWidth = PlotWidth(width);
            var total = Sim.Clock.TicksPerSession;

            RefreshDayStats(stock, scale);

            // 오른쪽 가격 눈금. 값이 적혀 있지 않으면 선의 높낮이가 얼마인지 알 수 없다.
            var step = NiceStep(span / 4f);
            var first = Mathf.Ceil(min / step) * step;
            var priceY = ChartY(stock.LivePrice, min, span, height);
            var shown = 0;

            for (var i = 0; i < CHART_AXIS_MAX; i++)
            {
                var value = first + step * i;

                if (value > scale.Max)
                {
                    break;
                }

                var gridY = ChartY(value, min, span, height);

                // 현재가 꼬리표가 덮을 자리의 눈금은 적지 않는다. 숫자 두 개가 겹쳐 보인다.
                if (Mathf.Abs(gridY - priceY) < CHART_LABEL_EDGE)
                {
                    continue;
                }

                PlaceAxisLabel(chart, shown, value, PlotLeft() + plotWidth + 10f, gridY);
                shown++;
            }

            HideAxisLabels(chart, shown);

            // 현재가 꼬리표 — 증권사 차트처럼 오른쪽 끝에 지금 값을 붙인다.
            PlacePriceTag(chart, stock, min, span, width, height);

            var openY = ChartY(scale.OpenValue, min, span, height);

            // 시작 라벨은 늘 왼쪽 끝 점 오른쪽에 눕힌다. 위로 올리면 종목 이름·가격 글자와 겹친다.
            PlaceMarker(chart, "marker-open", "chart-marker-label--open",
                string.Format(StringConstants.TEXT_CHART_OPEN, scale.OpenValue), PlotLeft() + 12f, openY, plotWidth,
                true, MarkerAnchor.Anchor_Left);

            // 아직 시작가가 그대로 저가(고가)이면 같은 점에 라벨 두 개가 포개진다.
            // 그때는 저·고를 적지 않는다. 장이 흘러 새 저가가 찍히면 그 자리에 나타난다.
            if (IsOpenPoint(scale.LowIndex) == true)
            {
                HideMarker(chart, "marker-low");
            }
            else
            {
                var lowY = ChartY(scale.LowValue, min, span, height);
                PlaceMarker(chart, "marker-low", "chart-marker-label--low",
                    string.Format(StringConstants.TEXT_CHART_LOW, scale.LowValue), ChartXAt(scale.LowIndex, total, width), lowY, plotWidth,
                    lowY > height - CHART_LABEL_EDGE, MarkerAnchor.Anchor_Center);
            }

            if (IsOpenPoint(scale.HighIndex) == true)
            {
                HideMarker(chart, "marker-high");
            }
            else
            {
                var highY = ChartY(scale.HighValue, min, span, height);
                PlaceMarker(chart, "marker-high", "chart-marker-label--high",
                    string.Format(StringConstants.TEXT_CHART_HIGH, scale.HighValue), ChartXAt(scale.HighIndex, total, width), highY, plotWidth,
                    highY > CHART_LABEL_EDGE, MarkerAnchor.Anchor_Center);
            }
        }

        /// <summary>그 지점이 아직 장 시작점인가. 시작 라벨이 이미 그 자리를 적고 있다.</summary>
        private static bool IsOpenPoint(float index)
        {
            return index < 0.5f;
        }

        /// <summary>호가 패널 아래의 일중 요약. 시가는 고정, 저가·고가는 갱신될 때마다 바뀐다.</summary>
        private void RefreshDayStats(AdventurerStock stock, ChartScale scale)
        {
            SetText("day-open", scale.OpenValue.ToString("N1"));
            SetText("day-high", scale.HighValue.ToString("N1"));
            SetText("day-low", scale.LowValue.ToString("N1"));
        }

        /// <summary>라벨을 점의 어디에 붙일지.</summary>
        private enum MarkerAnchor
        {
            Anchor_Center,
            Anchor_Left
        }

        /// <summary>
        /// 차트 한 장의 세로 눈금과 세 지점.
        /// 선·점·라벨·눈금이 모두 같은 자를 써야 하므로 한 번만 재서 돌려 쓴다.
        /// </summary>
        private struct ChartScale
        {
            public float Min;
            public float Max;
            public float Span;
            public float OpenValue;
            public float LowValue;
            public float LowIndex;
            public float HighValue;
            public float HighIndex;
            public float LiveIndex;
        }

        /// <summary>
        /// 장 시작가와 그 뒤의 저가·고가를 찾는다.
        ///
        /// 시작가는 장이 열린 순간의 값으로 고정이다.
        /// 저가와 고가는 그 뒤에 실제로 찍힌 자리에 남고, 더 낮은(높은) 값이 나오면 그리로 옮겨간다.
        /// 지금 이 순간의 호가도 후보에 넣는다. 틱이 닫힐 때까지 기다리면 갱신이 한 박자 늦는다.
        /// </summary>
        private ChartScale BuildScale(AdventurerStock stock)
        {
            var history = stock.PriceHistory;
            var scale = new ChartScale();

            scale.OpenValue = stock.SessionOpenPrice;
            scale.LowValue = history[0];
            scale.HighValue = history[0];
            scale.LowIndex = 0f;
            scale.HighIndex = 0f;

            for (var i = 1; i < history.Count; i++)
            {
                if (history[i] < scale.LowValue)
                {
                    scale.LowValue = history[i];
                    scale.LowIndex = i;
                }

                if (history[i] > scale.HighValue)
                {
                    scale.HighValue = history[i];
                    scale.HighIndex = i;
                }
            }

            scale.LiveIndex = history.Count - 1 + Sim.Clock.TickFraction;
            var live = stock.LivePrice;

            if (live < scale.LowValue)
            {
                scale.LowValue = live;
                scale.LowIndex = scale.LiveIndex;
            }

            if (live > scale.HighValue)
            {
                scale.HighValue = live;
                scale.HighIndex = scale.LiveIndex;
            }

            // 위아래를 조금 비운다. 고·저 점이 테두리에 붙으면 라벨이 차트 밖으로 나간다.
            var pad = (scale.HighValue - scale.LowValue) * CHART_RANGE_PAD;

            if (pad <= 0.0001f)
            {
                pad = Mathf.Max(0.5f, scale.HighValue * 0.01f);
            }

            scale.Min = scale.LowValue - pad;
            scale.Max = scale.HighValue + pad;

            // 장이 막 열렸을 때는 고가와 저가가 붙어 있다. 그 폭에 눈금을 맞추면
            // 0.1짜리 미세 흔들림이 차트 높이를 다 쓰는 큰 파동처럼 보인다.
            // 최소 폭을 두면 초반에는 잔잔하고, 실제로 움직이기 시작하면 알아서 넓어진다.
            var minimumSpan = Mathf.Max(0.5f, scale.OpenValue * CHART_MIN_SPAN_RATIO);

            if (scale.Max - scale.Min < minimumSpan)
            {
                var center = (scale.Max + scale.Min) * 0.5f;
                scale.Min = center - minimumSpan * 0.5f;
                scale.Max = center + minimumSpan * 0.5f;
            }

            scale.Span = ChartSpan(scale.Min, scale.Max);
            return scale;
        }

        private static float ChartSpan(float min, float max)
        {
            var span = max - min;

            if (span <= 0.0001f)
            {
                return 1f;
            }

            return span;
        }

        /// <summary>선이 그려지는 왼쪽 끝. 0으로 두면 시작 점이 테두리에 반쯤 잘린다.</summary>
        private static float PlotLeft()
        {
            return CHART_PLOT_LEFT;
        }

        /// <summary>오른쪽 눈금 자리를 뺀 나머지가 선이 쓰는 폭이다.</summary>
        private static float PlotWidth(float width)
        {
            return Mathf.Max(10f, width - CHART_AXIS_WIDTH - CHART_PLOT_LEFT);
        }

        private static float ChartX(int index, int total, float width)
        {
            return ChartXAt(index, total, width);
        }

        /// <summary>소수 자리도 받는다. 마지막 점이 틱과 틱 사이를 미끄러지게 하려면 필요하다.</summary>
        private static float ChartXAt(float index, int total, float width)
        {
            if (total <= 0)
            {
                return CHART_PLOT_LEFT;
            }

            // 점이 틱 수보다 많아도 선이 눈금 자리로 넘어가지 않게 막는다.
            if (index > total)
            {
                index = total;
            }

            return CHART_PLOT_LEFT + index / total * PlotWidth(width);
        }

        private static float ChartY(float value, float min, float span, float height)
        {
            return height - (value - min) / span * height;
        }

        /// <summary>눈금 간격을 1·2·5 단위로 떨어뜨린다. 61.9, 63.0 같은 값은 눈금으로 쓰기 나쁘다.</summary>
        private static float NiceStep(float rough)
        {
            if (rough <= 0f)
            {
                return 1f;
            }

            var power = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(rough)));
            var fraction = rough / power;

            if (fraction <= 1f)
            {
                return power;
            }

            if (fraction <= 2f)
            {
                return 2f * power;
            }

            if (fraction <= 5f)
            {
                return 5f * power;
            }

            return 10f * power;
        }

        private static void PlaceAxisLabel(VisualElement chart, int index, float value, float x, float y)
        {
            var name = "axis-" + index;
            var label = chart.Q<Label>(name);

            if (label == null)
            {
                label = new Label();
                label.name = name;
                label.pickingMode = PickingMode.Ignore;
                label.AddToClassList("chart-axis-label");
                chart.Add(label);
            }

            label.RemoveFromClassList("hidden");
            label.text = value.ToString("N1");
            label.style.left = x;
            label.style.top = y;
            label.style.translate = new Translate(0f, Length.Percent(-50f));
        }

        private static void HideAxisLabels(VisualElement chart, int from)
        {
            for (var i = from; i < CHART_AXIS_MAX; i++)
            {
                HideMarker(chart, "axis-" + i);
            }
        }

        private void PlacePriceTag(VisualElement chart, AdventurerStock stock,
            float min, float span, float width, float height)
        {
            var tag = chart.Q<Label>("price-tag");

            if (tag == null)
            {
                tag = new Label();
                tag.name = "price-tag";
                tag.pickingMode = PickingMode.Ignore;
                tag.AddToClassList("chart-price-tag");
                chart.Add(tag);
            }

            var rising = stock.SessionChangeRatio >= -0.0005f;

            tag.RemoveFromClassList("hidden");
            tag.text = stock.LivePrice.ToString("N1");
            tag.EnableInClassList("chart-price-tag--up", rising);
            tag.EnableInClassList("chart-price-tag--down", rising == false);
            tag.style.left = PlotLeft() + PlotWidth(width) + 6f;
            tag.style.top = ChartY(stock.LivePrice, min, span, height);
            tag.style.translate = new Translate(0f, Length.Percent(-50f));
        }

        /// <summary>
        /// 라벨을 한 번 만들어 두고 자리만 옮긴다. 매 틱 새로 만들면 쓰레기가 쌓인다.
        /// above가 참이면 점 위에, 거짓이면 점 아래에 붙인다.
        /// </summary>
        private static void PlaceMarker(VisualElement chart, string name, string toneClass,
            string text, float x, float y, float plotWidth, bool above, MarkerAnchor anchor)
        {
            var label = chart.Q<Label>(name);

            if (label == null)
            {
                label = new Label();
                label.name = name;
                label.pickingMode = PickingMode.Ignore;
                label.AddToClassList("chart-marker-label");
                label.AddToClassList(toneClass);
                chart.Add(label);
            }

            label.RemoveFromClassList("hidden");
            label.text = text;

            var left = x;
            var shift = Length.Percent(-50f);

            if (anchor == MarkerAnchor.Anchor_Left)
            {
                shift = Length.Percent(0f);
            }
            else
            {
                // 끝에 붙은 점의 라벨이 눈금이나 테두리를 넘지 않게 안쪽으로 당긴다.
                var right = CHART_PLOT_LEFT + plotWidth - CHART_MARKER_MARGIN;
                left = Mathf.Clamp(x, CHART_PLOT_LEFT + CHART_MARKER_MARGIN, Mathf.Max(CHART_PLOT_LEFT, right));
            }

            label.style.left = left;
            label.style.top = y;
            label.style.translate = above == true
                ? new Translate(shift, Length.Percent(-135f))
                : new Translate(shift, Length.Percent(35f));
        }

        private static void HideMarker(VisualElement chart, string name)
        {
            var label = chart.Q<Label>(name);

            if (label == null)
            {
                return;
            }

            label.AddToClassList("hidden");
        }

        /// <summary>
        /// 가격 이력을 단일 선으로 그린다. 격자는 두지 않고 기준선 하나와 Y 라벨 셋만 둔다.
        /// 코드에 OHLC와 거래량이 없으므로 캔들이나 거래량 막대를 흉내내지 않는다.
        /// </summary>
        private void DrawChart(MeshGenerationContext context)
        {
            AdventurerStock stock;

            if (Sim == null || Sim.StockLookup.TryGetValue(SelectedId, out stock) == false)
            {
                return;
            }

            var history = stock.PriceHistory;

            if (history.Count < 1)
            {
                return;
            }

            var size = context.visualElement.contentRect;

            if (size.width <= 0f || size.height <= 0f)
            {
                return;
            }

            var scale = BuildScale(stock);
            var min = scale.Min;
            var span = scale.Span;
            var painter = context.painter2D;
            var total = Sim.Clock.TicksPerSession;
            var plotRight = CHART_PLOT_LEFT + PlotWidth(size.width);

            // 가격 눈금선. 오른쪽 라벨과 같은 값에 긋는다.
            var step = NiceStep(span / 4f);
            var first = Mathf.Ceil(min / step) * step;
            painter.strokeColor = new Color(0.55f, 0.50f, 0.43f, 0.22f);
            painter.lineWidth = 1f;

            for (var i = 0; i < CHART_AXIS_MAX; i++)
            {
                var value = first + step * i;

                if (value > scale.Max)
                {
                    break;
                }

                var gridY = ChartY(value, min, span, size.height);
                painter.BeginPath();
                painter.MoveTo(new Vector2(CHART_PLOT_LEFT, gridY));
                painter.LineTo(new Vector2(plotRight, gridY));
                painter.Stroke();
            }

            // 장 시작가 기준선. 눈금선보다 진하게 둬서 기준임을 드러낸다.
            var baseY = ChartY(scale.OpenValue, min, span, size.height);
            painter.strokeColor = new Color(0.45f, 0.40f, 0.33f, 0.55f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(CHART_PLOT_LEFT, baseY));
            painter.LineTo(new Vector2(plotRight, baseY));
            painter.Stroke();

            // 가격 선. 휴장이면 회색으로 둔다.
            var halted = Sim.Clock.Phase == MarketPhase.Phase_Halted;
            painter.strokeColor = halted == true
                ? new Color(0.38f, 0.42f, 0.48f, 1f)
                : HexColor(stock.SessionChangeRatio);
            painter.lineWidth = CHART_LINE_WIDTH;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();

            for (var i = 0; i < history.Count; i++)
            {
                var x = ChartX(i, total, size.width);
                var y = ChartY(history[i], min, span, size.height);

                if (i == 0)
                {
                    painter.MoveTo(new Vector2(x, y));
                    continue;
                }

                painter.LineTo(new Vector2(x, y));
            }

            // 마지막으로 지금 이 순간의 호가까지 잇는다.
            // 틱 종가만 찍으면 7.5초에 한 번 꺾이는 선이 되어 멈춰 보인다.
            var livePoint = LiveChartPoint(stock, history.Count, total, min, span, size);
            painter.LineTo(livePoint);

            painter.Stroke();

            // 시작·저·고 지점에 점을 찍는다. 라벨만으로는 선의 어디를 가리키는지 알 수 없다.
            // 아직 시작점이 저가(고가)이면 점도 겹치므로 시작점 하나만 찍는다.
            DrawDot(painter, livePoint, halted == true
                ? new Color32(0x61, 0x6B, 0x7A, 0xFF)
                : HexColor(stock.SessionChangeRatio));
            DrawDot(painter, new Vector2(CHART_PLOT_LEFT, baseY), new Color32(0x6E, 0x60, 0x52, 0xFF));

            if (IsOpenPoint(scale.LowIndex) == false)
            {
                DrawDot(painter, new Vector2(ChartXAt(scale.LowIndex, total, size.width),
                    ChartY(scale.LowValue, min, span, size.height)), new Color32(0x24, 0x5E, 0xC7, 0xFF));
            }

            if (IsOpenPoint(scale.HighIndex) == false)
            {
                DrawDot(painter, new Vector2(ChartXAt(scale.HighIndex, total, size.width),
                    ChartY(scale.HighValue, min, span, size.height)), new Color32(0xC5, 0x36, 0x43, 0xFF));
            }
        }

        /// <summary>지금 이 순간의 호가가 찍히는 자리. 마지막 틱에서 다음 틱 쪽으로 조금 나아간다.</summary>
        private Vector2 LiveChartPoint(AdventurerStock stock, int count, int total,
            float min, float span, Rect size)
        {
            var index = count - 1 + Sim.Clock.TickFraction;
            return new Vector2(ChartXAt(index, total, size.width),
                ChartY(stock.LivePrice, min, span, size.height));
        }

        /// <summary>점 하나를 찍는다. 선 위에 겹쳐도 보이도록 밝은 테두리를 먼저 깐다.</summary>
        private static void DrawDot(Painter2D painter, Vector2 center, Color color)
        {
            painter.fillColor = new Color(1f, 1f, 1f, 0.9f);
            painter.BeginPath();
            painter.Arc(center, CHART_DOT_RADIUS + 2f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();

            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, CHART_DOT_RADIUS, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();
        }

        private static Color HexColor(float changeRatio)
        {
            if (changeRatio < -0.0005f)
            {
                return new Color32(0x24, 0x5E, 0xC7, 0xFF);
            }

            return new Color32(0xC5, 0x36, 0x43, 0xFF);
        }

        /// <summary>
        /// 호가창을 그린다. 매도 5단계를 위에, 현재가를 가운데, 매수 5단계를 아래에 둔다.
        /// 틱이 바뀔 때만 다시 만든다. 매 프레임 다시 만들면 클릭이 먹지 않는다.
        /// </summary>
        private void RefreshBook()
        {
            var list = Root.Q<VisualElement>("book-list");

            if (list == null)
            {
                return;
            }

            AdventurerStock stock;

            if (Sim.StockLookup.TryGetValue(SelectedId, out stock) == false)
            {
                return;
            }

            if (BookTickShown == Sim.MicroSeq)
            {
                return;
            }

            BookTickShown = Sim.MicroSeq;
            OrderBook.FillAsks(stock, Sim.MicroSeq, Asks);
            OrderBook.FillBids(stock, Sim.MicroSeq, Bids);

            list.Clear();

            for (var i = Asks.Count - 1; i >= 0; i--)
            {
                list.Add(BuildBookRow(Asks[i], true));
            }

            var mid = new VisualElement();
            mid.AddToClassList("book-row");
            mid.AddToClassList("book-row--mid");
            var midLabel = new Label(OrderBook.SnapToTick(stock.LivePrice, stock.Price).ToString("N1"));
            midLabel.AddToClassList("book-mid-price");
            mid.Add(midLabel);
            list.Add(mid);

            for (var i = 0; i < Bids.Count; i++)
            {
                list.Add(BuildBookRow(Bids[i], false));
            }
        }

        private VisualElement BuildBookRow(BookLevel level, bool isAsk)
        {
            var row = new VisualElement();
            row.AddToClassList("book-row");
            row.AddToClassList(isAsk == true ? "book-row--ask" : "book-row--bid");

            var price = new Label(level.Price.ToString("N1"));
            price.AddToClassList("book-price");
            price.AddToClassList(isAsk == true ? "rise" : "fall");

            var qty = new Label(level.Quantity.ToString());
            qty.AddToClassList("book-qty");

            row.Add(price);
            row.Add(qty);

            var picked = level.Price;
            row.RegisterCallback<ClickEvent>(evt => PickBookPrice(picked));
            return row;
        }

        /// <summary>대기 중인 지정가 주문 목록. 취소 버튼을 함께 둔다.</summary>
        private void RefreshPending()
        {
            var list = Root.Q<VisualElement>("pending-list");
            var title = Root.Q<VisualElement>("pending-head");

            if (list == null)
            {
                return;
            }

            var orders = Sim.PendingOrders;
            var waiting = 0;

            for (var i = 0; i < orders.Count; i++)
            {
                if (orders[i].State == PendingState.Pending_Waiting)
                {
                    waiting++;
                }
            }

            if (title != null)
            {
                title.EnableInClassList("hidden", waiting == 0);
            }

            if (PendingCountShown == waiting)
            {
                return;
            }

            PendingCountShown = waiting;
            list.Clear();

            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];

                if (order.State != PendingState.Pending_Waiting)
                {
                    continue;
                }

                var row = new VisualElement();
                row.AddToClassList("pending-row");

                AdventurerStock stock;
                var name = Sim.StockLookup.TryGetValue(order.StockId, out stock) == true
                    ? stock.DisplayName
                    : order.StockId;
                var verb = order.Side == OrderSide.Side_Buy
                    ? StringConstants.TEXT_SIDE_BUY
                    : StringConstants.TEXT_SIDE_SELL;

                var text = new Label(string.Format(StringConstants.TEXT_PENDING_ROW, name, order.LimitPrice, order.Quantity, verb));
                text.AddToClassList("pending-text");
                text.AddToClassList(order.Side == OrderSide.Side_Buy ? "rise" : "fall");

                var cancel = new Button();
                cancel.text = StringConstants.TEXT_PENDING_CANCEL;
                cancel.AddToClassList("pending-cancel");
                var id = order.Id;
                cancel.clicked += () =>
                {
                    Sim.CancelOrder(id);
                    PendingCountShown = -1;
                    Message = StringConstants.TEXT_ORDER_CANCELLED;
                };

                row.Add(text);
                row.Add(cancel);
                list.Add(row);
            }
        }

        private void RefreshOrder()
        {
            AdventurerStock stock;

            if (Sim.StockLookup.TryGetValue(SelectedId, out stock) == false)
            {
                return;
            }

            var buying = Side == OrderSide.Side_Buy;

            var buyTab = Root.Q<Button>("side-buy");
            var sellTab = Root.Q<Button>("side-sell");

            if (buyTab != null)
            {
                buyTab.EnableInClassList("tab--buy-on", buying);
            }

            if (sellTab != null)
            {
                sellTab.EnableInClassList("tab--sell-on", buying == false);
            }

            SetFieldValue("qty-value", Quantity.ToString());

            var limiting = Type == OrderType.Type_Limit;
            Toggle("type-market", "type-tab--on", limiting == false);
            Toggle("type-limit", "type-tab--on", limiting);

            var limitRow = Root.Q<VisualElement>("limit-row");

            if (limitRow != null)
            {
                limitRow.EnableInClassList("hidden", limiting == false);
            }

            if (limiting == true && LimitTouched == false)
            {
                LimitPrice = OrderBook.SnapToTick(stock.Price, stock.Price);
            }

            SetFieldValue("limit-value", LimitPrice.ToString("N1"));

            var halted = Sim.Clock.Phase != MarketPhase.Phase_Open;
            // 예상 체결가도 실제 체결과 같은 방식으로 호가를 먹어 계산한다.
            float fill;
            float swept;
            OrderBook.Sweep(stock, Sim.MicroSeq, Side, Quantity, out fill, out swept);

            var shown = limiting == true ? LimitPrice : fill;

            SetText("price-label", halted == true ? StringConstants.TEXT_PRICE_LAST : StringConstants.TEXT_PRICE_NOW);
            SetText("order-price", stock.LivePrice.ToString("N1"));
            SetText("order-fill", halted == true ? "—" : shown.ToString("N1"));
            SetText("order-total", halted == true ? "—" : (shown * Quantity).ToString("N1"));
            SetText("order-cash", Sim.Wallet.AvailableCash.ToString("N0"));

            SetText("order-hint", limiting == true
                ? StringConstants.TEXT_HINT_LIMIT
                : StringConstants.TEXT_HINT_MARKET);

            var held = Sim.Wallet.QuantityOf(stock.Id);
            var available = Sim.Wallet.AvailableQuantityOf(stock.Id);

            // 없는 주식을 팔 수는 없다. 수량칸을 눌러도 아무 일이 없는 것보다 잠가 두는 쪽이 낫다.
            var nothingToSell = buying == false && available <= 0;
            var qtyBox = Root.Q<VisualElement>("qty-box");

            if (qtyBox != null)
            {
                qtyBox.SetEnabled(nothingToSell == false);
                qtyBox.EnableInClassList("qty-box--off", nothingToSell);
            }

            var ratioRow = Root.Q<VisualElement>("ratio-row");

            if (ratioRow != null)
            {
                ratioRow.SetEnabled(nothingToSell == false);
                ratioRow.EnableInClassList("qty-box--off", nothingToSell);
            }

            // 매수는 현금의 몇 할, 매도는 보유의 몇 할이다. 마지막 칸만 말이 달라진다.
            SetButtonText("ratio-100", buying == true ? StringConstants.TEXT_RATIO_MAX_BUY : StringConstants.TEXT_RATIO_MAX_SELL);

            SetText("hold-qty", held == available
                ? string.Format(StringConstants.TEXT_HOLD_QTY, held)
                : string.Format(StringConstants.TEXT_HOLD_QTY_LOCKED, held, available));
            var averageCost = Sim.Wallet.AverageCostOf(stock.Id);
            SetText("hold-avg", averageCost.ToString("N1"));

            var profit = (stock.LivePrice - averageCost) * held;
            var profitLabel = Root.Q<Label>("hold-profit");

            if (profitLabel != null)
            {
                profitLabel.text = held == 0 ? "—" : (profit >= 0f ? "+" : "") + profit.ToString("N1");
                profitLabel.EnableInClassList("rise", held > 0 && profit > 0f);
                profitLabel.EnableInClassList("fall", held > 0 && profit < 0f);
            }

            // 유동성은 날것의 수치라 뜻이 와닿지 않는다. 이 수량이 가격을 얼마나 미는지로 적는다.
            var impactRatio = stock.LivePrice <= 0f ? 0f : (fill - stock.LivePrice) / stock.LivePrice;
            SetText("impact-label", string.Format(StringConstants.TEXT_IMPACT_LABEL, Quantity));
            SetText("day-impact", halted == true
                ? "—"
                : (impactRatio >= 0f ? "+" : "") + (impactRatio * 100f).ToString("N1") + "%");

            var submit = Root.Q<Button>("submit-button");

            if (submit != null)
            {
                var blocked = Sim.Clock.AcceptsOrders == false || Sim.PlayerAtMarket == false
                    || nothingToSell == true;
                submit.SetEnabled(blocked == false);
                submit.EnableInClassList("btn--buy", blocked == false && buying);
                submit.EnableInClassList("btn--sell", blocked == false && buying == false);
                submit.EnableInClassList("btn--disabled", blocked);

                if (nothingToSell == true)
                {
                    submit.text = StringConstants.TEXT_SUBMIT_NO_SHARES;
                }
                else if (Sim.Clock.Phase == MarketPhase.Phase_Halted)
                {
                    submit.text = StringConstants.TEXT_SUBMIT_HALTED;
                }
                else if (Sim.Clock.IsPaused == true)
                {
                    submit.text = StringConstants.TEXT_SUBMIT_PAUSED;
                }
                else if (limiting == true)
                {
                    submit.text = string.Format(StringConstants.TEXT_SUBMIT_LIMIT, LimitPrice, Quantity,
                        buying == true ? StringConstants.TEXT_SIDE_BUY : StringConstants.TEXT_SIDE_SELL);
                }
                else
                {
                    submit.text = string.Format(StringConstants.TEXT_SUBMIT_MARKET, Quantity,
                        buying == true ? StringConstants.TEXT_SIDE_BUY : StringConstants.TEXT_SIDE_SELL);
                }
            }

            SetText("order-message", Message);
        }

        /// <summary>
        /// 하단 보유 띠. 어떤 종목을 얼마나 들고 있고 지금 얼마가 되었는가.
        /// 접혀 있어도 머리줄의 종목 수와 평가손익은 갱신한다. 그걸 보려고 접어 두는 것이다.
        /// </summary>
        private void RefreshHoldings()
        {
            var popup = Root.Q<VisualElement>("holdings-popup");

            if (popup == null)
            {
                return;
            }

            popup.EnableInClassList("holdings-popup--collapsed", HoldingsCollapsed);

            SetToggleIcon("holdings-toggle-icon", HoldingsCollapsed);

            var holdings = Sim.Wallet.AllHoldings;
            var owned = 0;
            var profit = 0f;

            foreach (var pair in holdings)
            {
                var holding = pair.Value;

                if (holding.Quantity <= 0)
                {
                    continue;
                }

                AdventurerStock stock;

                if (Sim.StockLookup.TryGetValue(holding.StockId, out stock) == false)
                {
                    continue;
                }

                owned++;
                profit += (stock.LivePrice - holding.AverageCost) * holding.Quantity;
            }

            SetText("holdings-count", string.Format(StringConstants.TEXT_HOLDINGS_COUNT, owned));

            var profitLabel = Root.Q<Label>("holdings-profit");

            if (profitLabel != null)
            {
                profitLabel.text = owned == 0 ? "—" : (profit >= 0f ? "+" : "") + profit.ToString("N1");
                profitLabel.EnableInClassList("rise", owned > 0 && profit > 0f);
                profitLabel.EnableInClassList("fall", owned > 0 && profit < 0f);
            }

            // 접혀 있으면 목록은 만들지 않는다. 보이지도 않는 행을 매번 짓는 것은 낭비다.
            if (HoldingsCollapsed == true)
            {
                return;
            }

            // 값이 실시간으로 움직이므로 미세 호가가 바뀔 때마다 다시 짓는다. 행 수가 적어 부담이 없다.
            if (HoldingsSeqShown == Sim.MicroSeq)
            {
                return;
            }

            HoldingsSeqShown = Sim.MicroSeq;

            var list = Root.Q<ScrollView>("holdings-list");

            if (list == null)
            {
                return;
            }

            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            list.Clear();

            for (var i = 0; i < Sim.Stocks.Count; i++)
            {
                var stock = Sim.Stocks[i];
                var quantity = Sim.Wallet.QuantityOf(stock.Id);

                if (quantity <= 0)
                {
                    continue;
                }

                list.Add(BuildHoldingRow(stock, quantity));
            }

            var empty = Root.Q<Label>("holdings-empty");

            if (empty != null)
            {
                empty.EnableInClassList("hidden", owned > 0);
            }
        }

        private VisualElement BuildHoldingRow(AdventurerStock stock, int quantity)
        {
            var averageCost = Sim.Wallet.AverageCostOf(stock.Id);
            var value = stock.LivePrice * quantity;
            var profit = (stock.LivePrice - averageCost) * quantity;
            var ratio = averageCost <= 0f ? 0f : (stock.LivePrice - averageCost) / averageCost;

            var row = new VisualElement();
            row.AddToClassList("holding-row");

            var top = new VisualElement();
            top.AddToClassList("holding-line");

            var name = new Label(stock.DisplayName);
            name.AddToClassList("holding-name");

            var qty = new Label(string.Format(StringConstants.TEXT_HOLD_QTY, quantity));
            qty.AddToClassList("holding-qty");

            top.Add(name);
            top.Add(qty);

            var bottom = new VisualElement();
            bottom.AddToClassList("holding-line");

            var sub = new Label(string.Format(StringConstants.TEXT_HOLDING_SUB, averageCost, value));
            sub.AddToClassList("holding-sub");

            var profitText = (profit >= 0f ? "+" : "") + profit.ToString("N1")
                + " (" + (ratio >= 0f ? "+" : "") + (ratio * 100f).ToString("N1") + "%)";
            var profitLabel = new Label(profitText);
            profitLabel.AddToClassList("holding-profit");
            profitLabel.AddToClassList(profit >= 0f ? "rise" : "fall");

            bottom.Add(sub);
            bottom.Add(profitLabel);

            row.Add(top);
            row.Add(bottom);

            // 누르면 그 종목으로 넘어간다. 보유 목록을 여는 이유는 대개 팔기 위해서다.
            var id = stock.Id;
            row.RegisterCallback<ClickEvent>(evt => SelectStock(id));

            return row;
        }

        /// <summary>
        /// 정보 타임 연출을 연다. 장을 세우고 거래 화면을 덮는다.
        ///
        /// 정보 타임은 알림이 아니라 하나의 사건이다. 기획서 §11에 따라
        /// 주식 패널을 내리고 지하세계 장면에서 소식을 한 건씩 전한다.
        /// </summary>
        private void OpenScene()
        {
            if (Sim.BriefingEntries.Count == 0)
            {
                Sim.AcknowledgeBriefing();
                return;
            }

            if (Dialogue == null)
            {
                // 대화상자가 아직 붙지 않았으면 소식만 흘려보낸다. 장이 멈춘 채로 둘 수는 없다.
                Sim.AcknowledgeBriefing();
                return;
            }

            // 읽는 동안 장이 흐르면 읽을 틈이 없다.
            Sim.Clock.SetPaused(true);
            SceneIndex = 0;

            Dialogue.SetBackground(SceneBackground());
            Dialogue.Play(BuildBriefingLines(), CloseBriefingScene);
        }

        // ── 지하 방 (2026-10-17) ─────────────────
        // 거래 화면은 방 안의 주식판에서 여는 판이 된다. 방을 걷는 일은 UnderworldRoom이 맡는다.

        /// <summary>지금 돌고 있는 시장. 방이 투자꾼 반응·시계를 읽는다.</summary>
        public MarketSimulation Simulation
        {
            get
            {
                return Sim;
            }
        }

        /// <summary>거래판이 열려 있는가. 방 모드가 아니면 늘 참이다.</summary>
        public bool IsPanelOpen
        {
            get
            {
                return RoomMode == false || PanelOpen == true;
            }
        }

        /// <summary>방을 덮고 있는가 — 거래판·정보 타임 대화·안내가 떠 있다. 이때 주인공은 걷지 않는다.</summary>
        public bool IsCoveringRoom
        {
            get
            {
                return IsPanelOpen == true || SceneIndex >= 0 || (Tutorial != null && Tutorial.IsOpen == true);
            }
        }

        /// <summary>정보 타임이 와서 정보상이 기다리는 중인가. 시간은 멈춰 있다.</summary>
        public bool IsBriefingWaiting { get; private set; }

        public void OpenPanel()
        {
            PanelOpen = true;
            ApplyPanel();
        }

        /// <summary>판을 닫고 방으로 돌아간다. 안내가 떠 있는 동안에는 닫지 않는다.</summary>
        public void ClosePanel()
        {
            if (RoomMode == false || (Tutorial != null && Tutorial.IsOpen == true))
            {
                return;
            }

            PanelOpen = false;
            ApplyPanel();
        }

        /// <summary>정보상에게 말을 걸었다. 기다리던 소식이 열린다.</summary>
        public void OpenBriefing()
        {
            if (IsBriefingWaiting == false)
            {
                return;
            }

            IsBriefingWaiting = false;
            OpenScene();
        }

        /// <summary>
        /// 정보 타임이 왔다. 시간을 멈추고 판을 닫는다 — 정보상에게 걸어가는 동안 가격이 먼저 움직이지 않게
        /// (2026-10-17 사용자 결정 ①). 플레이어가 정보상에게 가면 <see cref="OpenBriefing"/>이 연다.
        /// </summary>
        private void WaitForInformant()
        {
            if (IsBriefingWaiting == true)
            {
                return;
            }

            IsBriefingWaiting = true;
            Sim.Clock.SetPaused(true);
            PanelOpen = false;
        }

        // 판이 닫히면 거래 화면 몸통만 감춘다. 대화상자·쪽지는 같은 판 위에 있으므로 그대로 둔다.
        private void ApplyPanel()
        {
            if (Root == null)
            {
                return;
            }

            var body = Root.Q<VisualElement>("market-root");

            if (body == null)
            {
                return;
            }

            var open = IsPanelOpen;
            var shown = body.style.display != DisplayStyle.None;

            if (PanelApplied == true && shown == open)
            {
                return;
            }

            PanelApplied = true;
            body.style.display = open == true ? DisplayStyle.Flex : DisplayStyle.None;

            if (RoomMode == true && Root.Q<Button>("room-button") == null)
            {
                AddRoomButton();
            }
        }

        // 판 위에 「방으로」 단추를 끼운다. Tab으로도 닫힌다.
        private void AddRoomButton()
        {
            var leave = Root.Q<Button>("village-button");

            if (leave == null || leave.parent == null)
            {
                return;
            }

            var button = new Button(ClosePanel);
            button.name = "room-button";
            button.text = StringConstants.TEXT_ROOM_BACK;
            button.AddToClassList("btn");
            leave.parent.Insert(leave.parent.IndexOf(leave), button);
        }

        /// <summary>대화가 끝났다. 장이 다시 흐른다.</summary>
        private void CloseBriefingScene()
        {
            SceneIndex = -1;
            Sim.AcknowledgeBriefing();
            Sim.Clock.SetPaused(false);
            InfoCountShown = -1;
        }

        /// <summary>
        /// 이번 정보 타임에 들어온 소식을 대화 줄로 옮긴다.
        /// 소식의 주인이 말하는 것이 아니라, 그 사람 이야기가 화면에 서는 것이다.
        /// </summary>
        private List<DialogueLine> BuildBriefingLines()
        {
            var lines = new List<DialogueLine>();
            var session = Sim.Clock.Session == MarketSession.Session_Evening
                ? StringConstants.TEXT_SESSION_EVENING
                : StringConstants.TEXT_SESSION_MORNING;

            for (var i = 0; i < Sim.BriefingEntries.Count; i++)
            {
                var entry = Sim.BriefingEntries[i];

                AdventurerStock stock;
                var name = Sim.StockLookup.TryGetValue(entry.StockId, out stock) == true
                    ? stock.DisplayName
                    : entry.StockId;

                // 말하는 이는 거래소를 운영하는 정보상이다. 어느 단의 소식인지는 꼬리표에 적는다(2026-10-17 사용자 결정).
                // 정보상 초상은 아직 없다(Codex에 그릴 예정) — 그때까지 인물 자리를 비운다.
                var line = new DialogueLine(StringConstants.TEXT_INFORMANT,
                    $"{name} · {SourceText(entry.Source)} · {ClockText.SeasonDay(Sim.Clock.Day)} {session}"
                    + $" {ClockText.FromSeconds(Sim.Clock.ClockTotalSeconds, false)}",
                    entry.Headline);

                var held = Sim.Wallet.QuantityOf(entry.StockId);

                if (held > 0)
                {
                    line.Badge = string.Format(StringConstants.TEXT_HELD_NOTE, held);
                }

                line.Portrait = Data == null ? null : Data.InformantFace(EmotionOf(entry));
                line.Emotion = EmotionOf(entry);
                line.IsSecret = entry.IsPublic == false;
                line.Note = entry.IsPublic == true
                    ? StringConstants.TEXT_SCENE_PUBLIC
                    : StringConstants.TEXT_SCENE_PRIVATE;

                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 소식의 무게로 얼굴을 고른다. 결과가 확정된 소식이 가장 무겁다.
        /// 글자를 읽기 전에 분위기가 먼저 전해져야 한다.
        /// </summary>
        private static PortraitEmotion EmotionOf(InfoEntry entry)
        {
            if (entry.Stage == EventStage.Stage_Outcome)
            {
                return PortraitEmotion.Emotion_Shocked;
            }

            if (entry.IsPublic == false)
            {
                return PortraitEmotion.Emotion_Anxious;
            }

            return PortraitEmotion.Emotion_Neutral;
        }

        /// <summary>분위기가 가라앉으면 등불이 꺼져 가는 방을 쓴다. 같은 지하실이고 빛만 다르다.</summary>
        private string SceneBackground()
        {
            var grim = Sim.Sentiment == MarketSentiment.Sentiment_Fear
                || Sim.Sentiment == MarketSentiment.Sentiment_Drained;

            return grim == true ? "dialogue-root--underworld-dim" : "dialogue-root--underworld";
        }

        private void RefreshInfo()
        {
            var list = Root.Q<ScrollView>("info-list");

            if (list == null)
            {
                return;
            }

            // 가로 스크롤은 쓰지 않는다. 긴 문장은 줄바꿈으로 처리한다.
            list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            var log = Sim.InfoLog;

            RefreshInfoHead(log);

            // 목록은 매 프레임 다시 만들지 않는다. 기록이 늘었거나 보유가 바뀌었을 때만 다시 그린다.
            // 보유가 바뀌면 「보유 N주」 표시와 「내 종목」 탭의 내용이 달라진다.
            var held = HeldSignature();

            if (InfoCountShown == log.Count && InfoHeldShown == held)
            {
                return;
            }

            InfoCountShown = log.Count;
            InfoHeldShown = held;
            list.Clear();

            if (Filter == InfoFilter.Filter_Bands)
            {
                var bands = Sim.CodexSubjects();

                for (var b = 0; b < bands.Count; b++)
                {
                    list.Add(CodexCards.Build(Sim, bands[b], Sim.NameOf(bands[b])));
                }

                SelectFilterButtons();
                Root.Q<Label>("info-empty").EnableInClassList("hidden", true);
                return;
            }

            var shown = 0;

            for (var i = log.Count - 1; i >= 0 && shown < INFO_MAX_ROWS; i--)
            {
                var entry = log[i];

                if (Filter == InfoFilter.Filter_Private && entry.IsPublic == true)
                {
                    continue;
                }

                if (Filter == InfoFilter.Filter_Mine && Sim.Wallet.QuantityOf(entry.StockId) <= 0)
                {
                    continue;
                }

                list.Add(BuildInfoCard(entry));
                shown++;
            }

            var empty = Root.Q<Label>("info-empty");

            if (empty != null)
            {
                empty.EnableInClassList("hidden", shown > 0);
                empty.text = Filter == InfoFilter.Filter_Mine
                    ? StringConstants.TEXT_INFO_EMPTY_MINE
                    : StringConstants.TEXT_INFO_EMPTY;
            }

            SelectFilterButtons();
        }

        private void SelectFilterButtons()
        {
            Select("filter-mine", Filter == InfoFilter.Filter_Mine);
            Select("filter-all", Filter == InfoFilter.Filter_All);
            Select("filter-private", Filter == InfoFilter.Filter_Private);
            Select("filter-bands", Filter == InfoFilter.Filter_Bands);
        }

        // 보유 상태를 숫자 하나로 줄인다. 바뀌었는지만 알면 된다. 매 프레임 불리므로 할당하지 않는다.
        private int HeldSignature()
        {
            var signature = 0;

            for (var i = 0; i < Sim.Stocks.Count; i++)
            {
                signature = signature * 31 + Sim.Wallet.QuantityOf(Sim.Stocks[i].Id);
            }

            return signature;
        }

        /// <summary>
        /// 팝업 머리 — 접힘 표시, 건수, 그리고 접었을 때 보일 최근 소식 한 줄.
        /// 접어 두면 새 소식이 들어온 줄 모르므로 머리에 마지막 한 줄만 흘려 둔다.
        /// </summary>
        private void RefreshInfoHead(IReadOnlyList<InfoEntry> log)
        {
            var popup = Root.Q<VisualElement>("info-popup");

            if (popup != null)
            {
                popup.EnableInClassList("info-popup--collapsed", InfoCollapsed);
            }

            SetToggleIcon("info-toggle-icon", InfoCollapsed);

            SetText("info-count", string.Format(StringConstants.TEXT_INFO_COUNT, log.Count));

            // 접혀 있으면 필터는 거를 목록이 없다. 버튼을 치워야 최근 소식 한 줄이 들어간다.
            ShowFilterButton("filter-mine");
            ShowFilterButton("filter-all");
            ShowFilterButton("filter-private");
            ShowFilterButton("filter-bands");

            var latest = Root.Q<Label>("info-latest");

            if (latest == null)
            {
                return;
            }

            if (InfoCollapsed == false || log.Count == 0)
            {
                latest.text = string.Empty;
                return;
            }

            latest.text = log[log.Count - 1].Headline;
        }

        private void ShowFilterButton(string elementName)
        {
            var button = Root.Q<Button>(elementName);

            if (button == null)
            {
                return;
            }

            button.EnableInClassList("hidden", InfoCollapsed);
        }

        /// <summary>
        /// 소식 한 장. 두 줄이다 — 첫 줄은 누구의·언제·어디서, 둘째 줄은 내용.
        /// 예전에는 세 줄에 "시장에 공개됨"이 한 줄을 통째로 쓰고 종목 이름은 작은 회색 글씨에 묻혀
        /// 한 화면에 두 장밖에 안 보였다(2026-10-17 사용자 보고: 가독성이 떨어진다).
        /// 가진 종목의 소식은 왼쪽 띠와 「보유 N주」로 바로 알아보게 한다.
        /// </summary>
        private VisualElement BuildInfoCard(InfoEntry entry)
        {
            var heldQuantity = Sim.Wallet.QuantityOf(entry.StockId);

            var card = new VisualElement();
            card.AddToClassList("info-card");
            card.EnableInClassList("info-card--private", entry.IsPublic == false);
            card.EnableInClassList("info-card--held", heldQuantity > 0);

            var line = new VisualElement();
            line.AddToClassList("info-line");

            AdventurerStock stock;
            var name = Sim.StockLookup.TryGetValue(entry.StockId, out stock) == true
                ? stock.DisplayName
                : entry.StockId;

            var stockLabel = new Label(name);
            stockLabel.AddToClassList("info-stock");
            line.Add(stockLabel);

            if (heldQuantity > 0)
            {
                var held = new Label(string.Format(StringConstants.TEXT_INFO_HELD, heldQuantity));
                held.AddToClassList("info-held");
                line.Add(held);
            }

            var when = new Label(string.Format(StringConstants.TEXT_INFO_WHEN, SourceText(entry.Source), entry.Day,
                SessionName(entry.Session)));
            when.AddToClassList("info-when");
            line.Add(when);

            // 공개 여부는 점으로. 나만 아는 소식만 글자를 붙인다 — 그쪽이 드물고 값지다.
            var dot = new VisualElement();
            dot.AddToClassList("info-dot");
            dot.AddToClassList(entry.IsPublic == true ? "info-dot--public" : "info-dot--private");
            line.Add(dot);

            if (entry.IsPublic == false)
            {
                var secret = new Label(StringConstants.TEXT_INFO_PRIVATE_SHORT);
                secret.AddToClassList("info-secret");
                line.Add(secret);
            }

            var headline = new Label(entry.Headline);
            headline.AddToClassList("info-headline");

            card.Add(line);
            card.Add(headline);
            return card;
        }

        // ── 보조 ──────────────────────────────────

        private void SetButtonText(string elementName, string value)
        {
            var button = Root.Q<Button>(elementName);

            if (button == null)
            {
                return;
            }

            button.text = value;
        }

        /// <summary>
        /// 결과 쪽지를 띄운다. 스스로 사라지므로 닫는 조작이 필요 없다.
        /// 체결은 한순간이라 주문 패널 아래 작은 글자로는 놓치기 쉽다.
        /// </summary>
        private void ShowToast(string title, string note, OrderSide side, bool accepted)
        {
            // 화면이 붙기 전(OnEnable에서 시장을 이어 받을 때)에도 불릴 수 있다. 그때는 아래 문구로만 남긴다.
            if (Root == null)
            {
                Message = note;
                return;
            }

            var area = Root.Q<VisualElement>("toast-area");

            if (area == null)
            {
                return;
            }

            // 오래된 것부터 걷어낸다. 쌓이기만 하면 주문 탭까지 덮는다.
            while (area.childCount >= TOAST_MAX)
            {
                area.RemoveAt(area.childCount - 1);
            }

            var card = new VisualElement();
            card.AddToClassList("toast");
            card.pickingMode = PickingMode.Ignore;

            if (accepted == false)
            {
                card.AddToClassList("toast--deny");
            }
            else
            {
                card.AddToClassList(side == OrderSide.Side_Buy ? "toast--buy" : "toast--sell");
            }

            var head = new Label(title);
            head.AddToClassList("toast-text");
            head.AddToClassList(accepted == false
                ? "weak"
                : (side == OrderSide.Side_Buy ? "rise" : "fall"));

            var body = new Label(note);
            body.AddToClassList("toast-note");

            card.Add(head);
            card.Add(body);

            // 새 쪽지가 위에 온다. 아래로 밀려날수록 오래된 것이다.
            area.Insert(0, card);
            PlaySlideIn(card);

            // 실시간 기준으로 센다. 배속이나 일시정지에 끌려가면 안 된다.
            card.schedule.Execute(() => PlayFadeOut(card))
                .StartingIn((long)(TOAST_SECONDS * 1000f));
        }

        /// <summary>
        /// 오른쪽에서 밀려 들어온다. 갑자기 나타나면 눈이 어디서 생겼는지 따라가지 못한다.
        /// 실시간 기준으로 돌린다. 장이 멈춰 있어도 쪽지는 움직여야 한다.
        /// </summary>
        private static void PlaySlideIn(VisualElement card)
        {
            card.style.opacity = 0f;
            card.style.translate = new Translate(TOAST_SLIDE_FROM, 0f);

            var progress = 0f;
            DOTween.To(() => progress, value =>
            {
                progress = value;

                if (card.panel == null)
                {
                    return;
                }

                card.style.opacity = value;
                card.style.translate = new Translate(Mathf.Lerp(TOAST_SLIDE_FROM, 0f, value), 0f);
            }, 1f, TOAST_SLIDE_SECONDS).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        /// <summary>흐려지며 사라진다. 다 흐려진 뒤에 떼어 낸다.</summary>
        private static void PlayFadeOut(VisualElement card)
        {
            if (card.parent == null)
            {
                return;
            }

            var progress = 1f;
            DOTween.To(() => progress, value =>
            {
                progress = value;

                if (card.panel == null)
                {
                    return;
                }

                card.style.opacity = value;
            }, 0f, TOAST_SLIDE_SECONDS).SetEase(Ease.InQuad).SetUpdate(true)
                .OnComplete(() =>
                {
                    if (card.parent != null)
                    {
                        card.RemoveFromHierarchy();
                    }
                });
        }

        private string StockName(string stockId)
        {
            AdventurerStock stock;

            if (Sim.StockLookup.TryGetValue(stockId, out stock) == true)
            {
                return stock.DisplayName;
            }

            return stockId;
        }

        /// <summary>접기 손잡이의 그림을 상태에 맞춘다.</summary>
        private void SetToggleIcon(string elementName, bool collapsed)
        {
            var icon = Root.Q<VisualElement>(elementName);

            if (icon == null)
            {
                return;
            }

            icon.EnableInClassList("icon--collapse", collapsed);
            icon.EnableInClassList("icon--expand", collapsed == false);
        }

        private void SetText(string elementName, string value)
        {
            var label = Root.Q<Label>(elementName);

            if (label == null)
            {
                return;
            }

            label.text = value;
        }

        /// <summary>버튼에 클래스를 켜고 끈다.</summary>
        private void Toggle(string elementName, string className, bool on)
        {
            var button = Root.Q<Button>(elementName);

            if (button == null)
            {
                return;
            }

            button.EnableInClassList(className, on);
        }

        private void Select(string elementName, bool selected)
        {
            var button = Root.Q<Button>(elementName);

            if (button == null)
            {
                return;
            }

            button.EnableInClassList("btn--selected", selected);
        }

        private static string PhaseText(MarketPhase phase)
        {
            switch (phase)
            {
                case MarketPhase.Phase_Open:
                {
                    return StringConstants.TEXT_MARKET_PHASE_OPEN;
                }
                case MarketPhase.Phase_Halted:
                {
                    return StringConstants.TEXT_MARKET_PHASE_HALTED_SHORT;
                }
                default:
                {
                    return StringConstants.TEXT_MARKET_PHASE_CLOSED;
                }
            }
        }

        private static string SentimentText(MarketSentiment sentiment)
        {
            switch (sentiment)
            {
                case MarketSentiment.Sentiment_Fever:
                {
                    return StringConstants.TEXT_SENTIMENT_FEVER;
                }
                case MarketSentiment.Sentiment_Fear:
                {
                    return StringConstants.TEXT_SENTIMENT_FEAR;
                }
                case MarketSentiment.Sentiment_Drained:
                {
                    return StringConstants.TEXT_SENTIMENT_DRAINED;
                }
                default:
                {
                    return StringConstants.TEXT_SENTIMENT_CALM;
                }
            }
        }

        private static string SourceText(InfoSource source)
        {
            switch (source)
            {
                case InfoSource.Source_TavernTalk:
                {
                    return StringConstants.TEXT_SOURCE_TAVERN_TALK;
                }
                case InfoSource.Source_GuildRecord:
                {
                    return StringConstants.TEXT_SOURCE_GUILD_RECORD;
                }
                case InfoSource.Source_PrivateLetter:
                {
                    return StringConstants.TEXT_SOURCE_PRIVATE_LETTER;
                }
                case InfoSource.Source_BrokerLeak:
                {
                    return StringConstants.TEXT_SOURCE_BROKER_LEAK;
                }
                default:
                {
                    return StringConstants.TEXT_SOURCE_MARKET_BOARD;
                }
            }
        }
    }
}
