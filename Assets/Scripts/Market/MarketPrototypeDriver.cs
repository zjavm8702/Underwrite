using System.Text;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Utility;
using UnityEngine;

namespace Underwrite.Market
{
    /// <summary>
    /// 시장 코어를 씬에서 돌려 보는 시제품 드라이버. 기획서 §14의 검증 질문 1·2·6을 확인하는 용도다.
    ///
    /// UI는 IMGUI로 거칠게 만들었다. 실제 거래 화면(§5)은 검증 이후 UI Toolkit으로 다시 만든다.
    /// 이 클래스에는 규칙을 넣지 않는다. 판단은 모두 <see cref="MarketSimulation"/>에 있다.
    /// </summary>
    public sealed class MarketPrototypeDriver : MonoBehaviour
    {
        /// <summary>한 프레임이 시장에 보낼 수 있는 최대 실시간 초.</summary>
        private const float MAX_DELTA_SECONDS = 0.1f;

        [Header("장세")]
        [SerializeField] private int Seed = 20261016;
        [SerializeField] private bool IsInspectionDay;
        [SerializeField] private MarketSentiment StartingSentiment = MarketSentiment.Sentiment_Calm;

        [Header("주문")]
        [SerializeField] private int OrderQuantity = 10;

        [Header("후원 (기획서 §9)")]
        [SerializeField] private float PatronageAmount = 120f;
        [SerializeField] private float PatronageValueGain = 0.12f;

        [Header("진행")]
        [SerializeField] private bool AutoPauseOnOutcome = true;

        private MarketSimulation Sim;
        private Texture2D Pixel;
        private Vector2 LogScroll;
        private string LastMessage = "";
        private readonly StringBuilder Builder = new StringBuilder();

        private void Awake()
        {
            Pixel = new Texture2D(1, 1);
            Pixel.SetPixel(0, 0, Color.white);
            Pixel.Apply();

            Sim = new MarketSimulation(Seed, Constants.STARTING_CASH);

            var stocks = PrototypeScenario.CreateStocks();

            for (var i = 0; i < stocks.Count; i++)
            {
                Sim.AddStock(stocks[i]);
            }

            Sim.AddEvents(PrototypeScenario.CreateEvents());
            Sim.SetSentiment(StartingSentiment);
            Sim.OpenSession(IsInspectionDay);

            // 1단계에는 영지가 없으므로 지하시장 안에 있다고 가정한다. 토글로 밖에 나간 상태를 흉내낸다.
            Sim.PlayerAtMarket = true;

            UnderwriteLog.Info($"시장 시제품을 시작했다. 시드 {Seed}, 종목 {stocks.Count}개.", context: this);
        }

        private void OnDestroy()
        {
            if (Pixel != null)
            {
                Destroy(Pixel);
            }
        }

        private void Update()
        {
            // 플레이 중 재컴파일로 도메인이 리로드되면 Sim이 사라진다. 직렬화되지 않는 순수 객체다.
            // 이때 방치하면 매 프레임 NullReferenceException이 쌓인다.
            if (Sim == null)
            {
                return;
            }

            // 에디터 창이 비활성이었다가 돌아오면 deltaTime이 크게 튄다. 그대로 넘기면 수십 틱이
            // 한 배치로 몰려 사건 타이밍이 뭉개진다. 한 프레임이 보낼 수 있는 시간을 제한한다.
            var delta = Mathf.Min(Time.unscaledDeltaTime, MAX_DELTA_SECONDS);
            var ticks = Sim.Clock.Advance(delta);

            if (ticks <= 0)
            {
                return;
            }

            Sim.Step(ticks);

            // 결과가 확정된 사건에서 자동 정지한다. 기획서 §11.
            if (AutoPauseOnOutcome == true && Sim.HasPendingAlert == true)
            {
                Sim.Clock.SetPaused(true);
                LastMessage = "결과가 확정되어 자동 정지했습니다.";
            }
        }

        private void OnGUI()
        {
            if (Sim == null)
            {
                GUI.Label(new Rect(12f, 12f, 520f, 40f),
                    "재컴파일로 시장 상태가 사라졌습니다. 재생을 멈췄다가 다시 시작하세요.");
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 12f, 560f, Screen.height - 24f));

            DrawHeader();
            GUILayout.Space(6f);
            DrawStocks();
            GUILayout.Space(6f);
            DrawInfoLog();

            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            GUILayout.Label("지하거래소 — 시장 코어 시제품", GUI.skin.box);

            var hours = Sim.Clock.ElapsedMinutes / 60;
            var minutes = Sim.Clock.ElapsedMinutes % 60;

            Builder.Length = 0;
            Builder.Append(PhaseText(Sim.Clock.Phase));
            Builder.Append("   장중 ");
            Builder.Append(hours.ToString("00"));
            Builder.Append(':');
            Builder.Append(minutes.ToString("00"));
            Builder.Append("   틱 ");
            Builder.Append(Sim.Clock.Tick);
            Builder.Append(" / ");
            Builder.Append(Sim.Clock.TicksPerSession);
            Builder.Append("   분위기 ");
            Builder.Append(SentimentText(Sim.Sentiment));
            GUILayout.Label(Builder.ToString());

            var total = Sim.Wallet.TotalValue(Sim.StockLookup);
            Builder.Length = 0;
            Builder.Append("현금 ");
            Builder.Append(Sim.Wallet.Cash.ToString("N0"));
            Builder.Append("   평가액 ");
            Builder.Append(Sim.Wallet.HoldingsValue(Sim.StockLookup).ToString("N0"));
            Builder.Append("   총자산 ");
            Builder.Append(total.ToString("N0"));
            Builder.Append("   실현손익 ");
            Builder.Append(Sim.Wallet.RealizedProfit.ToString("N0"));
            Builder.Append("   후원지출 ");
            Builder.Append(Sim.Wallet.PatronageSpent.ToString("N0"));
            GUILayout.Label(Builder.ToString());

            GUILayout.BeginHorizontal();

            var pauseLabel = Sim.Clock.IsPaused == true ? "재개" : "일시정지";

            if (GUILayout.Button(pauseLabel) == true)
            {
                Sim.Clock.SetPaused(Sim.Clock.IsPaused == false);
            }

            if (GUILayout.Button("1배") == true)
            {
                Sim.Clock.SetSpeedScale(1f);
            }

            if (GUILayout.Button("4배") == true)
            {
                Sim.Clock.SetSpeedScale(4f);
            }

            if (GUILayout.Button("16배") == true)
            {
                Sim.Clock.SetSpeedScale(16f);
            }

            var placeLabel = Sim.PlayerAtMarket == true ? "지하시장에 있음" : "영지 이동 중";

            if (GUILayout.Button(placeLabel) == true)
            {
                Sim.PlayerAtMarket = Sim.PlayerAtMarket == false;
            }

            GUILayout.EndHorizontal();

            OrderQuantity = (int)GUILayout.HorizontalSlider(OrderQuantity, 1f, 100f);
            GUILayout.Label($"주문 수량 {OrderQuantity}주");

            if (string.IsNullOrEmpty(LastMessage) == false)
            {
                GUILayout.Label(LastMessage, GUI.skin.box);
            }
        }

        private void DrawStocks()
        {
            var stocks = Sim.Stocks;

            for (var i = 0; i < stocks.Count; i++)
            {
                var stock = stocks[i];

                GUILayout.BeginVertical(GUI.skin.box);

                var change = stock.SessionChangeRatio * 100f;
                var held = Sim.Wallet.QuantityOf(stock.Id);

                Builder.Length = 0;
                Builder.Append(stock.DisplayName);
                Builder.Append("   ");
                Builder.Append(stock.Price.ToString("N1"));
                Builder.Append("   ");
                Builder.Append(change >= 0f ? "+" : "");
                Builder.Append(change.ToString("N1"));
                Builder.Append('%');
                GUILayout.Label(Builder.ToString());

                Builder.Length = 0;
                Builder.Append("보유 ");
                Builder.Append(held);
                Builder.Append("주   평단 ");
                Builder.Append(Sim.Wallet.AverageCostOf(stock.Id).ToString("N1"));
                Builder.Append("   유동성 ");
                Builder.Append(stock.Liquidity.ToString("N0"));
                GUILayout.Label(Builder.ToString());

                DrawSparkline(stock);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("매수") == true)
                {
                    Execute(stock.Id, OrderSide.Side_Buy);
                }

                if (GUILayout.Button("매도") == true)
                {
                    Execute(stock.Id, OrderSide.Side_Sell);
                }

                if (GUILayout.Button($"후원 {PatronageAmount:N0}") == true)
                {
                    GrantPatronage(stock);
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        private void Execute(string stockId, OrderSide side)
        {
            var receipt = Sim.SubmitOrder(stockId, side, OrderQuantity);

            if (receipt.Result == OrderResult.Result_Filled)
            {
                var verb = side == OrderSide.Side_Buy ? "매수" : "매도";
                LastMessage = $"{verb} {receipt.Quantity}주 @ {receipt.FillPrice:N1} = {receipt.TotalAmount:N0}";
                return;
            }

            LastMessage = receipt.Message;
        }

        private void GrantPatronage(AdventurerStock stock)
        {
            var granted = Sim.GrantPatronage(stock.Id, PatronageAmount, PatronageValueGain);

            if (granted == true)
            {
                LastMessage = $"{stock.DisplayName}에게 {PatronageAmount:N0}을 후원했다. 효과는 서서히 나타난다.";
                return;
            }

            LastMessage = StringConstants.TEXT_ORDER_NO_CASH;
        }

        /// <summary>가격 이력을 막대로 그린다. 작은 변화는 차트로만 전달한다. 기획서 §13.</summary>
        private void DrawSparkline(AdventurerStock stock)
        {
            var rect = GUILayoutUtility.GetRect(520f, 48f);
            GUI.DrawTexture(rect, Pixel, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.25f), 0f, 0f);

            var history = stock.PriceHistory;

            if (history.Count < 2)
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

            var span = max - min;

            if (span <= 0f)
            {
                span = 1f;
            }

            var step = rect.width / (history.Count - 1);

            for (var i = 0; i < history.Count; i++)
            {
                var normalized = (history[i] - min) / span;
                var height = normalized * rect.height;
                var bar = new Rect(rect.x + step * i, rect.yMax - height, Mathf.Max(1f, step), 2f);
                GUI.DrawTexture(bar, Pixel, ScaleMode.StretchToFill, false, 0f, Color.white, 0f, 0f);
            }
        }

        private void DrawInfoLog()
        {
            GUILayout.Label("정보 기록 — 출처와 시점으로 신뢰도를 판단한다 (기획서 §8)", GUI.skin.box);

            LogScroll = GUILayout.BeginScrollView(LogScroll, GUILayout.Height(180f));

            var log = Sim.InfoLog;

            for (var i = log.Count - 1; i >= 0; i--)
            {
                var entry = log[i];

                Builder.Length = 0;
                Builder.Append('[');
                Builder.Append(entry.Tick);
                Builder.Append("] ");
                Builder.Append(SourceText(entry.Source));
                Builder.Append(entry.IsPublic == true ? " · 공개" : " · 나만 아는 정보");
                Builder.Append('\n');
                Builder.Append(entry.Headline);

                GUILayout.Label(Builder.ToString(), GUI.skin.box);
            }

            GUILayout.EndScrollView();
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
                    return StringConstants.TEXT_MARKET_PHASE_HALTED;
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
