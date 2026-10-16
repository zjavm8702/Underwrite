using System;
using Underwrite.Enumeration;
using Underwrite.Global;

namespace Underwrite.Market
{
    /// <summary>
    /// 틱 단위 가격 계산. 기획서 §7의 여섯 요인을 그대로 옮겼다.
    ///
    /// | 요인 | 구현 |
    /// |---|---|
    /// | 성과와 위험 | 사건의 결과가 바꾼 <see cref="AdventurerStock.TrueValue"/> |
    /// | 기대와 소문 | <see cref="AdventurerStock.MarketExpectation"/>이 만든 목표가 |
    /// | 시장 분위기 | <see cref="SentimentDrift"/>와 <see cref="SentimentVolatility"/> |
    /// | 거래량과 유동성 | <see cref="OrderImpact"/>가 유동성으로 나눈 충격 |
    /// | 이해관계자의 행동 | 소문 유포·대량 매도 사건이 기대와 유동성을 움직인다 |
    /// | 플레이어의 개입 | 후원이 내재가치를 올리고, 주문이 가격을 민다 |
    ///
    /// 무작위가 전부가 아니다. 사건 뒤에 플레이어가 원인을 되짚을 수 있어야 한다.
    /// </summary>
    public static class PriceModel
    {
        /// <summary>
        /// 한 틱의 가격을 계산한다. 엔진에 의존하지 않으므로 테스트에서 그대로 호출할 수 있다.
        /// </summary>
        public static float NextPrice(AdventurerStock stock, MarketSentiment sentiment, float gaussian)
        {
            if (stock == null)
            {
                throw new ArgumentNullException(nameof(stock));
            }

            var price = stock.Price;

            // 1. 목표가를 향한 수렴. 내재가치와 시장 기대가 목표를 만든다.
            //    내려가는 쪽이 더 빠르다. 기대가 꺾였을 때 가파르게 무너져야 공포가 체감된다.
            var gap = stock.TargetPrice - price;
            var reversion = gap < 0f
                ? Constants.PRICE_REVERSION_DOWN_PER_TICK
                : Constants.PRICE_REVERSION_PER_TICK;
            var pull = gap * reversion;

            // 2. 시장 분위기가 모든 종목에 공통으로 거는 흐름.
            var drift = price * SentimentDrift(sentiment);

            // 3. 무작위 변동. 종목 성격과 분위기가 폭을 키운다.
            var sigma = Constants.PRICE_NOISE_BASE * stock.VolatilityMultiplier * SentimentVolatility(sentiment);
            var noise = price * sigma * gaussian;

            var next = price + pull + drift + noise;

            return ClampMove(price, next);
        }

        /// <summary>
        /// 주문이 가격을 미는 정도. 유동성이 작은 무명 종목은 적은 자금으로도 크게 흔들린다.
        /// 매수는 올리고 매도는 내린다.
        /// </summary>
        public static float OrderImpact(AdventurerStock stock, OrderSide side, int quantity)
        {
            if (stock == null)
            {
                throw new ArgumentNullException(nameof(stock));
            }

            if (quantity <= 0)
            {
                return 0f;
            }

            var ratio = quantity / stock.Liquidity;
            var impact = stock.Price * Constants.ORDER_IMPACT_SCALE * ratio;

            if (side == OrderSide.Side_Sell)
            {
                return -impact;
            }

            return impact;
        }

        /// <summary>분위기가 거는 공통 흐름. 열풍은 올리고 공포는 내린다.</summary>
        public static float SentimentDrift(MarketSentiment sentiment)
        {
            switch (sentiment)
            {
                case MarketSentiment.Sentiment_Fever:
                {
                    return Constants.SENTIMENT_DRIFT_FEVER;
                }
                case MarketSentiment.Sentiment_Fear:
                {
                    return Constants.SENTIMENT_DRIFT_FEAR;
                }
                case MarketSentiment.Sentiment_Drained:
                {
                    return Constants.SENTIMENT_DRIFT_DRAINED;
                }
                default:
                {
                    return Constants.SENTIMENT_DRIFT_CALM;
                }
            }
        }

        /// <summary>분위기가 거는 변동성 배율. 공포 장세가 가장 거칠다.</summary>
        public static float SentimentVolatility(MarketSentiment sentiment)
        {
            switch (sentiment)
            {
                case MarketSentiment.Sentiment_Fever:
                {
                    return Constants.SENTIMENT_VOLATILITY_FEVER;
                }
                case MarketSentiment.Sentiment_Fear:
                {
                    return Constants.SENTIMENT_VOLATILITY_FEAR;
                }
                case MarketSentiment.Sentiment_Drained:
                {
                    return Constants.SENTIMENT_VOLATILITY_DRAINED;
                }
                default:
                {
                    return Constants.SENTIMENT_VOLATILITY_CALM;
                }
            }
        }

        /// <summary>
        /// 한 틱의 변동을 상한으로 묶는다. 급등락은 허용하되 폭주는 막는다.
        /// 상한은 비대칭이다. 무너지는 속도가 오르는 속도보다 빠르다.
        /// </summary>
        public static float ClampMove(float previous, float next)
        {
            var riseLimit = previous * Constants.PRICE_MAX_MOVE_PER_TICK;
            var dropLimit = previous * Constants.PRICE_MAX_DROP_PER_TICK;
            var delta = next - previous;

            if (delta > riseLimit)
            {
                delta = riseLimit;
            }
            else if (delta < -dropLimit)
            {
                delta = -dropLimit;
            }

            var result = previous + delta;

            if (result < Constants.PRICE_FLOOR)
            {
                return Constants.PRICE_FLOOR;
            }

            return result;
        }

        /// <summary>
        /// 평균 0, 표준편차 1의 정규분포 표본. Box-Muller 변환을 쓴다.
        /// 시드를 넘긴 <see cref="Random"/>을 쓰므로 같은 시드면 같은 장세가 재현된다.
        /// </summary>
        public static float Gaussian(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            // 로그에 0이 들어가지 않도록 하한을 둔다.
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();

            var value = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return (float)value;
        }
    }
}
