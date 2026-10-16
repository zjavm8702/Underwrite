namespace Underwrite.Enumeration
{
    /// <summary>종목의 초기 경향. 고정된 수익 보장이 아니며 성장·부상·사건으로 바뀐다. 기획서 §7.</summary>
    public enum StockPersonality
    {
        Personality_Veteran,
        Personality_Rookie,
        Personality_HighRisk,
        Personality_Famous
    }

    /// <summary>여러 종목에 공통으로 걸리는 시장 분위기. 기획서 §7.</summary>
    public enum MarketSentiment
    {
        Sentiment_Calm,
        Sentiment_Fever,
        Sentiment_Fear,
        Sentiment_Drained
    }

    /// <summary>지하시장의 개장 상태. 영주 조사일에는 휴장한다. 기획서 §11.</summary>
    public enum MarketPhase
    {
        Phase_Closed,
        Phase_Open,
        Phase_Halted
    }

    public enum OrderSide
    {
        Side_Buy,
        Side_Sell
    }

    /// <summary>주문 처리 결과. 실패 사유를 호출하는 쪽에 알린다.</summary>
    public enum OrderResult
    {
        Result_Filled,
        Result_RejectedMarketClosed,
        Result_RejectedNotAtMarket,
        Result_RejectedNoCash,
        Result_RejectedNoShares,
        Result_RejectedBadQuantity
    }

    /// <summary>
    /// 사건의 단계. 소문은 기대만 움직이고, 결과가 실제 가치를 확정한다. 기획서 §7·§8.
    /// </summary>
    public enum EventStage
    {
        Stage_Rumor,
        Stage_Report,
        Stage_Outcome
    }

    /// <summary>
    /// 정보의 세 가지 상태. 플레이어 판단 기회는 이 셋의 차이에서 나온다. 기획서 §8.
    /// </summary>
    public enum InfoVisibility
    {
        Visibility_TruthOnly,
        Visibility_PlayerOnly,
        Visibility_Public
    }

    /// <summary>정보의 출처. 신뢰도 판단 근거로 플레이어에게 함께 보여준다. 기획서 §8.</summary>
    public enum InfoSource
    {
        Source_TavernTalk,
        Source_GuildRecord,
        Source_PrivateLetter,
        Source_MarketBoard,
        Source_BrokerLeak
    }
}
