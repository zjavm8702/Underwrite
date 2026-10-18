using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Save;
using Underwrite.Utility;

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
        // 불러오면 다시 씨앗을 뿌린다. Random의 속을 저장할 수 없어서다.
        private Random Rng;
        private Random MicroRng;

        // 주기마다 새 용병단을 짓는다. 가격 흐름과 따로 굴려야 단이 늘어도 가격 흐름이 바뀌지 않는다.
        private Random BandRng;
        private int BandSerial;
        private readonly List<GeneratedBandState> Generated = new List<GeneratedBandState>();
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
            // 쓴 돈과 번 돈을 함께 셈한다. 영지는 돈을 쓰기만 하는 곳이 아니다.
            var net = GatherSession.NetChange;

            if (net < 0f)
            {
                Wallet.SpendPatronage(-net);
            }
            else if (net > 0f)
            {
                Wallet.Earn(net);
            }

            var notes = GatherSession.Gathered;

            for (var i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                InfoEntries.Add(new InfoEntry(Clock.Tick, note.StockId, note.Headline,
                    note.Source, EventStage.Stage_Report, false, Clock.Day, Clock.Session));
            }

            return notes.Count;
        }

        /// <summary>
        /// 플레이어가 쉬기로 한 장을 실제로 치른다. 기획서 §11 — 장에 나가는 것은 선택이다.
        ///
        /// **쉰다는 것은 시장이 멈춘다는 뜻이 아니다.** 그 장도 열리고 닫힌다.
        /// 가격이 움직이고, 소식이 돌고, 주기가 넘어가면 상장과 이탈이 일어난다.
        /// 플레이어만 그 자리에 없을 뿐이다. 그래야 "오늘 쉰 대가"가 생긴다.
        ///
        /// 정보 타임도 지나간다. 다만 대화로 전해 줄 사람이 없으므로 연출은 건너뛴다.
        /// 소식 자체는 <see cref="InfoEntries"/>에 남아 정보 기록에서 볼 수 있다.
        /// 현장에서 듣는 것은 놓치되 기록은 남는 셈이다.
        ///
        /// 걸어 둔 지정가 주문은 그대로 일한다. 쉬는 것은 플레이어이지 주문이 아니다.
        /// 다만 폐장 때 남은 것은 평소와 같이 정리된다.
        /// </summary>
        /// <returns>치른 장의 수.</returns>
        public int RunSkippedSessions(bool isInspectionDay)
        {
            var count = Clock.SkippedSessions;

            if (count <= 0)
            {
                return 0;
            }

            for (var i = 0; i < count; i++)
            {
                OpenNextSession(isInspectionDay);
                SkipToClose();
                EndSession();
            }

            // 연출을 띄울 사람이 없었다. 쌓인 보고는 내려놓는다.
            HasBriefing = false;
            Briefing.Clear();

            Clock.ClearSkippedSessions();
            return count;
        }

        /// <summary>보고를 읽었다. 장이 다시 움직인다.</summary>
        public void AcknowledgeBriefing()
        {
            HasBriefing = false;
            Briefing.Clear();
        }

        /// <summary>처음 만들 때의 씨앗. 불러올 때 같은 판을 다시 짜는 데 쓴다.</summary>
        public int Seed { get; private set; }

        public MarketSimulation(int seed, float startingCash)
        {
            Seed = seed;
            MicroRng = new Random(seed ^ 0x5F3759DF);
            Rng = new Random(seed);
            BandRng = new Random(seed ^ 0x2545F491);
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

                // 체결도 가격을 민다. 시장가와 같이 호가를 먹은 만큼만 움직인다.
                float sweptAverage;
                float sweptLast;
                OrderBook.Sweep(stock, MicroSeq, order.Side, order.Quantity, out sweptAverage, out sweptLast);
                stock.SetPrice(PriceModel.ClampMove(stock.Price, sweptLast));
                stock.AdjustLiquidity(order.Quantity * 0.1f);
            }
        }

        /// <summary>장 하나가 끝났음을 알린다. 계절 넘김은 여기가 아니라 재개장에서 한다(<see cref="OpenNextSession"/>).</summary>
        public void EndSession()
        {
            GrowIndexStocks();
            Clock.CountSession();
        }

        /// <summary>
        /// 재개장 장이 열렸는가, 그리고 원정 결과로 값이 뛰고 떨어진 종목 수. 거래 화면이 알릴 때 읽는다.
        /// 다음 장이 열리면 지워진다.
        /// </summary>
        public bool JustReopened { get; private set; }
        public int ReopenGapUp { get; private set; }
        public int ReopenGapDown { get; private set; }

        /// <summary>
        /// 원정 휴장이 끝나고 새 계절의 재개장 장이 열린다(시간구조 설계안 §2).
        /// 지난 계절의 결과(도감 사정·소문의 진위)가 드러나고, 상장과 이탈이 처리되고, 새 단이 영지에 온다.
        /// 설계안 §3의 생애(도착 → 상장 → 성장 → 이탈)에서 시장에 보이는 지점들이다.
        /// </summary>
        private void RollSeason()
        {
            ResolveCodexCycle(Clock.Cycle - 1);
            ApplyDepartures();
            ApplyArrivals();
            GenerateArrivals();
            ApplyNotices();
            SpreadTavernTalk(Clock.Cycle);
        }

        /// <summary>
        /// 끝난 주기의 대장간 사정과 여관 소문이 결과로 드러난다. 모험단 도감에서 캔 정보가 값으로 이어지는 곳이다.
        /// 대장간 장부는 늘 맞고, 여관 소문은 확률로 맞거나 헛말이 된다. 드러난 결과는 장 전체에 공개된다.
        /// 새로 온 단은 같은 유형의 문장 묶음을 쓴다.
        /// </summary>
        private void ResolveCodexCycle(int cycle)
        {
            var turn = BandCodex.TurnOf(cycle);
            var subjects = CodexResolveTargets();

            for (var i = 0; i < subjects.Count; i++)
            {
                var stock = subjects[i];
                var p = stock.CodexProfile;
                var smithEffect = Constants.CODEX_SMITH_EFFECT[p][turn];

                if (smithEffect != 0f)
                {
                    stock.ResolveOutcome(smithEffect);
                    AddCodexOutcome(stock.Id, Josa.Format(StringConstants.CODEX_SMITH_OUTCOME[p][turn], stock.DisplayName),
                        InfoSource.Source_SmithOrder, BandCodex.LearnedIn(stock.Id, CodexSource.Codex_Smith, cycle));
                }

                var rumor = StringConstants.CODEX_TAVERN[p][turn];
                var heard = BandCodex.LearnedIn(stock.Id, CodexSource.Codex_Tavern, cycle);

                if (Rng.NextDouble() < Constants.CODEX_TAVERN_TRUTH[p][turn])
                {
                    stock.ResolveOutcome(Constants.CODEX_TAVERN_EFFECT[p][turn]);
                    AddCodexOutcome(stock.Id, Josa.Format(StringConstants.CODEX_TAVERN_OUTCOME[p][turn], stock.DisplayName),
                        InfoSource.Source_TavernTalk, heard);
                }
                else
                {
                    // 헛말로 드러나면 소문이 붙여 놓은 기대가 빠진다. 가치는 그대로다.
                    stock.ResolveOutcome(0f);
                    AddCodexOutcome(stock.Id, string.Format(StringConstants.TEXT_CODEX_RUMOR_FALSE, stock.DisplayName,
                        rumor.TrimEnd('.')), InfoSource.Source_TavernTalk, heard);
                }
            }
        }

        /// <summary>
        /// 주기 결과를 받을 종목. 상장된 단, 그리고 이번 주기에 상장될 도착한 단이다.
        /// 도착한 단은 공모가는 그대로 두고 가치만 바뀐다 — 상장 전에 캔 사람이 상장 직후에 득을 본다.
        /// </summary>
        private List<AdventurerStock> CodexResolveTargets()
        {
            var list = new List<AdventurerStock>();

            for (var i = 0; i < StockOrder.Count; i++)
            {
                if (StockOrder[i].IsTradable == true && StockOrder[i].CodexProfile >= 0)
                {
                    list.Add(StockOrder[i]);
                }
            }

            for (var i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Cycle <= Clock.Cycle && Waiting[i].Stock.CodexProfile >= 0)
                {
                    list.Add(Waiting[i].Stock);
                }
            }

            return list;
        }

        /// <summary>새 주기의 여관 소문을 장 사람들도 듣는다. 말하는 변화의 일부가 기대로 먼저 붙는다.</summary>
        private void SpreadTavernTalk(int cycle)
        {
            var turn = BandCodex.TurnOf(cycle);

            for (var i = 0; i < StockOrder.Count; i++)
            {
                var stock = StockOrder[i];

                if (stock.IsTradable == false || stock.CodexProfile < 0)
                {
                    continue;
                }

                stock.AddExpectation(Constants.CODEX_TAVERN_EFFECT[stock.CodexProfile][turn]
                    * Constants.CODEX_TAVERN_CROWD_SHARE);
            }
        }

        /// <summary>
        /// 새 용병단이 영지에 도착한다. 다음 주기에 상장된다 — 그 사이가 상장 전 조사의 시간이다.
        /// 상장됐거나 도착한 단이 너무 많으면 덜 부른다.
        /// </summary>
        private void GenerateArrivals()
        {
            var present = 0;
            var taken = new List<string>();

            for (var i = 0; i < StockOrder.Count; i++)
            {
                taken.Add(StockOrder[i].DisplayName);

                if (StockOrder[i].IsTradable == true && StockOrder[i].Personality != StockPersonality.Personality_Index)
                {
                    present++;
                }
            }

            for (var i = 0; i < Waiting.Count; i++)
            {
                taken.Add(Waiting[i].Stock.DisplayName);
                present++;
            }

            var count = Math.Min(Constants.BAND_ARRIVALS_PER_CYCLE, Constants.BAND_PRESENT_MAX - present);

            for (var i = 0; i < count; i++)
            {
                BandSerial++;
                var spec = BandFactory.Roll(BandRng, BandSerial, Clock.Cycle + 1, taken);
                taken.Add(spec.Name);
                AddGenerated(spec);
            }
        }

        private void AddGenerated(GeneratedBandState spec)
        {
            Generated.Add(spec);
            AddWaiting(BandFactory.Build(spec), spec.ListCycle);
            PlanDeparture(spec.Id, spec.DelistCycle, spec.DelistReason);
        }

        /// <summary>그 종목이 쓰는 도감 문장 묶음. 없으면 -1.</summary>
        public int CodexProfileOf(string stockId)
        {
            AdventurerStock stock;

            if (StockTable.TryGetValue(stockId, out stock) == true)
            {
                return stock.CodexProfile;
            }

            for (var i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Stock.Id == stockId)
                {
                    return Waiting[i].Stock.CodexProfile;
                }
            }

            return -1;
        }

        /// <summary>
        /// 도감에 오르는 단 — 영지에 도착한 단을 먼저, 그다음 상장된 단. 떠난 단과 용사단은 빠진다.
        /// </summary>
        public List<string> CodexSubjects()
        {
            var list = new List<string>();

            for (var i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Cycle <= Clock.Cycle + 1 && Waiting[i].Stock.CodexProfile >= 0)
                {
                    list.Add(Waiting[i].Stock.Id);
                }
            }

            for (var i = 0; i < StockOrder.Count; i++)
            {
                if (StockOrder[i].IsTradable == true && StockOrder[i].CodexProfile >= 0)
                {
                    list.Add(StockOrder[i].Id);
                }
            }

            return list;
        }

        private void AddCodexOutcome(string stockId, string headline, InfoSource source, bool noted)
        {
            if (noted == true)
            {
                headline = StringConstants.TEXT_CODEX_NOTED + headline;
            }

            InfoEntries.Add(new InfoEntry(Clock.Tick, stockId, headline, source, EventStage.Stage_Outcome, true,
                Clock.Day, Clock.Session));
            HasPendingAlert = true;
        }

        /// <summary>
        /// 지수형 종목의 내재가치를 장마다 조금 올린다. 용사단은 3년째 영지의 의뢰를 다 해내고 있다.
        /// 가격은 이 값을 향해 천천히 따라간다. 놀라움이 없는 종목이다(설계안 §1-4).
        /// </summary>
        private void GrowIndexStocks()
        {
            for (var i = 0; i < StockOrder.Count; i++)
            {
                var stock = StockOrder[i];

                if (stock.IsTradable == true && stock.Personality == StockPersonality.Personality_Index)
                {
                    stock.ResolveOutcome(Constants.HERO_INDEX_GROWTH_PER_SESSION);
                }
            }
        }

        /// <summary>
        /// 용사단이 사정을 놓쳐 누군가 다치거나 죽었다는 소식이 지하에 닿았다. 용사단 종목이 그 자리에서 떨어진다.
        /// 지수는 안전한 것이 아니라 플레이어가 안전하게 유지하는 것이다(설계안 §1-5). 상장 폐지되지는 않는다 — 단은 남는다.
        /// 종목이 없으면 null, 아니면 알릴 문구를 돌려준다.
        /// </summary>
        public string ShockIndex(string stockId, float change)
        {
            AdventurerStock stock;

            if (StockTable.TryGetValue(stockId, out stock) == false || stock.IsTradable == false)
            {
                return null;
            }

            var before = stock.Price;
            stock.ResolveOutcome(change);
            stock.SetPrice(stock.TargetPrice);
            stock.RecordHistory();
            return string.Format(Josa.Format(StringConstants.TEXT_HERO_STOCK_SHOCK, stock.DisplayName), stock.DisplayName,
                before, stock.Price);
        }

        /// <summary>장이 끝났을 때 가진 돈과 종목 값을 합쳐 파산선 아래인가.</summary>
        public bool IsBankrupt
        {
            get
            {
                return Wallet.TotalValue(StockLookup) < Constants.BANKRUPT_THRESHOLD;
            }
        }

        /// <summary>정보상의 재기 자금. 첫 파산에 한 번 받는다.</summary>
        public void Rescue(float cash)
        {
            Wallet.Earn(cash);
        }

        /// <summary>
        /// 그 용병단이 생애의 어디에 있는가. 상장 한 주기 전에 영지에 도착한다(상장흐름 설계안 §3).
        /// 도착부터 상장까지가 남보다 먼저 볼 수 있는 시간이다.
        /// </summary>
        public BandStage StageOf(string stockId)
        {
            AdventurerStock stock;

            if (StockTable.TryGetValue(stockId, out stock) == true)
            {
                return stock.IsTradable == true ? BandStage.Band_Listed : BandStage.Band_Left;
            }

            for (var i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Stock.Id == stockId)
                {
                    return Waiting[i].Cycle <= Clock.Cycle + 1 ? BandStage.Band_Arrived : BandStage.Band_NotArrived;
                }
            }

            return BandStage.Band_NotArrived;
        }

        /// <summary>종목의 이름. 아직 상장 전인 단도 찾는다. 모르면 아이디를 돌려준다.</summary>
        public string NameOf(string stockId)
        {
            AdventurerStock stock;

            if (StockTable.TryGetValue(stockId, out stock) == true)
            {
                return stock.DisplayName;
            }

            for (var i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Stock.Id == stockId)
                {
                    return Waiting[i].Stock.DisplayName;
                }
            }

            return stockId;
        }

        /// <summary>도착했지만 아직 상장되지 않은 단의 이름. 없으면 빈 목록.</summary>
        public List<string> ArrivedNames()
        {
            var names = new List<string>();

            for (var i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Cycle <= Clock.Cycle + 1)
                {
                    names.Add(Waiting[i].Stock.DisplayName);
                }
            }

            return names;
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
            // 다음 주기에 상장될 단이 영지에 도착했다. 상장 전 조사의 시간이 시작된다(설계안 §3).
            for (var i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Cycle == Clock.Cycle + 1)
                {
                    var name = Waiting[i].Stock.DisplayName;
                    Notices.Add(string.Format(Josa.Format(StringConstants.TEXT_LISTING_ARRIVED, name), name,
                        Waiting[i].Stock.Price));
                }
            }

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
        /// <summary>
        /// 저장할 값. 장 사이에만 부른다 — 대기 주문·정보 타임 연출은 담지 않는다.
        /// 사건 일정·대기열·이탈 계획은 표에서 다시 짜므로 어디까지 왔는지만 적는다.
        /// </summary>
        internal MarketState Capture()
        {
            var state = new MarketState
            {
                Seed = Seed,
                Sentiment = Sentiment,
                NextEventIndex = NextEventIndex,
                InfoTimesOpened = InfoTimesOpened,
                NextSlot = NextSlot,
                BriefingOrder = BriefingOrder,
                Wallet = Wallet.Capture(),
                BandSerial = BandSerial
            };

            state.Generated.AddRange(Generated);

            for (var i = 0; i < StockOrder.Count; i++)
            {
                state.Stocks.Add(StockOrder[i].Capture());
            }

            for (var i = 0; i < InfoEntries.Count; i++)
            {
                var entry = InfoEntries[i];
                state.Info.Add(new InfoState
                {
                    Tick = entry.Tick,
                    StockId = entry.StockId,
                    Headline = entry.Headline,
                    Source = entry.Source,
                    Stage = entry.Stage,
                    IsPublic = entry.IsPublic,
                    Day = entry.Day,
                    Session = entry.Session
                });
            }

            return state;
        }

        /// <summary>
        /// 표에서 새로 짠 판에 저장한 값을 덮어쓴다. 저장 때 상장돼 있던 대기열의 단은 상장시킨다.
        /// 저장에는 있는데 표에 없는 종목은 버린다(표가 바뀐 경우).
        /// </summary>
        internal void Restore(MarketState state, MarketClock clock)
        {
            Clock = clock;
            Sentiment = state.Sentiment;
            NextEventIndex = state.NextEventIndex;
            InfoTimesOpened = state.InfoTimesOpened;
            NextSlot = state.NextSlot;
            BriefingOrder = state.BriefingOrder;
            HasBriefing = false;
            Briefing.Clear();
            Notices.Clear();
            Wallet.Restore(state.Wallet);

            // 지었던 새 단을 먼저 다시 세운다. 상장돼 있던 단은 아래에서 대기열에서 꺼내 올린다.
            BandSerial = state.BandSerial;

            for (var i = 0; i < state.Generated.Count; i++)
            {
                AddGenerated(state.Generated[i]);
            }

            for (var i = 0; i < state.Stocks.Count; i++)
            {
                var row = state.Stocks[i];
                AdventurerStock stock;

                if (StockTable.TryGetValue(row.Id, out stock) == false)
                {
                    stock = TakeWaiting(row.Id);

                    if (stock == null)
                    {
                        continue;
                    }

                    AddStock(stock);
                }

                stock.Restore(row);
            }

            InfoEntries.Clear();

            for (var i = 0; i < state.Info.Count; i++)
            {
                var row = state.Info[i];
                InfoEntries.Add(new InfoEntry(row.Tick, row.StockId, row.Headline, row.Source, row.Stage, row.IsPublic,
                    row.Day, row.Session));
            }

            // 같은 씨앗이면 불러올 때마다 같은 흐름이 되풀이된다. 지나온 장 수를 섞는다.
            var salt = clock.Cycle * 7919 + clock.Day * 104729 + clock.Tick;
            Rng = new Random(Seed ^ salt);
            MicroRng = new Random((Seed ^ 0x5F3759DF) ^ salt);
            BandRng = new Random((Seed ^ 0x2545F491) ^ salt);
        }

        private AdventurerStock TakeWaiting(string stockId)
        {
            for (var i = 0; i < Waiting.Count; i++)
            {
                if (Waiting[i].Stock.Id == stockId)
                {
                    var stock = Waiting[i].Stock;
                    Waiting.RemoveAt(i);
                    return stock;
                }
            }

            return null;
        }

        /// <summary>영지에서 먼저 흐르던 시계를 이어받는다. 시장을 처음 만들 때 한 번만 부른다.</summary>
        internal void AdoptClock(MarketClock clock)
        {
            Clock = clock;
        }

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

        /// <summary>다음 장을 연다. 오전장 다음은 오후장, 오후장 다음은 이튿날 오전장 — 계절 첫날이면 그날 오후장이다.</summary>
        public void OpenNextSession(bool isInspectionDay)
        {
            int nextDay;
            var next = MarketClock.NextSessionAfter(Clock.Session, Clock.Day, out nextDay);

            if (nextDay != Clock.Day)
            {
                Clock.AdvanceDay();
            }

            Notices.Clear();
            JustReopened = false;
            ReopenGapUp = 0;
            ReopenGapDown = 0;

            // 휴장 동안 거래되던 종목. 재개장에 새로 상장하는 단은 갭에서 뺀다 — 공모가에 사는 것이 상장 전에 캔 사람의 몫이다.
            var tradedBefore = new List<AdventurerStock>();

            if (Clock.AdvanceCycleForReopening(Clock.Day) == true)
            {
                for (var i = 0; i < StockOrder.Count; i++)
                {
                    if (StockOrder[i].IsTradable == true)
                    {
                        tradedBefore.Add(StockOrder[i]);
                    }
                }

                RollSeason();
                JustReopened = true;
            }

            // 달력의 시찰일이면 닫는다. 인자는 시험용으로 강제로 닫을 때 쓴다.
            var halted = isInspectionDay == true || MarketClock.IsInspectionDay(Clock.Day);
            OpenSession(halted, next);

            if (JustReopened == true && halted == false)
            {
                ApplyReopeningGap(tradedBefore);
            }
        }

        /// <summary>
        /// 재개장 갭. 원정 결과가 알려진 만큼 장이 열리자마자 이미 뛰었거나 떨어져 있다(설계안 §2).
        /// 목표가까지의 거리 가운데 <see cref="Constants.REOPEN_GAP_SHARE"/>만 한 번에 건너뛰고, 나머지는 장중에 따라간다.
        /// 휴장 중에는 주문을 받지 않으므로 이 갭은 피할 수 없다 — 출발 밤에 들고 가느냐가 판단이다.
        /// </summary>
        private void ApplyReopeningGap(List<AdventurerStock> stocks)
        {
            for (var i = 0; i < stocks.Count; i++)
            {
                var stock = stocks[i];

                // 상장 폐지된 단은 이미 청산됐다.
                if (stock.IsTradable == false)
                {
                    continue;
                }

                var next = stock.Price + (stock.TargetPrice - stock.Price) * Constants.REOPEN_GAP_SHARE;

                if (next > stock.Price * (1f + Constants.REOPEN_GAP_MIN_RATIO))
                {
                    ReopenGapUp++;
                }
                else if (next < stock.Price * (1f - Constants.REOPEN_GAP_MIN_RATIO))
                {
                    ReopenGapDown++;
                }

                stock.ApplyOpeningGap(next);
            }
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

            if (InformantTips > 0)
            {
                InformantTips--;
                AddInformantTip();
            }

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

        // 이번 장에서 정보상이 더 해 줄 귀띔(계절 계획 「지하 사람들과 어울린다」). 장 밖에서는 저장하지 않는다 — 재개장 장에서 받아 그 장에 쓴다.
        private int InformantTips;

        /// <summary>정보상이 이번 장의 정보 타임마다 귀띔을 하나씩 더 준다. 정보 타임이 남은 만큼만 쓰인다.</summary>
        public void GrantInformantTips(int count)
        {
            InformantTips += count;
        }

        /// <summary>
        /// 원정 결과를 아직 덜 따라간 종목 하나를 짚는다. 목표가와의 거리가 가장 큰 단이다.
        /// 용사단(지수형)은 놀라움이 없으니 뺀다.
        /// </summary>
        private void AddInformantTip()
        {
            AdventurerStock best = null;
            var bestGap = 0f;

            for (var i = 0; i < StockOrder.Count; i++)
            {
                var stock = StockOrder[i];

                if (stock.IsTradable == false || stock.Personality == StockPersonality.Personality_Index
                    || stock.Price <= 0f)
                {
                    continue;
                }

                var gap = Math.Abs(stock.TargetPrice - stock.Price) / stock.Price;

                if (gap > bestGap)
                {
                    bestGap = gap;
                    best = stock;
                }
            }

            if (best == null)
            {
                return;
            }

            var format = best.TargetPrice > best.Price
                ? StringConstants.TEXT_INFORMANT_TIP_UP
                : StringConstants.TEXT_INFORMANT_TIP_DOWN;
            InfoEntries.Add(new InfoEntry(Clock.Tick, best.Id, string.Format(format, best.DisplayName),
                InfoSource.Source_BrokerLeak, EventStage.Stage_Report, false, Clock.Day, Clock.Session));
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
                    Clock.Tick, step.StockId, step.Headline, step.Source, step.Stage, step.AffectsMarket,
                    Clock.Day, Clock.Session));
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

            float fill;
            float lastPrice;
            OrderBook.Sweep(stock, MicroSeq, side, quantity, out fill, out lastPrice);

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

            // 현재가는 마지막으로 먹은 호가가 된다. 보이는 호가창 밖으로 튀지 않는다.
            stock.SetPrice(PriceModel.ClampMove(stock.Price, lastPrice));

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
