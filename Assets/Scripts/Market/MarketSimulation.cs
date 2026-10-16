using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Market
{
    /// <summary>주문 처리 결과. 실패해도 예외를 던지지 않고 사유를 돌려준다.</summary>
    public struct OrderReceipt
    {
        public OrderResult Result;
        public int Quantity;
        public float FillPrice;
        public float TotalAmount;
        public string Message;
    }

    /// <summary>
    /// 시장 전체를 진행시킨다. 엔진에 의존하지 않으므로 테스트에서 틱을 직접 돌릴 수 있다.
    ///
    /// 담당 범위
    /// - 틱마다 종목 가격을 갱신하고 기대를 식힌다
    /// - 일정에 놓인 사건을 소문 → 보고 → 결과 순서로 적용한다
    /// - 시장가 주문을 체결하고 유동성에 따라 가격을 민다
    /// - 플레이어가 아는 정보만 정보 기록에 쌓는다
    ///
    /// 거래 장소 제약: 사용자 결정(2026-10-16)에 따라 주문은 지하시장에서만 받는다.
    /// 이동 중에는 가격과 속보 조회만 가능하다. <see cref="PlayerAtMarket"/>로 통제한다.
    /// </summary>
    public sealed class MarketSimulation
    {
        private readonly Dictionary<string, AdventurerStock> StockTable =
            new Dictionary<string, AdventurerStock>();
        private readonly List<AdventurerStock> StockOrder = new List<AdventurerStock>();
        private readonly List<MarketEventStep> Schedule = new List<MarketEventStep>();
        private readonly List<InfoEntry> InfoEntries = new List<InfoEntry>();
        private readonly Random Rng;

        private int NextEventIndex;

        public MarketClock Clock { get; private set; } = new MarketClock();
        public Portfolio Wallet { get; private set; }
        public MarketSentiment Sentiment { get; private set; } = MarketSentiment.Sentiment_Calm;

        /// <summary>플레이어가 지하시장 안에 있는지. 거짓이면 주문을 받지 않는다.</summary>
        public bool PlayerAtMarket { get; set; }

        /// <summary>마지막 틱에 결과가 확정된 사건이 있었는지. 자동 정지 판단에 쓴다. 기획서 §11.</summary>
        public bool HasPendingAlert { get; private set; }

        public MarketSimulation(int seed, float startingCash)
        {
            Rng = new Random(seed);
            Wallet = new Portfolio(startingCash);
        }

        public IReadOnlyList<AdventurerStock> Stocks
        {
            get
            {
                return StockOrder;
            }
        }

        public IReadOnlyDictionary<string, AdventurerStock> StockLookup
        {
            get
            {
                return StockTable;
            }
        }

        /// <summary>플레이어가 읽은 정보. 최신이 뒤에 온다.</summary>
        public IReadOnlyList<InfoEntry> InfoLog
        {
            get
            {
                return InfoEntries;
            }
        }

        public void AddStock(AdventurerStock stock)
        {
            if (stock == null)
            {
                throw new ArgumentNullException(nameof(stock));
            }

            if (StockTable.ContainsKey(stock.Id) == true)
            {
                throw new InvalidOperationException("A stock with the same id is already listed: " + stock.Id);
            }

            StockTable.Add(stock.Id, stock);
            StockOrder.Add(stock);
        }

        /// <summary>사건 일정을 넣는다. 틱 순서로 정렬해 보관한다.</summary>
        public void AddEvents(IEnumerable<MarketEventStep> steps)
        {
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            foreach (var step in steps)
            {
                if (StockTable.ContainsKey(step.StockId) == false)
                {
                    throw new InvalidOperationException("Event targets an unlisted stock: " + step.StockId);
                }

                Schedule.Add(step);
            }

            Schedule.Sort(CompareByTick);
            NextEventIndex = 0;
        }

        private static int CompareByTick(MarketEventStep left, MarketEventStep right)
        {
            return left.Tick.CompareTo(right.Tick);
        }

        public void SetSentiment(MarketSentiment sentiment)
        {
            Sentiment = sentiment;
        }

        /// <summary>장을 연다. 휴장일이면 가격이 멈춘 상태로 연다.</summary>
        public void OpenSession(bool isInspectionDay)
        {
            Clock.OpenSession(isInspectionDay);

            for (var i = 0; i < StockOrder.Count; i++)
            {
                StockOrder[i].BeginSession();
            }
        }

        /// <summary>
        /// 넘어간 틱 수만큼 시장을 진행시킨다. <see cref="MarketClock.Advance"/>의 반환값을 넘긴다.
        /// 휴장 중에는 사건만 진행하고 가격은 움직이지 않는다. 기획서 §11.
        /// </summary>
        public void Step(int ticks)
        {
            if (ticks <= 0)
            {
                return;
            }

            HasPendingAlert = false;
            // 한 번의 Advance가 장 마감을 넘어설 수 있다. 현재 Phase가 아니라 그 배치가 시작될 때의
            // Phase로 판단해야 마감을 넘은 틱의 가격 갱신이 사라지지 않는다.
            var priceMoves = Clock.PhaseDuringAdvance == MarketPhase.Phase_Open;

            for (var step = 0; step < ticks; step++)
            {
                ApplyDueEvents();

                if (priceMoves == false)
                {
                    continue;
                }

                for (var i = 0; i < StockOrder.Count; i++)
                {
                    var stock = StockOrder[i];

                    if (stock.IsTradable == false)
                    {
                        continue;
                    }

                    stock.DecayExpectation(Constants.EXPECTATION_DECAY_PER_TICK);
                    var gaussian = PriceModel.Gaussian(Rng);
                    stock.SetPrice(PriceModel.NextPrice(stock, Sentiment, gaussian));
                    stock.RecordHistory();
                }
            }
        }

        /// <summary>현재 틱까지 도달한 사건을 적용한다.</summary>
        private void ApplyDueEvents()
        {
            while (NextEventIndex < Schedule.Count && Schedule[NextEventIndex].Tick <= Clock.Tick)
            {
                var step = Schedule[NextEventIndex];
                NextEventIndex++;
                ApplyEvent(step);
            }
        }

        private void ApplyEvent(MarketEventStep step)
        {
            AdventurerStock stock;

            if (StockTable.TryGetValue(step.StockId, out stock) == false)
            {
                return;
            }

            // 시장에 공개된 정보만 가격에 반영된다. 플레이어만 아는 정보는 기대를 움직이지 않는다.
            if (step.AffectsMarket == true && step.ExpectationDelta != 0f)
            {
                stock.AddExpectation(step.ExpectationDelta);
            }

            if (step.Stage == EventStage.Stage_Outcome)
            {
                stock.ResolveOutcome(step.ValueDelta);
                HasPendingAlert = true;
            }

            if (step.VisibleToPlayer == true)
            {
                InfoEntries.Add(new InfoEntry(
                    Clock.Tick, step.StockId, step.Headline, step.Source, step.Stage, step.AffectsMarket));
            }
        }

        /// <summary>
        /// 시장가 주문. 체결가는 충격의 절반을 적용한 값이고, 체결 뒤 가격은 충격 전체만큼 움직인다.
        /// 지정가 주문과 수수료는 시제품 범위 밖이다. 기획서 §7.
        /// </summary>
        public OrderReceipt SubmitOrder(string stockId, OrderSide side, int quantity)
        {
            if (quantity <= 0)
            {
                return Reject(OrderResult.Result_RejectedBadQuantity, StringConstants.TEXT_ORDER_BAD_QUANTITY);
            }

            if (PlayerAtMarket == false)
            {
                return Reject(OrderResult.Result_RejectedNotAtMarket, StringConstants.TEXT_ORDER_NOT_AT_MARKET);
            }

            // 정지와 휴장을 나눠 알린다. 플레이어가 무엇을 풀어야 하는지 알아야 한다.
            if (Clock.IsPaused == true)
            {
                return Reject(OrderResult.Result_RejectedPaused, StringConstants.TEXT_ORDER_PAUSED);
            }

            if (Clock.AcceptsOrders == false)
            {
                return Reject(OrderResult.Result_RejectedMarketClosed, StringConstants.TEXT_ORDER_MARKET_CLOSED);
            }

            AdventurerStock stock;

            if (StockTable.TryGetValue(stockId, out stock) == false || stock.IsTradable == false)
            {
                return Reject(OrderResult.Result_RejectedMarketClosed, StringConstants.TEXT_ORDER_MARKET_CLOSED);
            }

            var impact = PriceModel.OrderImpact(stock, side, quantity);
            var fill = stock.Price + impact * 0.5f;

            if (fill < Constants.PRICE_FLOOR)
            {
                fill = Constants.PRICE_FLOOR;
            }

            var amount = fill * quantity;

            if (side == OrderSide.Side_Buy)
            {
                if (Wallet.CanAfford(amount) == false)
                {
                    return Reject(OrderResult.Result_RejectedNoCash, StringConstants.TEXT_ORDER_NO_CASH);
                }

                Wallet.Buy(stockId, quantity, fill);
            }
            else
            {
                if (Wallet.QuantityOf(stockId) < quantity)
                {
                    return Reject(OrderResult.Result_RejectedNoShares, StringConstants.TEXT_ORDER_NO_SHARES);
                }

                Wallet.Sell(stockId, quantity, fill);
            }

            stock.SetPrice(PriceModel.ClampMove(stock.Price, stock.Price + impact));

            // 거래가 일어나면 유동성이 조금 늘어난다. 거래량이 유동성을 만든다. 기획서 §7.
            stock.AdjustLiquidity(quantity * 0.1f);

            return new OrderReceipt
            {
                Result = OrderResult.Result_Filled,
                Quantity = quantity,
                FillPrice = fill,
                TotalAmount = amount,
                Message = StringConstants.TEXT_ORDER_FILLED
            };
        }

        private static OrderReceipt Reject(OrderResult result, string message)
        {
            return new OrderReceipt
            {
                Result = result,
                Quantity = 0,
                FillPrice = 0f,
                TotalAmount = 0f,
                Message = message
            };
        }

        /// <summary>
        /// 후원을 집행한다. 시장 거래와 분리된 지출이며 모험가의 실제 전망을 올린다. 기획서 §9.
        /// 효과가 즉시 가격에 다 반영되지는 않는다. 내재가치만 올리고 기대는 건드리지 않으므로,
        /// 가격은 다음 틱부터 목표가를 향해 서서히 따라온다.
        /// </summary>
        public bool GrantPatronage(string stockId, float amount, float valueGain)
        {
            AdventurerStock stock;

            if (StockTable.TryGetValue(stockId, out stock) == false)
            {
                return false;
            }

            if (Wallet.SpendPatronage(amount) == false)
            {
                return false;
            }

            stock.ResolvePatronage(valueGain);
            return true;
        }
    }
}
