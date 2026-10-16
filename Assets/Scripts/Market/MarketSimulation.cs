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
    /// <summary>아직 상장되지 않은 종목과 올라올 주기.</summary>
    public struct WaitingEntry
    {
        public AdventurerStock Stock;
        public int Cycle;
    }

    /// <summary>떠날 주기와 사유.</summary>
    public struct DeparturePlan
    {
        public int Cycle;
        public DelistReason Reason;
    }

    public sealed class MarketSimulation
    {
        private readonly List<WaitingEntry> Waiting = new List<WaitingEntry>();
        private readonly Dictionary<string, DeparturePlan> Departures =
            new Dictionary<string, DeparturePlan>();

        private readonly Dictionary<string, AdventurerStock> StockTable =
            new Dictionary<string, AdventurerStock>();
        private readonly List<AdventurerStock> StockOrder = new List<AdventurerStock>();
        private readonly List<MarketEventStep> Schedule = new List<MarketEventStep>();
        private readonly List<InfoEntry> InfoEntries = new List<InfoEntry>();
        private readonly List<PendingOrder> Pending = new List<PendingOrder>();
        private readonly Random Rng;
        private readonly Random MicroRng;
        private float MicroTimer;

        private int NextEventIndex;
        private int InfoTimesOpened;
        private int NextSlot;
        private readonly List<InfoEntry> Briefing = new List<InfoEntry>();
        private readonly List<string> Notices = new List<string>();

        public MarketClock Clock { get; private set; } = new MarketClock();
        public Portfolio Wallet { get; private set; }
        public MarketSentiment Sentiment { get; private set; } = MarketSentiment.Sentiment_Calm;

        /// <summary>플레이어가 지하시장 안에 있는지. 거짓이면 주문을 받지 않는다.</summary>
        public bool PlayerAtMarket { get; set; }

        /// <summary>마지막 틱에 결과가 확정된 사건이 있었는지. 자동 정지 판단에 쓴다. 기획서 §11.</summary>
        public bool HasPendingAlert { get; private set; }

        /// <summary>대기 중이거나 끝난 지정가 주문. 최신이 뒤에 온다.</summary>
        public IReadOnlyList<PendingOrder> PendingOrders
        {
            get
            {
                return Pending;
            }
        }

        /// <summary>마지막 Step에서 체결된 지정가 주문 수. 알림에 쓴다.</summary>
        public int LastFilledCount { get; private set; }

        /// <summary>미세 호가가 몇 번 바뀌었는가. 호가창 잔량이 이 값으로 섞인다.</summary>
        public int MicroSeq { get; private set; }

        /// <summary>
        /// 정보 타임이 열려 플레이어가 아직 확인하지 않은 상태인가.
        /// 켜져 있는 동안 장을 세우고 보고를 보여 준다.
        /// </summary>
        public bool HasBriefing { get; private set; }

        /// <summary>이번 정보 타임에 들어온 소식. 확인하면 비운다.</summary>
        public IReadOnlyList<InfoEntry> BriefingEntries
        {
            get
            {
                return Briefing;
            }
        }

        /// <summary>다음 정보 타임의 번호. 영지에서 캐낼 소식이 이 묶음에서 나온다.</summary>
        public int NextBriefingSlot
        {
            get
            {
                return NextSlot;
            }
        }

        /// <summary>이번 장에서 몇 번째 정보 타임인가. 1 또는 2.</summary>
        public int BriefingOrder { get; private set; }

        /// <summary>
        /// 주기가 넘어갈 때 일어난 일. 상장·공고·이탈을 한 줄씩 담는다.
        /// 화면이 이것을 읽어 쪽지로 띄운다.
        /// </summary>
        public IReadOnlyList<string> CycleNotices
        {
            get
            {
                return Notices;
            }
        }

        /// <summary>
        /// 영지에서 한 일을 받아 적용한다. 장을 열기 전에 한 번 부른다.
        ///
        /// 돈은 여기서 빠진다. 영지가 직접 지갑을 건드리면 잔고가 두 군데가 되고,
        /// 어느 쪽이 맞는지 알 수 없게 된다.
        /// 캐낸 소식은 **나만 아는 정보**로 들어간다. 시장은 아직 모르므로 가격은 움직이지 않는다.
        /// </summary>
        public int ApplyGatherSession()
        {
            var spent = GatherSession.Spent;

            if (spent > 0f)
            {
                Wallet.SpendPatronage(spent);
            }

            var notes = GatherSession.Gathered;

            for (var i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                InfoEntries.Add(new InfoEntry(Clock.Tick, note.StockId, note.Headline,
                    note.Source, EventStage.Stage_Report, false));
            }

            return notes.Count;
        }

        /// <summary>보고를 읽었다. 장이 다시 움직인다.</summary>
        public void AcknowledgeBriefing()
        {
            HasBriefing = false;
            Briefing.Clear();
        }

        public MarketSimulation(int seed, float startingCash)
        {
            MicroRng = new Random(seed ^ 0x5F3759DF);
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

            Schedule.Sort(CompareBySlot);
            NextEventIndex = 0;
        }

        private static int CompareBySlot(MarketEventStep left, MarketEventStep right)
        {
            return left.Slot.CompareTo(right.Slot);
        }

        public void SetSentiment(MarketSentiment sentiment)
        {
            Sentiment = sentiment;
        }

        /// <summary>장을 연다. 휴장일이면 가격이 멈춘 상태로 연다.</summary>
        public void OpenSession(bool isInspectionDay, MarketSession session = MarketSession.Session_Morning)
        {
            Clock.OpenSession(isInspectionDay, session);

            InfoTimesOpened = 0;
            HasBriefing = false;
            Briefing.Clear();

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
                OpenBriefingIfDue();

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
                    var momentum = PriceModel.NextMomentum(stock, Sentiment, gaussian);
                    stock.SetMomentum(momentum);
                    stock.SetPrice(PriceModel.NextPrice(stock, Sentiment, momentum));
                    stock.RecordHistory();
                }

                // 가격이 움직인 뒤에 지정가를 확인한다. 순서가 바뀌면 한 틱 늦게 체결된다.
                ResolvePending();
            }
        }

        /// <summary>
        /// 현재가가 통과한 지정가 주문을 체결한다. 체결가는 지정가 그대로다.
        /// 시장가와 달리 유리한 가격이 보장되는 대신 체결 자체는 보장되지 않는다.
        /// </summary>
        private void ResolvePending()
        {
            LastFilledCount = 0;

            for (var i = 0; i < Pending.Count; i++)
            {
                var order = Pending[i];

                if (order.State != PendingState.Pending_Waiting)
                {
                    continue;
                }

                AdventurerStock stock;

                if (StockTable.TryGetValue(order.StockId, out stock) == false || stock.IsTradable == false)
                {
                    continue;
                }

                if (order.ShouldFill(stock.LivePrice) == false)
                {
                    continue;
                }

                if (order.Side == OrderSide.Side_Buy)
                {
                    Wallet.ReleaseCash(order.ReservedCash);
                    Wallet.Buy(order.StockId, order.Quantity, order.LimitPrice);
                }
                else
                {
                    Wallet.ReleaseShares(order.StockId, order.Quantity);
                    Wallet.Sell(order.StockId, order.Quantity, order.LimitPrice);
                }

                order.MarkFilled();
                LastFilledCount++;

                // 체결도 가격을 민다. 시장가와 같은 유동성 규칙을 쓴다.
                var impact = PriceModel.OrderImpact(stock, order.Side, order.Quantity);
                stock.SetPrice(PriceModel.ClampMove(stock.Price, stock.Price + impact));
                stock.AdjustLiquidity(order.Quantity * 0.1f);
            }
        }

        /// <summary>
        /// 장 하나가 끝났음을 알리고, 주기가 넘어가면 상장과 이탈을 처리한다.
        /// 설계안 §3의 생애(도착 → 상장 → 성장 → 이탈)에서 시장에 보이는 두 지점만 다룬다.
        /// </summary>
        public void EndSessionAndRoll()
        {
            Notices.Clear();

            if (Clock.CountSessionAndAdvanceCycle() == false)
            {
                return;
            }

            ApplyDepartures();
            ApplyArrivals();
            ApplyNotices();
        }

        /// <summary>이번 주기에 떠나기로 되어 있는 종목을 내보낸다. 보유분은 청산한다.</summary>
        private void ApplyDepartures()
        {
            for (var i = 0; i < StockOrder.Count; i++)
            {
                var stock = StockOrder[i];

                if (stock.IsTradable == false)
                {
                    continue;
                }

                var plan = DepartureOf(stock.Id);

                if (plan.Cycle <= 0 || plan.Cycle > Clock.Cycle)
                {
                    continue;
                }

                stock.Leave(plan.Reason);

                var held = Wallet.QuantityOf(stock.Id);
                var ratio = AdventurerStock.LiquidationRatioOf(plan.Reason);

                if (held <= 0)
                {
                    Notices.Add($"{stock.DisplayName} 상장 폐지. 보유分 없음.".Replace("分", "분"));
                    continue;
                }

                // 묶여 있던 지분을 먼저 풀어야 청산할 수 있다. 지정가 매도가 걸려 있을 수 있다.
                ReleaseOrdersOf(stock.Id);

                var unit = stock.TrueValue * ratio;
                Wallet.Sell(stock.Id, held, unit);
                Notices.Add($"{stock.DisplayName} 상장 폐지. {held}주를 주당 {unit:N1}에 청산했습니다.");
            }
        }

        /// <summary>이번 주기에 올라오는 종목을 상장한다.</summary>
        private void ApplyArrivals()
        {
            for (var i = 0; i < Waiting.Count; i++)
            {
                var entry = Waiting[i];

                if (entry.Cycle > Clock.Cycle)
                {
                    continue;
                }

                if (StockTable.ContainsKey(entry.Stock.Id) == true)
                {
                    continue;
                }

                AddStock(entry.Stock);
                Notices.Add($"{entry.Stock.DisplayName} 신규 상장. 공모가 {entry.Stock.Price:N1}.");
            }

            Waiting.RemoveAll(e => e.Cycle <= Clock.Cycle);
        }

        /// <summary>다음 주기에 떠날 종목을 미리 알린다. 기습 상장폐지는 설계안 §5가 금한다.</summary>
        private void ApplyNotices()
        {
            for (var i = 0; i < StockOrder.Count; i++)
            {
                var stock = StockOrder[i];

                if (stock.IsTradable == false || stock.NoticedDelistCycle > 0)
                {
                    continue;
                }

                var plan = DepartureOf(stock.Id);

                if (plan.Cycle != Clock.Cycle + 1)
                {
                    continue;
                }

                // 전멸은 예고하지 않는다. 그 갑작스러움이 고위험 종목의 진짜 위험이다.
                if (plan.Reason == DelistReason.Delist_Wiped)
                {
                    continue;
                }

                stock.NoticeDeparture(plan.Cycle);
                var ratio = AdventurerStock.LiquidationRatioOf(plan.Reason) * 100f;
                Notices.Add($"{stock.DisplayName} 다음 주기 상장 폐지 예고. 청산율 {ratio:N0}퍼센트.");
            }
        }

        private DeparturePlan DepartureOf(string stockId)
        {
            DeparturePlan plan;

            if (Departures.TryGetValue(stockId, out plan) == true)
            {
                return plan;
            }

            return new DeparturePlan();
        }

        /// <summary>청산 전에 그 종목에 걸린 대기 주문을 거둔다. 묶인 자산을 풀어야 한다.</summary>
        private void ReleaseOrdersOf(string stockId)
        {
            for (var i = 0; i < Pending.Count; i++)
            {
                var order = Pending[i];

                if (order.StockId != stockId || order.State != PendingState.Pending_Waiting)
                {
                    continue;
                }

                if (order.Side == OrderSide.Side_Buy)
                {
                    Wallet.ReleaseCash(order.ReservedCash);
                }
                else
                {
                    Wallet.ReleaseShares(order.StockId, order.Quantity);
                }

                order.MarkCancelled();
            }
        }

        /// <summary>아직 상장되지 않은 종목과 그 주기.</summary>
        public void AddWaiting(AdventurerStock stock, int cycle)
        {
            Waiting.Add(new WaitingEntry { Stock = stock, Cycle = cycle });
        }

        /// <summary>떠날 주기와 사유를 등록한다. 상장 시점에 청산율을 보여 주기 위해 종목에도 적어 둔다.</summary>
        public void PlanDeparture(string stockId, int cycle, DelistReason reason)
        {
            if (cycle <= 0 || reason == DelistReason.Delist_None)
            {
                return;
            }

            Departures[stockId] = new DeparturePlan { Cycle = cycle, Reason = reason };

            AdventurerStock stock;

            if (StockTable.TryGetValue(stockId, out stock) == true)
            {
                stock.PlanDeparture(reason);
            }
        }

        /// <summary>
        /// 틱 사이의 미세 호가를 돌린다. 매 프레임 불린다.
        ///
        /// 틱 종가는 건드리지 않고 그 주위에서만 떨린다. 실제 호가창은 틱 단위로 멈춰 있지 않고
        /// 쉬지 않고 흔들리며, 그래서 "지금 이 값에 그냥 걸어 두면 된다"가 성립하지 않는다.
        /// 지정가도 여기서 확인하므로 틱을 기다리지 않고 체결된다.
        /// 장이 멈춰 있으면 호가도 멈춘다. 주문을 받지 않는 동안 호가만 움직이면 거짓말이 된다.
        /// </summary>
        public void MicroStep(float deltaSeconds)
        {
            if (Clock.AcceptsOrders == false)
            {
                return;
            }

            MicroTimer += deltaSeconds;

            if (MicroTimer < Constants.MARKET_MICRO_SECONDS)
            {
                return;
            }

            MicroTimer = 0f;
            MicroSeq++;

            for (var i = 0; i < StockOrder.Count; i++)
            {
                var stock = StockOrder[i];

                if (stock.IsTradable == false)
                {
                    continue;
                }

                var gaussian = PriceModel.Gaussian(MicroRng);
                stock.SetMicroOffset(PriceModel.NextMicroOffset(stock, gaussian));
            }

            ResolvePending();
        }

        /// <summary>
        /// 지정가 주문을 건다. 매수는 현금을, 매도는 지분을 묶어 둔다.
        /// 묶지 않으면 같은 자산으로 주문을 무한히 걸 수 있다.
        /// </summary>
        public OrderReceipt SubmitLimitOrder(string stockId, OrderSide side, int quantity, float limitPrice)
        {
            if (quantity <= 0)
            {
                return Reject(OrderResult.Result_RejectedBadQuantity, StringConstants.TEXT_ORDER_BAD_QUANTITY);
            }

            if (PlayerAtMarket == false)
            {
                return Reject(OrderResult.Result_RejectedNotAtMarket, StringConstants.TEXT_ORDER_NOT_AT_MARKET);
            }

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

            var price = OrderBook.SnapToTick(limitPrice, stock.Price);

            if (price < Constants.PRICE_FLOOR)
            {
                return Reject(OrderResult.Result_RejectedBadPrice, StringConstants.TEXT_ORDER_BAD_PRICE);
            }

            if (CountWaiting(stockId) >= Constants.PENDING_MAX_PER_STOCK)
            {
                return Reject(OrderResult.Result_RejectedBadQuantity, StringConstants.TEXT_ORDER_TOO_MANY);
            }

            var reserved = 0f;

            if (side == OrderSide.Side_Buy)
            {
                reserved = price * quantity;

                if (Wallet.ReserveCash(reserved) == false)
                {
                    return Reject(OrderResult.Result_RejectedNoCash, StringConstants.TEXT_ORDER_NO_CASH);
                }
            }
            else
            {
                if (Wallet.ReserveShares(stockId, quantity) == false)
                {
                    return Reject(OrderResult.Result_RejectedNoShares, StringConstants.TEXT_ORDER_NO_SHARES);
                }
            }

            Pending.Add(new PendingOrder(stockId, side, price, quantity, Clock.Tick, reserved));

            return new OrderReceipt
            {
                Result = OrderResult.Result_Queued,
                Quantity = quantity,
                FillPrice = price,
                TotalAmount = price * quantity,
                Message = StringConstants.TEXT_ORDER_QUEUED
            };
        }

        private int CountWaiting(string stockId)
        {
            var count = 0;

            for (var i = 0; i < Pending.Count; i++)
            {
                if (Pending[i].State == PendingState.Pending_Waiting && Pending[i].StockId == stockId)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>대기 중인 지정가 주문을 취소하고 묶어 둔 자산을 푼다.</summary>
        public bool CancelOrder(int orderId)
        {
            for (var i = 0; i < Pending.Count; i++)
            {
                var order = Pending[i];

                if (order.Id != orderId || order.State != PendingState.Pending_Waiting)
                {
                    continue;
                }

                if (order.Side == OrderSide.Side_Buy)
                {
                    Wallet.ReleaseCash(order.ReservedCash);
                }
                else
                {
                    Wallet.ReleaseShares(order.StockId, order.Quantity);
                }

                order.MarkCancelled();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 남은 틱을 전부 돌려 장을 끝낸다. 기획서 §11의 "사건이 적은 기간은 넘긴다"에 해당한다.
        /// 사건과 가격은 모두 정상 처리되므로 넘겨도 놓치는 것은 없다. 보는 시간만 줄어든다.
        /// 넘기는 동안 체결된 지정가 수를 돌려준다.
        /// </summary>
        public int SkipToClose()
        {
            var filled = 0;
            var guard = 0;

            while (Clock.Tick < Clock.TicksPerSession && guard < Clock.TicksPerSession + 8)
            {
                guard++;
                Clock.ForceTick();
                Step(1);
                filled += LastFilledCount;
            }

            ExpirePendingAtClose();
            Clock.CloseSession();
            return filled;
        }

        /// <summary>다음 장을 연다. 오전장 다음은 오후장, 오후장 다음은 이튿날 오전장이다.</summary>
        public void OpenNextSession(bool isInspectionDay)
        {
            var next = Clock.Session == MarketSession.Session_Morning
                ? MarketSession.Session_Evening
                : MarketSession.Session_Morning;

            if (next == MarketSession.Session_Morning)
            {
                Clock.AdvanceDay();
            }

            OpenSession(isInspectionDay, next);
        }

        /// <summary>폐장 시 남은 지정가 주문을 정리한다. 다음 장으로 넘기지 않는다.</summary>
        public void ExpirePendingAtClose()
        {
            for (var i = 0; i < Pending.Count; i++)
            {
                var order = Pending[i];

                if (order.State != PendingState.Pending_Waiting)
                {
                    continue;
                }

                if (order.Side == OrderSide.Side_Buy)
                {
                    Wallet.ReleaseCash(order.ReservedCash);
                }
                else
                {
                    Wallet.ReleaseShares(order.StockId, order.Quantity);
                }

                order.MarkExpired();
            }
        }

        /// <summary>현재 틱까지 도달한 사건을 적용한다.</summary>
        /// <summary>
        /// 정보 타임이 되면 그 번호의 소식을 한꺼번에 들여보낸다.
        ///
        /// 소식이 장중 아무 때나 흘러가면 플레이어가 판단할 "순간"이 없다.
        /// 정해진 시각에 묶어서 주면 그 자리에서 읽고 결정하게 된다.
        /// 들어온 소식은 지하세계가 동시에 알게 되므로 가격도 그 자리에서 함께 움직인다.
        /// </summary>
        private void OpenBriefingIfDue()
        {
            if (Clock.Tick < NextInfoTick)
            {
                return;
            }

            InfoTimesOpened++;
            BriefingOrder = InfoTimesOpened;

            var before = InfoEntries.Count;

            while (NextEventIndex < Schedule.Count && Schedule[NextEventIndex].Slot <= NextSlot)
            {
                var step = Schedule[NextEventIndex];
                NextEventIndex++;
                ApplyEvent(step);
            }

            NextSlot++;

            Briefing.Clear();

            for (var i = before; i < InfoEntries.Count; i++)
            {
                Briefing.Add(InfoEntries[i]);
            }

            // 빈 정보 타임은 세우지 않는다. 아무 소식도 없는데 멈추면 성가시기만 하다.
            if (Briefing.Count > 0)
            {
                HasBriefing = true;
            }
        }

        /// <summary>다음 정보 타임이 열리는 틱. 두 번을 다 쓰면 이 장에는 더 없다.</summary>
        private int NextInfoTick
        {
            get
            {
                if (InfoTimesOpened == 0)
                {
                    return Constants.INFO_TIME_FIRST_TICK;
                }

                if (InfoTimesOpened == 1)
                {
                    return Constants.INFO_TIME_SECOND_TICK;
                }

                return int.MaxValue;
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
            var fill = stock.LivePrice + impact * 0.5f;

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
