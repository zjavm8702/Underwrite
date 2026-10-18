using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;
using Underwrite.Save;

namespace Underwrite.Market
{
    /// <summary>
    /// 거래 대상 한 명의 모험가. 기획서 §6에 따라 거래 단위는 개인이다.
    ///
    /// 세 값을 분리해서 들고 있는 것이 핵심이다. 기획서 §8의 세 가지 정보 상태를 그대로 옮겼다.
    /// - <see cref="TrueValue"/>: 실제로 일어난 사건이 만든 내재가치. 플레이어에게 직접 보여주지 않는다.
    /// - <see cref="MarketExpectation"/>: 시장에 공개되어 가격에 이미 반영된 기대. 양수도 음수도 된다.
    /// - <see cref="Price"/>: 실제 거래되는 가격. 위 둘과 분위기·유동성·주문이 함께 만든다.
    ///
    /// 좋은 소식이 이미 기대에 반영되어 있으면 결과가 확정될 때 상승이 작다. 그 구조를 여기서 만든다.
    /// </summary>
    public sealed class AdventurerStock
    {
        private readonly List<float> History = new List<float>(Constants.PRICE_HISTORY_CAPACITY);

        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public StockPersonality Personality { get; private set; }

        /// <summary>실제 내재가치. 사건의 결과가 확정될 때만 움직인다.</summary>
        public float TrueValue { get; private set; }

        /// <summary>시장이 이미 가격에 반영한 기대. 1.0이면 내재가치의 두 배를 기대하는 상태다.</summary>
        public float MarketExpectation { get; private set; }

        /// <summary>현재 거래 가격.</summary>
        public float Price { get; private set; }

        /// <summary>유동성. 작을수록 같은 수량의 주문이 가격을 크게 민다. 기획서 §7.</summary>
        public float Liquidity { get; private set; }

        /// <summary>이 종목이 살아 있는지. 사망·은퇴·영지 이동 시 거래가 멈춘다.</summary>
        public bool IsTradable { get; private set; } = true;

        /// <summary>떠난 사유. 아직 살아 있으면 None이다.</summary>
        public DelistReason LeftBecause { get; private set; } = DelistReason.Delist_None;

        /// <summary>
        /// 떠나기로 공고된 주기. 0이면 공고가 없다.
        /// 예고 없는 전멸과 예고 있는 은퇴를 가르는 것이 설계안 §5의 핵심이다.
        /// </summary>
        public int NoticedDelistCycle { get; private set; }

        /// <summary>떠날 때 돌려받는 비율. 상장 시점에 공개한다. 설계안 §5.</summary>
        public float LiquidationRatio
        {
            get
            {
                return LiquidationRatioOf(PlannedReason);
            }
        }

        /// <summary>떠나기로 되어 있는 사유. 공고 전에도 청산율을 보여 주기 위해 들고 있는다.</summary>
        public DelistReason PlannedReason { get; private set; } = DelistReason.Delist_Retired;

        public static float LiquidationRatioOf(DelistReason reason)
        {
            switch (reason)
            {
                case DelistReason.Delist_Wiped:
                {
                    return Constants.LIQUIDATION_WIPED;
                }
                case DelistReason.Delist_Moved:
                {
                    return Constants.LIQUIDATION_MOVED;
                }
                default:
                {
                    return Constants.LIQUIDATION_RETIRED;
                }
            }
        }

        internal void PlanDeparture(DelistReason reason)
        {
            PlannedReason = reason;
        }

        /// <summary>떠난다고 공고한다. 플레이어는 이때부터 팔 수 있다.</summary>
        internal void NoticeDeparture(int cycle)
        {
            NoticedDelistCycle = cycle;
        }

        /// <summary>시장을 떠난다. 거래가 멈추고 남은 지분은 청산된다.</summary>
        internal void Leave(DelistReason reason)
        {
            LeftBecause = reason;
            PlannedReason = reason;
            IsTradable = false;
        }

        /// <summary>장 시작 시점의 가격. 등락률 표시에 쓴다.</summary>
        public float SessionOpenPrice { get; private set; }

        /// <summary>
        /// 직전 틱들이 남긴 흐름. 매 틱 독립인 잡음만 쓰면 가격이 한 수준에서 맴돈다.
        /// 흐름을 이어 두면 떨어지던 종목이 더 떨어질 수도 있어 되돌림을 노린 매매가 공짜가 아니게 된다.
        /// </summary>
        public float Momentum { get; private set; }

        /// <summary>
        /// 틱 사이의 미세 호가 흔들림. 틱 종가 대비 비율이다.
        /// 실제 호가창은 틱 단위로 멈춰 있지 않고 쉬지 않고 흔들린다.
        /// </summary>
        public float MicroOffset { get; private set; }

        /// <summary>
        /// 지금 이 순간의 호가. 틱 종가에 미세 흔들림을 더한 값이다.
        /// 체결가와 지정가 판정은 모두 이 값을 본다. 차트는 틱 종가를 그대로 쓴다.
        /// </summary>
        public float LivePrice
        {
            get
            {
                var live = Price * (1f + MicroOffset);

                if (live < Constants.PRICE_FLOOR)
                {
                    return Constants.PRICE_FLOOR;
                }

                return live;
            }
        }

        public AdventurerStock(string id, string displayName, StockPersonality personality,
            float trueValue, float liquidity)
        {
            if (string.IsNullOrWhiteSpace(id) == true)
            {
                throw new ArgumentException("Stock id must not be empty.", nameof(id));
            }

            if (trueValue <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(trueValue), "True value must be positive.");
            }

            if (liquidity <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(liquidity), "Liquidity must be positive.");
            }

            Id = id;
            DisplayName = displayName;
            Personality = personality;
            TrueValue = trueValue;
            MarketExpectation = 0f;
            Price = trueValue;
            SessionOpenPrice = trueValue;
            Liquidity = liquidity;
            History.Add(trueValue);
        }

        /// <summary>종목 성격이 주는 변동성 배율. 기획서 §7의 종목별 성격.</summary>
        public float VolatilityMultiplier
        {
            get
            {
                switch (Personality)
                {
                    case StockPersonality.Personality_Veteran:
                    {
                        return Constants.VOLATILITY_VETERAN;
                    }
                    case StockPersonality.Personality_Rookie:
                    {
                        return Constants.VOLATILITY_ROOKIE;
                    }
                    case StockPersonality.Personality_HighRisk:
                    {
                        return Constants.VOLATILITY_HIGH_RISK;
                    }
                    case StockPersonality.Personality_Index:
                    {
                        return Constants.VOLATILITY_INDEX;
                    }
                    default:
                    {
                        return Constants.VOLATILITY_FAMOUS;
                    }
                }
            }
        }

        /// <summary>내재가치와 시장 기대가 만드는 목표가. 가격은 이 값을 향해 수렴한다.</summary>
        public float TargetPrice
        {
            get
            {
                return TrueValue * (1f + MarketExpectation);
            }
        }

        /// <summary>장 시작 대비 등락률. 0.05면 5퍼센트 상승이다.</summary>
        public float SessionChangeRatio
        {
            get
            {
                if (SessionOpenPrice <= 0f)
                {
                    return 0f;
                }

                // 화면에 보이는 가격과 같은 값으로 재야 한다. 틱 종가로 재면
                // 큰 숫자는 54.2인데 등락률은 53.7 기준이 되어 둘 중 하나가 거짓말이 된다.
                return (LivePrice - SessionOpenPrice) / SessionOpenPrice;
            }
        }

        public IReadOnlyList<float> PriceHistory
        {
            get
            {
                return History;
            }
        }

        internal void SetPrice(float next)
        {
            if (next < Constants.PRICE_FLOOR)
            {
                next = Constants.PRICE_FLOOR;
            }

            Price = next;
        }

        /// <summary>
        /// 장이 열리자마자 값이 뛰어 있다(재개장 갭). 장 시작가는 지난 종가로 두어 등락률에 갭이 보이게 하고,
        /// 차트는 뛴 자리에서 시작한다. <see cref="BeginSession"/> 뒤에 부른다.
        /// </summary>
        internal void ApplyOpeningGap(float next)
        {
            SetPrice(next);
            History.Clear();
            History.Add(Price);
        }

        internal void RecordHistory()
        {
            History.Add(Price);

            if (History.Count > Constants.PRICE_HISTORY_CAPACITY)
            {
                History.RemoveAt(0);
            }
        }

        internal void SetMomentum(float value)
        {
            Momentum = value;
        }

        internal void SetMicroOffset(float value)
        {
            MicroOffset = value;
        }

        internal void BeginSession()
        {
            SessionOpenPrice = Price;
            MicroOffset = 0f;

            // 차트는 "한 장"만 보여 준다. 지난 장의 점을 남겨 두면
            // 틱 수보다 점이 많아져 선이 차트 밖으로 흘러나간다.
            History.Clear();
            History.Add(Price);
        }

        /// <summary>소문이 기대를 움직인다. 내재가치는 건드리지 않는다.</summary>
        internal void AddExpectation(float delta)
        {
            MarketExpectation += delta;
        }

        /// <summary>기대가 확정되지 않은 채로 시간이 지나면 식는다.</summary>
        internal void DecayExpectation(float rate)
        {
            MarketExpectation -= MarketExpectation * rate;
        }

        /// <summary>
        /// 결과가 확정되어 실제 가치가 바뀐다. 기대는 사라지고 현실이 그 자리를 차지한다.
        /// 이미 기대에 반영돼 있던 만큼은 가격이 더 오르지 않는다.
        /// </summary>
        internal void ResolveOutcome(float valueDelta)
        {
            TrueValue *= 1f + valueDelta;

            if (TrueValue < Constants.PRICE_FLOOR)
            {
                TrueValue = Constants.PRICE_FLOOR;
            }

            MarketExpectation = 0f;
        }

        /// <summary>
        /// 후원이 실제 전망을 올린다. 기대는 건드리지 않으므로 가격이 즉시 뛰지 않고
        /// 다음 틱부터 목표가를 향해 서서히 따라온다. 기획서 §9의 "효과에는 시간이 걸린다".
        /// </summary>
        internal void ResolvePatronage(float valueGain)
        {
            TrueValue *= 1f + valueGain;

            if (TrueValue < Constants.PRICE_FLOOR)
            {
                TrueValue = Constants.PRICE_FLOOR;
            }
        }

        /// <summary>
        /// 시장에 돈이 들어와 가치와 호가 깊이가 함께 불어난다(거품). 기대는 그대로 — 가격은 목표가를 따라 올라간다.
        /// 깊이도 같이 늘어야 같은 크기의 주문이 같은 만큼만 값을 민다.
        /// </summary>
        internal void Inflate(float rate)
        {
            TrueValue *= 1f + rate;
            Liquidity *= 1f + rate;

            if (TrueValue < Constants.PRICE_FLOOR)
            {
                TrueValue = Constants.PRICE_FLOOR;
            }

            if (Liquidity < 1f)
            {
                Liquidity = 1f;
            }
        }

        /// <summary>유동성이 바뀐다. 거래가 늘면 커지고 줄면 작아진다.</summary>
        internal void AdjustLiquidity(float delta)
        {
            Liquidity += delta;

            if (Liquidity < 1f)
            {
                Liquidity = 1f;
            }
        }

        /// <summary>사망·은퇴·영지 이동으로 거래가 멈춘다. 청산 규칙은 시제품 범위 밖이다.</summary>
        internal void StopTrading()
        {
            IsTradable = false;
        }

        /// <summary>
        /// 도감 문장 묶음 번호(<see cref="Global.StringConstants.CODEX_FIXED"/> 등의 첫 번째 칸). 없으면 -1.
        /// 처음 다섯 단은 제 묶음을 쓰고, 주기마다 새로 오는 단은 유형이 같은 묶음을 돌려 쓴다.
        /// </summary>
        public int CodexProfile { get; private set; } = -1;

        internal void SetCodexProfile(int profile)
        {
            CodexProfile = profile;
        }

        internal StockState Capture()
        {
            var state = new StockState
            {
                Id = Id,
                TrueValue = TrueValue,
                MarketExpectation = MarketExpectation,
                Price = Price,
                IsTradable = IsTradable,
                LeftBecause = LeftBecause,
                NoticedDelistCycle = NoticedDelistCycle,
                PlannedReason = PlannedReason,
                SessionOpenPrice = SessionOpenPrice,
                Momentum = Momentum
            };

            state.History.AddRange(History);
            return state;
        }

        /// <summary>저장한 값을 덮어쓴다. 이름·성격·유동성은 표에서 오므로 건드리지 않는다.</summary>
        internal void Restore(StockState state)
        {
            TrueValue = state.TrueValue;
            MarketExpectation = state.MarketExpectation;
            Price = state.Price;
            IsTradable = state.IsTradable;
            LeftBecause = state.LeftBecause;
            NoticedDelistCycle = state.NoticedDelistCycle;
            PlannedReason = state.PlannedReason;
            SessionOpenPrice = state.SessionOpenPrice;
            Momentum = state.Momentum;
            MicroOffset = 0f;
            History.Clear();
            History.AddRange(state.History);
        }
    }
}
