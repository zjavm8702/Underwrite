namespace Underwrite.Enumeration
{
    /// <summary>
    /// 할 일을 맡기는 사람(2026-10-17 사장님 결정 — 받는 장면이 있어야 한다).
    /// 영주민은 정보 출처의 사람(InfoPeople)과 같은 얼굴을 쓴다.
    /// </summary>
    public enum QuestGiver
    {
        /// <summary>누가 시킨 것이 아니라 주인공이 스스로 정한 일. 혼잣말로 받는다.</summary>
        Giver_Self,
        Giver_Innkeeper,
        Giver_Archivist,
        Giver_Broker,
        Giver_Swordswoman,
        Giver_Scholar,
        Giver_Elf
    }

    /// <summary>
    /// 계절 계획에서 원정 기간의 한 달에 맡는 일(시간구조 설계안 §3 「주인공은 그동안 무엇을 하나」).
    /// 출발 밤에 세 달 몫을 고른다. 같은 일을 여러 달 골라도 된다.
    /// </summary>
    public enum SeasonTask
    {
        /// <summary>여관 일을 더 맡는다. 다음 계절 투자 자금.</summary>
        Task_Inn,

        /// <summary>길드 기록실 심부름. 다음 계절에 오는 단 하나의 내력을 미리 안다.</summary>
        Task_Guild,

        /// <summary>대장간 견습. 다음 계절 단 하나의 대장간 사정을 미리 안다.</summary>
        Task_Smith,

        /// <summary>지하 사람들과 어울린다. 재개장 장에서 정보상이 귀띔을 하나 더 준다.</summary>
        Task_Underworld,

        /// <summary>원정 짐수레를 따라간다. 용사단 모두와 조금 가까워진다.</summary>
        Task_Wagon,

        /// <summary>국경의 징조를 찾는다. 멸망의 단서가 쌓인다(설계안 §4).</summary>
        Task_Omens
    }

    /// <summary>
    /// 프롤로그 — 지하를 찾기 전의 마을 생활(2026-10-18 온보딩 설계안). 단계마다 열리는 것이 다르다.
    /// 순서가 곧 진행이다: 혼잣말 → 마을 순찰(여관 → 대장간 → 길드 → 게시판 → 영주관) → 잠 → 여덟째 날 주방 일 → 끝.
    /// </summary>
    public enum PrologueStage
    {
        /// <summary>일곱째 날 첫 혼잣말과 걷기 안내.</summary>
        Stage_Opening,

        Stage_Inn,
        Stage_Smith,
        Stage_Guild,
        Stage_Board,
        Stage_Manor,

        /// <summary>순찰을 마쳤다. 여관 다락방에서 자야 한다.</summary>
        Stage_Sleep,

        /// <summary>여덟째 날. 여관 주방 일을 거들다 창고를 찾는다(웹툰 5~6화).</summary>
        Stage_Kitchen,

        /// <summary>지하를 찾았다. 프롤로그 끝.</summary>
        Stage_Done
    }

    /// <summary>첫 지하 방문 튜토리얼의 단계(2026-10-18). 순서가 곧 진행이다.</summary>
    public enum MarketOnboardingStage
    {
        /// <summary>정보상의 첫인사와 시장 게시판 소개.</summary>
        Onboarding_Arrive,

        /// <summary>게시판으로 가서 거래판을 연다. 처음 열면 거래판 안내.</summary>
        Onboarding_ToBoard,

        /// <summary>1주 산다(매수·시장가·1주 고정).</summary>
        Onboarding_FirstBuy,

        /// <summary>산 1주를 판다. 팔면 현금을 도착 때로 되돌린다.</summary>
        Onboarding_FirstSell,

        /// <summary>시간이 흐른다. 첫 정보 타임에 정보상이 자기와 정보를 소개한다.</summary>
        Onboarding_FirstBriefing,

        /// <summary>소식을 본 뒤 다시 거래하는 법.</summary>
        Onboarding_TradeAgain,

        Onboarding_Done
    }
}
