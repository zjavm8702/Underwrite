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
}
