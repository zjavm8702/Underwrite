using System;
using System.Collections.Generic;
using Underwrite.Enumeration;
using Underwrite.Global;

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

        /// <summary>장 시작 시점의 가격. 등락률 표시에 쓴다.</summary>
        public float SessionOpenPrice { get; private set; }

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

                return (Price - SessionOpenPrice) / SessionOpenPrice;
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

        internal void RecordHistory()
        {
            History.Add(Price);

            if (History.Count > Constants.PRICE_HISTORY_CAPACITY)
            {
                History.RemoveAt(0);
            }
        }

        internal void BeginSession()
        {
            SessionOpenPrice = Price;
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
    }
}
