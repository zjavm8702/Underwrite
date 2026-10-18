using Underwrite.Global;
using UnityEngine;

namespace Underwrite.Data
{
    /// <summary>
    /// 밸런스를 보면서 고치는 시장 수치. 표 한 장이 자산 하나가 된다.
    ///
    /// 구조와 안전장치는 여기 두지 않는다. <see cref="Constants.PRICE_FLOOR"/>나
    /// <see cref="Constants.BOOK_LEVELS"/>가 0이나 음수가 되면 시뮬레이션이 깨지는데,
    /// 인스펙터에는 그걸 막을 방법이 없다. 그런 값은 코드에 남긴다.
    /// </summary>
    [CreateAssetMenu(fileName = "MarketTuningData", menuName = "Underwrite/시장 수치")]
    public sealed class MarketTuningData : ScriptableObject
    {
        [Header("시간")]
        [Tooltip("실시간 1초가 흐르는 게임 내 분. 0.6667이면 한 틱이 7.5초, 한 장이 약 9분이다.")]
        [SerializeField] private float MinutesPerRealSecond = 0.6667f;

        [Header("가격 흐름")]
        [Tooltip("목표가를 향해 올라갈 때의 수렴 속도.")]
        [SerializeField] private float ReversionPerTick = 0.02f;

        [Tooltip("내려갈 때의 수렴 속도. 무너지는 쪽이 더 빠르다.")]
        [SerializeField] private float ReversionDownPerTick = 0.035f;

        [Tooltip("틱마다 더하는 무작위 변동의 기본 폭.")]
        [SerializeField] private float NoiseBase = 0.0032f;

        [Tooltip("직전 틱의 흐름이 다음 틱에 남는 비율. 0이면 가격이 한 수준에서 맴돈다.")]
        [SerializeField] private float MomentumRetention = 0.62f;

        [Tooltip("기대가 실제 가치로 식어가는 속도.")]
        [SerializeField] private float ExpectationDecayPerTick = 0.02f;

        [Tooltip("한 틱에 허용하는 최대 상승률.")]
        [SerializeField] private float MaxMovePerTick = 0.18f;

        [Tooltip("한 틱에 허용하는 최대 하락률. 상승 상한보다 크다.")]
        [SerializeField] private float MaxDropPerTick = 0.30f;

        [Header("미세 호가")]
        [Tooltip("미세 호가를 다시 뽑는 간격(실시간 초).")]
        [SerializeField] private float MicroSeconds = 0.12f;

        [Tooltip("미세 흔들림 한 번의 크기. 틱 종가 대비 비율이다.")]
        [SerializeField] private float MicroAmplitude = 0.0013f;

        [Tooltip("미세 흔들림이 남는 비율.")]
        [SerializeField] private float MicroRetention = 0.72f;

        [Tooltip("틱 종가에서 미세 흔들림이 멀어질 수 있는 한계.")]
        [SerializeField] private float MicroLimit = 0.007f;

        [Header("호가와 체결")]
        [Tooltip("주문 한 주가 유동성 대비 가격을 밀어내는 정도.")]
        [SerializeField] private float OrderImpactScale = 0.9f;

        [Tooltip("호가 단위를 가격의 몇 배로 잡을지.")]
        [SerializeField] private float BookTickRatio = 0.004f;

        [Tooltip("호가 한 단계에 쌓이는 수량의 기본 비율.")]
        [SerializeField] private float BookDepthBase = 0.35f;

        [Tooltip("호가가 멀어질수록 두꺼워지는 정도.")]
        [SerializeField] private float BookDepthGrowth = 0.45f;

        [Header("플레이어")]
        [Tooltip("시작 자금.")]
        [SerializeField] private float StartingCash = 500f;

        /// <summary>표의 값을 코드 쪽 수치에 덮어쓴다. 장을 열기 전에 한 번 부른다.</summary>
        public void Apply()
        {
            Constants.MARKET_MINUTES_PER_REAL_SECOND = MinutesPerRealSecond;

            Constants.PRICE_REVERSION_PER_TICK = ReversionPerTick;
            Constants.PRICE_REVERSION_DOWN_PER_TICK = ReversionDownPerTick;
            Constants.PRICE_NOISE_BASE = NoiseBase;
            Constants.PRICE_MOMENTUM_RETENTION = MomentumRetention;
            Constants.EXPECTATION_DECAY_PER_TICK = ExpectationDecayPerTick;
            Constants.PRICE_MAX_MOVE_PER_TICK = MaxMovePerTick;
            Constants.PRICE_MAX_DROP_PER_TICK = MaxDropPerTick;

            Constants.MARKET_MICRO_SECONDS = MicroSeconds;
            Constants.MICRO_AMPLITUDE = MicroAmplitude;
            Constants.MICRO_RETENTION = MicroRetention;
            Constants.MICRO_LIMIT = MicroLimit;

            Constants.ORDER_IMPACT_SCALE = OrderImpactScale;
            Constants.BOOK_TICK_RATIO = BookTickRatio;
            Constants.BOOK_DEPTH_BASE = BookDepthBase;
            Constants.BOOK_DEPTH_GROWTH = BookDepthGrowth;

            Constants.STARTING_CASH = StartingCash;
        }

        /// <summary>표를 만들 때 쓴다. 이름으로 값을 넣는다.</summary>
        public void SetByKey(string key, float value)
        {
            switch (key)
            {
                case "MARKET_MINUTES_PER_REAL_SECOND":
                {
                    MinutesPerRealSecond = value;
                    break;
                }
                case "PRICE_REVERSION_PER_TICK":
                {
                    ReversionPerTick = value;
                    break;
                }
                case "PRICE_REVERSION_DOWN_PER_TICK":
                {
                    ReversionDownPerTick = value;
                    break;
                }
                case "PRICE_NOISE_BASE":
                {
                    NoiseBase = value;
                    break;
                }
                case "PRICE_MOMENTUM_RETENTION":
                {
                    MomentumRetention = value;
                    break;
                }
                case "EXPECTATION_DECAY_PER_TICK":
                {
                    ExpectationDecayPerTick = value;
                    break;
                }
                case "PRICE_MAX_MOVE_PER_TICK":
                {
                    MaxMovePerTick = value;
                    break;
                }
                case "PRICE_MAX_DROP_PER_TICK":
                {
                    MaxDropPerTick = value;
                    break;
                }
                case "MARKET_MICRO_SECONDS":
                {
                    MicroSeconds = value;
                    break;
                }
                case "MICRO_AMPLITUDE":
                {
                    MicroAmplitude = value;
                    break;
                }
                case "MICRO_RETENTION":
                {
                    MicroRetention = value;
                    break;
                }
                case "MICRO_LIMIT":
                {
                    MicroLimit = value;
                    break;
                }
                case "ORDER_IMPACT_SCALE":
                {
                    OrderImpactScale = value;
                    break;
                }
                case "BOOK_TICK_RATIO":
                {
                    BookTickRatio = value;
                    break;
                }
                case "BOOK_DEPTH_BASE":
                {
                    BookDepthBase = value;
                    break;
                }
                case "BOOK_DEPTH_GROWTH":
                {
                    BookDepthGrowth = value;
                    break;
                }
                case "STARTING_CASH":
                {
                    StartingCash = value;
                    break;
                }
                default:
                {
                    break;
                }
            }
        }
    }
}
