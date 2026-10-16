using System.Collections.Generic;
using System.Text;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Market;
using Underwrite.Utility;
using UnityEngine;
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
        private const int CHART_Y_LABELS = 3;
        private const int INFO_MAX_ROWS = 20;

        private enum InfoFilter
        {
            Filter_All,
            Filter_Private,
            Filter_Public
        }

        [Header("장세")]
        [SerializeField] private int Seed = 20261016;
        [SerializeField] private bool IsInspectionDay;
        [SerializeField] private MarketSentiment StartingSentiment = MarketSentiment.Sentiment_Calm;

        [Header("진행")]
        [SerializeField] private bool AutoPauseOnOutcome = true;

        [Header("테마")]
        [Tooltip("켜면 어두운 지하 테마를 덮어씌운다. 색 토큰만 바뀌고 레이아웃은 같다.")]
        [SerializeField] private bool UseUndergroundTheme;
        [SerializeField] private StyleSheet UndergroundTheme;

        private MarketSimulation Sim;
        private VisualElement Root;
        private readonly StringBuilder Builder = new StringBuilder();
        private readonly List<VisualElement> StockRows = new List<VisualElement>();

        private string SelectedId = "";
        private OrderSide Side = OrderSide.Side_Buy;
        private int Quantity = 10;
        private InfoFilter Filter = InfoFilter.Filter_All;
        private int InfoCountShown = -1;
        private int ChartTickShown = -1;
        private int BookTickShown = -1;
        private int PendingCountShown = -1;
        private string Message = "";

        private bool InfoCollapsed;
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

            Sim = new MarketSimulation(Seed, Constants.STARTING_CASH);

            var stocks = PrototypeScenario.CreateStocks();

            for (var i = 0; i < stocks.Count; i++)
            {
                Sim.AddStock(stocks[i]);
            }

            Sim.AddEvents(PrototypeScenario.CreateEvents());
            Sim.SetSentiment(StartingSentiment);
            Sim.OpenSession(IsInspectionDay);
            Sim.PlayerAtMarket = true;

            SelectedId = stocks[0].Id;
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
            var ticks = Sim.Clock.Advance(delta);

            if (ticks > 0)
            {
                Sim.Step(ticks);

                if (AutoPauseOnOutcome == true && Sim.HasPendingAlert == true)
                {
                    Sim.Clock.SetPaused(true);
                    Message = "결과가 확정되어 자동 정지했습니다.";
                }
            }

            Refresh();
        }

        /// <summary>
        /// 어두운 테마를 덮어씌운다. 색 토큰만 바꾸는 시트라 나중에 붙이기만 하면 된다.
        /// 재생 중 F2로 끄고 켜며 비교할 수 있다.
        /// </summary>
        private void ApplyTheme()
        {
            if (UndergroundTheme == null)
            {
                return;
            }

            var has = Root.styleSheets.Contains(UndergroundTheme);

            if (UseUndergroundTheme == true && has == false)
            {
                Root.styleSheets.Add(UndergroundTheme);
                return;
            }

            if (UseUndergroundTheme == false && has == true)
            {
                Root.styleSheets.Remove(UndergroundTheme);
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

        /// <summary>재생 중 F2로 테마를 전환한다. 두 안을 바로 비교하기 위한 것이다.</summary>
        public void ToggleTheme()
        {
            UseUndergroundTheme = UseUndergroundTheme == false;
            ApplyTheme();
        }

        // ── 구성 ──────────────────────────────────

        private void BuildStockRows()
        {
            var list = Root.Q<VisualElement>("stock-list");

            if (list == null)
            {
                return;
            }

            list.Clear();
            StockRows.Clear();

            var stocks = Sim.Stocks;

            for (var i = 0; i < stocks.Count; i++)
            {
                var stock = stocks[i];
                var row = new VisualElement();
                row.AddToClassList("stock-row");

                var top = new VisualElement();
                top.AddToClassList("stock-line");

                var name = new Label(stock.DisplayName);
                name.AddToClassList("stock-name");
                var price = new Label("0.0");
                price.AddToClassList("stock-price");
                price.name = "price";
                var change = new Label("0.0%");
                change.AddToClassList("stock-change");
                change.name = "change";

                top.Add(name);
                top.Add(price);
                top.Add(change);

                var sub = new Label("보유 0주 · 평균 매입가 0.0");
                sub.AddToClassList("t-note");
                sub.name = "sub";

                row.Add(top);
                row.Add(sub);

                var id = stock.Id;
                row.RegisterCallback<ClickEvent>(evt => SelectStock(id));

                list.Add(row);
                StockRows.Add(row);
            }
        }

        private void BindButtons()
        {
            Click("pause-button", () => Sim.Clock.SetPaused(Sim.Clock.IsPaused == false));
            Click("speed-1", () => Sim.Clock.SetSpeedScale(1f));
            Click("speed-4", () => Sim.Clock.SetSpeedScale(4f));
            Click("skip-button", SkipOrOpen);
            Click("place-button", () => Sim.PlayerAtMarket = Sim.PlayerAtMarket == false);

            Click("side-buy", () => Side = OrderSide.Side_Buy);
            Click("side-sell", () => Side = OrderSide.Side_Sell);

            Click("type-market", () => Type = OrderType.Type_Market);
            Click("type-limit", () => SwitchToLimit());
            Click("limit-minus", () => NudgeLimit(-1));
            Click("limit-plus", () => NudgeLimit(1));

            Click("qty-minus", () => Quantity = Mathf.Max(QUANTITY_MIN, Quantity - QUANTITY_STEP));
            Click("qty-plus", () => Quantity = Mathf.Min(QUANTITY_MAX, Quantity + QUANTITY_STEP));

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

            Click("filter-all", () => SetFilter(InfoFilter.Filter_All));
            Click("filter-private", () => SetFilter(InfoFilter.Filter_Private));
            Click("filter-public", () => SetFilter(InfoFilter.Filter_Public));

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
                Sim.OpenNextSession(IsInspectionDay);
                ResetViewState();
                Message = $"{SessionName(Sim.Clock.Session)}이 열렸습니다.";
                return;
            }

            var before = Sim.InfoLog.Count;
            var filled = Sim.SkipToClose();
            var news = Sim.InfoLog.Count - before;
            ResetViewState();

            Message = filled > 0
                ? $"장을 넘겼습니다. 소식 {news}건, 지정가 체결 {filled}건."
                : $"장을 넘겼습니다. 소식 {news}건.";
        }

        /// <summary>화면이 다시 그려지도록 캐시를 비운다.</summary>
        private void ResetViewState()
        {
            InfoCountShown = -1;
            ChartTickShown = -1;
            BookTickShown = -1;
            PendingCountShown = -1;
        }

        private void RefreshSkipButton()
        {
            var skip = Root.Q<Button>("skip-button");

            if (skip == null)
            {
                return;
            }

            if (Sim.Clock.Phase == MarketPhase.Phase_Closed)
            {
                var next = Sim.Clock.Session == MarketSession.Session_Morning ? "오후장" : "다음 날 오전장";
                skip.text = next + " 열기";
                return;
            }

            skip.text = SessionName(Sim.Clock.Session) + " 넘기기";
        }

        private static string SessionName(MarketSession session)
        {
            return session == MarketSession.Session_Evening ? "오후장" : "오전장";
        }

        private void Submit()
        {
            var verb = Side == OrderSide.Side_Buy ? "매수" : "매도";

            if (Type == OrderType.Type_Limit)
            {
                var queued = Sim.SubmitLimitOrder(SelectedId, Side, Quantity, LimitPrice);

                if (queued.Result == OrderResult.Result_Queued)
                {
                    Message = $"{queued.FillPrice:N1}에 {queued.Quantity}주 {verb} 주문을 걸었습니다.";
                    PendingCountShown = -1;
                    return;
                }

                Message = queued.Message;
                return;
            }

            var receipt = Sim.SubmitOrder(SelectedId, Side, Quantity);

            if (receipt.Result == OrderResult.Result_Filled)
            {
                Message = $"{verb} {receipt.Quantity}주 @ {receipt.FillPrice:N1} = {receipt.TotalAmount:N0}";
                return;
            }

            Message = receipt.Message;
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
        }

        private void RefreshHeader()
        {
            var phase = Root.Q<Label>("phase-chip");

            if (phase != null)
            {
                phase.text = PhaseText(Sim.Clock.Phase);
                phase.EnableInClassList("status-chip--halt", Sim.Clock.Phase == MarketPhase.Phase_Halted);
            }

            var label = Sim.Clock.Session == MarketSession.Session_Evening ? "오후장" : "오전장";
            SetText("clock-text",
                $"{Sim.Clock.Day}일차 {label} {Sim.Clock.ClockHour:00}:{Sim.Clock.ClockMinute:00}"
                + $" · {Sim.Clock.Tick} / {Sim.Clock.TicksPerSession}틱");
            SetText("sentiment-text", $"분위기 {SentimentText(Sim.Sentiment)}");

            var pause = Root.Q<Button>("pause-button");

            if (pause != null)
            {
                pause.text = Sim.Clock.IsPaused == true ? "재개" : "일시정지";
            }

            var place = Root.Q<Button>("place-button");

            if (place != null)
            {
                place.text = Sim.PlayerAtMarket == true ? "지하시장에 있음" : "영지 이동 중";
            }

            Select("speed-1", Mathf.Approximately(Sim.Clock.SpeedScale, 1f));
            Select("speed-4", Mathf.Approximately(Sim.Clock.SpeedScale, 4f));
            RefreshSkipButton();

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
            var stocks = Sim.Stocks;

            for (var i = 0; i < stocks.Count && i < StockRows.Count; i++)
            {
                var stock = stocks[i];
                var row = StockRows[i];

                row.EnableInClassList("stock-row--selected", stock.Id == SelectedId);

                var price = row.Q<Label>("price");

                if (price != null)
                {
                    price.text = stock.Price.ToString("N1");
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
                    Builder.Append("보유 ").Append(Sim.Wallet.QuantityOf(stock.Id)).Append("주 · 평균 매입가 ");
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
            SetText("chart-price", stock.Price.ToString("N1"));

            var halted = Sim.Clock.Phase == MarketPhase.Phase_Halted;
            SetText("chart-footer", halted == true
                ? "휴장이라 가격이 갱신되지 않아요."
                : $"한 장 · {Sim.Clock.TicksPerSession}틱");

            var change = Root.Q<Label>("chart-change");

            if (change != null)
            {
                ApplyChange(change, stock.SessionChangeRatio);
            }

            // 매 프레임 다시 그리라고 하지 않는다. 틱이 넘어갔을 때만 다시 그린다.
            // generateVisualContent 안에서는 트리를 건드리지 않으므로 여기서만 요청한다.
            if (ChartTickShown == Sim.Clock.Tick)
            {
                return;
            }


            ChartTickShown = Sim.Clock.Tick;
            var chart = Root.Q<VisualElement>("chart");

            if (chart != null)
            {
                chart.MarkDirtyRepaint();
            }

            RefreshChartAxis(stock);
        }

        /// <summary>
        /// 차트 아래에 가격 범위를 세 값으로 적는다. 격자 대신 이것만 둔다.
        /// 축이 없으면 선의 높낮이가 얼마를 뜻하는지 알 수 없다.
        /// </summary>
        private void RefreshChartAxis(AdventurerStock stock)
        {
            var axis = Root.Q<VisualElement>("chart-axis");

            if (axis == null)
            {
                return;
            }

            var history = stock.PriceHistory;

            if (history.Count < 2)
            {
                axis.Clear();
                return;
            }

            var min = history[0];
            var max = history[0];

            for (var i = 1; i < history.Count; i++)
            {
                if (history[i] < min)
                {
                    min = history[i];
                }

                if (history[i] > max)
                {
                    max = history[i];
                }
            }

            if (stock.SessionOpenPrice < min)
            {
                min = stock.SessionOpenPrice;
            }

            if (stock.SessionOpenPrice > max)
            {
                max = stock.SessionOpenPrice;
            }

            axis.Clear();
            AddAxisLabel(axis, $"저 {min:N1}");
            AddAxisLabel(axis, $"시작 {stock.SessionOpenPrice:N1}");
            AddAxisLabel(axis, $"고 {max:N1}");
        }

        private static void AddAxisLabel(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.AddToClassList("chart-axis-label");
            parent.Add(label);
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

            if (history.Count < 2)
            {
                return;
            }

            var size = context.visualElement.contentRect;

            if (size.width <= 0f || size.height <= 0f)
            {
                return;
            }

            var min = history[0];
            var max = history[0];

            for (var i = 1; i < history.Count; i++)
            {
                if (history[i] < min)
                {
                    min = history[i];
                }

                if (history[i] > max)
                {
                    max = history[i];
                }
            }

            if (stock.SessionOpenPrice < min)
            {
                min = stock.SessionOpenPrice;
            }

            if (stock.SessionOpenPrice > max)
            {
                max = stock.SessionOpenPrice;
            }

            var span = max - min;

            if (span <= 0.0001f)
            {
                span = 1f;
            }

            var painter = context.painter2D;
            var total = Sim.Clock.TicksPerSession;

            // 장 시작가 기준선. 점선 대신 옅은 실선으로 둔다.
            var baseY = size.height - (stock.SessionOpenPrice - min) / span * size.height;
            painter.strokeColor = new Color(0.78f, 0.81f, 0.85f, 1f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, baseY));
            painter.LineTo(new Vector2(size.width, baseY));
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
                var x = total <= 0 ? 0f : (float)i / total * size.width;
                var y = size.height - (history[i] - min) / span * size.height;

                if (i == 0)
                {
                    painter.MoveTo(new Vector2(x, y));
                    continue;
                }

                painter.LineTo(new Vector2(x, y));
            }

            painter.Stroke();
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

            if (BookTickShown == Sim.Clock.Tick)
            {
                return;
            }

            BookTickShown = Sim.Clock.Tick;
            OrderBook.FillAsks(stock, Sim.Clock.Tick, Asks);
            OrderBook.FillBids(stock, Sim.Clock.Tick, Bids);

            list.Clear();

            for (var i = Asks.Count - 1; i >= 0; i--)
            {
                list.Add(BuildBookRow(Asks[i], true));
            }

            var mid = new VisualElement();
            mid.AddToClassList("book-row");
            mid.AddToClassList("book-row--mid");
            var midLabel = new Label(stock.Price.ToString("N1"));
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
            var title = Root.Q<Label>("pending-title");

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
                var verb = order.Side == OrderSide.Side_Buy ? "매수" : "매도";

                var text = new Label($"{name} · {order.LimitPrice:N1} {order.Quantity}주 {verb}");
                text.AddToClassList("pending-text");
                text.AddToClassList(order.Side == OrderSide.Side_Buy ? "rise" : "fall");

                var cancel = new Button();
                cancel.text = "취소";
                cancel.AddToClassList("pending-cancel");
                var id = order.Id;
                cancel.clicked += () =>
                {
                    Sim.CancelOrder(id);
                    PendingCountShown = -1;
                    Message = "주문을 취소했습니다.";
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

            SetText("qty-value", $"{Quantity}주");

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

            SetText("limit-value", LimitPrice.ToString("N1"));

            var halted = Sim.Clock.Phase != MarketPhase.Phase_Open;
            var impact = PriceModel.OrderImpact(stock, Side, Quantity);
            var fill = stock.Price + impact * 0.5f;

            var shown = limiting == true ? LimitPrice : fill;

            SetText("price-label", halted == true ? "마지막 거래가" : "현재가");
            SetText("order-price", stock.Price.ToString("N1"));
            SetText("order-fill", halted == true ? "—" : shown.ToString("N1"));
            SetText("order-total", halted == true ? "—" : (shown * Quantity).ToString("N1"));
            SetText("order-cash", Sim.Wallet.AvailableCash.ToString("N0"));

            SetText("order-hint", limiting == true
                ? "가격이 지정가에 닿으면 체결됩니다. 닿지 않으면 폐장 때 사라집니다."
                : "체결 시 가격이 달라질 수 있어요.");

            var held = Sim.Wallet.QuantityOf(stock.Id);
            var available = Sim.Wallet.AvailableQuantityOf(stock.Id);
            SetText("hold-qty", held == available ? $"{held}주" : $"{held}주 (가용 {available})");
            SetText("hold-avg", Sim.Wallet.AverageCostOf(stock.Id).ToString("N1"));
            SetText("hold-liquidity", stock.Liquidity.ToString("N0"));

            var submit = Root.Q<Button>("submit-button");

            if (submit != null)
            {
                var blocked = Sim.Clock.AcceptsOrders == false || Sim.PlayerAtMarket == false;
                submit.SetEnabled(blocked == false);
                submit.EnableInClassList("btn--buy", blocked == false && buying);
                submit.EnableInClassList("btn--sell", blocked == false && buying == false);
                submit.EnableInClassList("btn--disabled", blocked);

                if (Sim.Clock.Phase == MarketPhase.Phase_Halted)
                {
                    submit.text = "휴장 중";
                }
                else if (Sim.Clock.IsPaused == true)
                {
                    submit.text = "정지 중";
                }
                else if (limiting == true)
                {
                    submit.text = $"{LimitPrice:N1}에 {Quantity}주 {(buying == true ? "매수" : "매도")}";
                }
                else
                {
                    submit.text = $"{Quantity}주 {(buying == true ? "매수" : "매도")}";
                }
            }

            SetText("order-message", Message);
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

            var popupNow = Root.Q<VisualElement>("info-popup");

            if (popupNow != null)
            {
                popupNow.EnableInClassList("info-popup--collapsed", InfoCollapsed);
            }

            var iconNow = Root.Q<Label>("info-toggle-icon");

            if (iconNow != null)
            {
                iconNow.text = InfoCollapsed == true ? "▸" : "▾";
            }

            // 목록은 매 프레임 다시 만들지 않는다. 기록이 늘었을 때만 다시 그린다.
            if (InfoCountShown == log.Count)
            {
                return;
            }

            var popup = Root.Q<VisualElement>("info-popup");

            if (popup != null)
            {
                popup.EnableInClassList("info-popup--collapsed", InfoCollapsed);
            }

            var icon = Root.Q<Label>("info-toggle-icon");

            if (icon != null)
            {
                icon.text = InfoCollapsed == true ? "▸" : "▾";
            }

            SetText("info-count", $"{log.Count}건");

            InfoCountShown = log.Count;
            list.Clear();

            var shown = 0;

            for (var i = log.Count - 1; i >= 0 && shown < INFO_MAX_ROWS; i--)
            {
                var entry = log[i];

                if (Filter == InfoFilter.Filter_Private && entry.IsPublic == true)
                {
                    continue;
                }

                if (Filter == InfoFilter.Filter_Public && entry.IsPublic == false)
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
            }

            Select("filter-all", Filter == InfoFilter.Filter_All);
            Select("filter-private", Filter == InfoFilter.Filter_Private);
            Select("filter-public", Filter == InfoFilter.Filter_Public);
        }

        private VisualElement BuildInfoCard(InfoEntry entry)
        {
            var card = new VisualElement();
            card.AddToClassList("info-card");
            card.EnableInClassList("info-card--private", entry.IsPublic == false);

            var head = new VisualElement();
            head.AddToClassList("info-head");

            var tag = new Label(entry.IsPublic == true ? "시장에 공개됨" : "나만 아는 정보");
            tag.AddToClassList("info-tag");
            tag.AddToClassList(entry.IsPublic == true ? "info-tag--public" : "info-tag--private");

            Builder.Length = 0;
            Builder.Append(SourceText(entry.Source)).Append(" · ").Append(entry.Tick).Append("틱");

            AdventurerStock stock;

            if (Sim.StockLookup.TryGetValue(entry.StockId, out stock) == true)
            {
                Builder.Append(" · ").Append(stock.DisplayName);
            }

            var meta = new Label(Builder.ToString());
            meta.AddToClassList("info-meta");

            head.Add(tag);
            head.Add(meta);

            var headline = new Label(entry.Headline);
            headline.AddToClassList("info-headline");

            card.Add(head);
            card.Add(headline);

            if (entry.IsPublic == false)
            {
                var note = new Label("아직 시장에 공개되지 않음.");
                note.AddToClassList("t-note");
                card.Add(note);
            }

            return card;
        }

        // ── 보조 ──────────────────────────────────

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
                    return "휴장";
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
