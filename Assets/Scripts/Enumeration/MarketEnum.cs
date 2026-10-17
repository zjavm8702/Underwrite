namespace Underwrite.Enumeration
{
    /// <summary>종목의 초기 경향. 고정된 수익 보장이 아니며 성장·부상·사건으로 바뀐다. 기획서 §7.</summary>
    public enum StockPersonality
    {
        Personality_Veteran,
        Personality_Rookie,
        Personality_HighRisk,
        Personality_Famous,

        /// <summary>
        /// 용사단. 지수형(S&P 500·VOO)이다 — 아주 조금씩, 꾸준히 오른다(상장흐름 설계안 §1-4).
        /// 3년치 실적이 공개돼 있어 놀라움이 없다. 후원 기한을 넘기면 무너진다(§1-5).
        /// 표(PersonalityTable)로 덮어쓰지 않는다. 맨 뒤에 둔다 — 앞에 끼우면 직렬화된 값이 밀린다.
        /// </summary>
        Personality_Index
    }

    /// <summary>여러 종목에 공통으로 걸리는 시장 분위기. 기획서 §7.</summary>
    public enum MarketSentiment
    {
        Sentiment_Calm,
        Sentiment_Fever,
        Sentiment_Fear,
        Sentiment_Drained
    }

    /// <summary>
    /// 하루의 두 장. 지하시장은 낮과 밤에 한 번씩 열린다.
    /// 오전장은 모험가들이 의뢰를 받고 나가는 시간이고, 오후장은 돌아온 뒤의 시간이다.
    /// </summary>
    public enum MarketSession
    {
        Session_Morning,
        Session_Evening
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

    /// <summary>
    /// 주문 유형. 시장가는 즉시 체결되고, 지정가는 가격이 지정한 값을 통과할 때까지 기다린다.
    /// </summary>
    public enum OrderType
    {
        Type_Market,
        Type_Limit
    }

    /// <summary>대기 중인 지정가 주문의 상태.</summary>
    public enum PendingState
    {
        Pending_Waiting,
        Pending_Filled,
        Pending_Cancelled,
        Pending_ExpiredAtClose
    }

    /// <summary>주문 처리 결과. 실패 사유를 호출하는 쪽에 알린다.</summary>
    public enum OrderResult
    {
        Result_Filled,
        Result_RejectedMarketClosed,
        Result_RejectedPaused,
        Result_RejectedNotAtMarket,
        Result_RejectedNoCash,
        Result_RejectedNoShares,
        Result_RejectedBadQuantity,
        Result_RejectedBadPrice,
        Result_Queued
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

    /// <summary>
    /// 종목이 시장을 떠나는 사유. 설계안 §5.
    ///
    /// 예고가 있는 이탈과 없는 이탈을 나누는 것이 핵심이다.
    /// 예고 없는 전멸이 고위험 종목의 진짜 위험이고,
    /// 예고 있는 은퇴는 "언제 팔까"라는 판단거리가 된다.
    /// </summary>
    public enum DelistReason
    {
        Delist_None,
        Delist_Wiped,
        Delist_Retired,
        Delist_Moved
    }

    /// <summary>정보의 출처. 신뢰도 판단 근거로 플레이어에게 함께 보여준다. 기획서 §8.</summary>
    public enum InfoSource
    {
        Source_TavernTalk,
        Source_GuildRecord,
        Source_PrivateLetter,
        Source_MarketBoard,
        Source_BrokerLeak,

        /// <summary>
        /// 대장간의 주문 장부. 누가 장비를 크게 맞췄는지로 원정 규모를 읽는다.
        /// 내용이 아니라 **움직임의 크기**를 알려 주는 출처다.
        ///
        /// 뒤에 붙인 이유는 앞에 끼우면 이미 저장된 값이 한 칸씩 밀리기 때문이다.
        /// </summary>
        Source_SmithOrder
    }
}
