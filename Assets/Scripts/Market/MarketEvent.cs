using System;
using Underwrite.Enumeration;

namespace Underwrite.Market
{
    /// <summary>
    /// 사건 한 단계. 기획서 §7의 "소문 → 중간 보고 → 결과" 연쇄를 한 줄씩 나타낸다.
    ///
    /// 단계마다 하는 일이 다르다.
    /// - 소문: 시장 기대만 움직인다. 실제 가치는 그대로다. 틀린 소문일 수도 있다.
    /// - 중간 보고: 기대를 일부 수정한다. 실제 가치는 아직 확정되지 않는다.
    /// - 결과: 실제 가치를 확정하고 기대를 지운다. 이미 반영된 만큼은 더 오르지 않는다.
    /// </summary>
    public sealed class MarketEventStep
    {
        /// <summary>
        /// 이 단계가 들어오는 정보 타임 번호. 0부터 센다.
        /// 틱이 아니라 번호인 이유는 장이 바뀌면 틱이 0으로 돌아가기 때문이다.
        /// 한 장에 정보 타임이 두 번이므로 0·1은 첫 장, 2·3은 다음 장이 된다.
        /// </summary>
        public int Slot { get; private set; }

        public string StockId { get; private set; }
        public EventStage Stage { get; private set; }

        /// <summary>소문·보고가 시장 기대에 더하는 값. 결과 단계에서는 쓰지 않는다.</summary>
        public float ExpectationDelta { get; private set; }

        /// <summary>결과 단계가 실제 내재가치에 곱하는 변화율. 다른 단계에서는 0이다.</summary>
        public float ValueDelta { get; private set; }

        /// <summary>플레이어에게 보여줄 문구. 출처와 함께 제공해 추론 근거로 쓴다.</summary>
        public string Headline { get; private set; }

        public InfoSource Source { get; private set; }

        /// <summary>
        /// 이 정보가 누구에게 보이는지. 플레이어만 아는 정보와 시장에 공개된 정보를 나눈다.
        /// 공개된 정보만 기대에 반영된다. 기획서 §8.
        /// </summary>
        public InfoVisibility Visibility { get; private set; }

        public MarketEventStep(int slot, string stockId, EventStage stage, string headline,
            InfoSource source, InfoVisibility visibility,
            float expectationDelta = 0f, float valueDelta = 0f)
        {
            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), "Event slot must not be negative.");
            }

            if (string.IsNullOrWhiteSpace(stockId) == true)
            {
                throw new ArgumentException("Event must target a stock id.", nameof(stockId));
            }

            Slot = slot;
            StockId = stockId;
            Stage = stage;
            Headline = headline;
            Source = source;
            Visibility = visibility;
            ExpectationDelta = expectationDelta;
            ValueDelta = valueDelta;
        }

        /// <summary>시장 기대에 반영되는 정보인지. 플레이어만 아는 정보는 가격을 움직이지 않는다.</summary>
        public bool AffectsMarket
        {
            get
            {
                return Visibility == InfoVisibility.Visibility_Public;
            }
        }

        /// <summary>플레이어에게 보여줄 정보인지. 실제 사건은 결과가 공개될 때까지 숨긴다.</summary>
        public bool VisibleToPlayer
        {
            get
            {
                return Visibility != InfoVisibility.Visibility_TruthOnly;
            }
        }
    }

    /// <summary>플레이어가 실제로 읽은 정보 한 줄. 출처와 시점을 함께 남겨 추론 근거로 쓴다.</summary>
    public sealed class InfoEntry
    {
        public int Tick { get; private set; }
        public string StockId { get; private set; }
        public string Headline { get; private set; }
        public InfoSource Source { get; private set; }
        public EventStage Stage { get; private set; }

        /// <summary>시장에도 공개된 정보인지. 거짓이면 플레이어만 아는 정보다.</summary>
        public bool IsPublic { get; private set; }

        /// <summary>며칠째 어느 장에 들어온 소식인가. 틱만으로는 언제인지 알 수 없다.</summary>
        public int Day { get; private set; }
        public MarketSession Session { get; private set; }

        public InfoEntry(int tick, string stockId, string headline, InfoSource source,
            EventStage stage, bool isPublic, int day, MarketSession session)
        {
            Day = day;
            Session = session;
            Tick = tick;
            StockId = stockId;
            Headline = headline;
            Source = source;
            Stage = stage;
            IsPublic = isPublic;
        }
    }
}
