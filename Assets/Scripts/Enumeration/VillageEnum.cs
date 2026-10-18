namespace Underwrite.Enumeration
{
    /// <summary>
    /// 영지의 자리에서 할 수 있는 일.
    ///
    /// 장이 끝나면 그 시간은 플레이어의 것이다. 기획서 §4는 영지에서
    /// "퀘스트, 대화, 원정 준비를 통해 **돈과 정보를** 얻는다"고 했다.
    /// 자리마다 하나씩 다른 쓸모가 있어야 고르는 일이 된다.
    /// </summary>
    public enum VillageActivity
    {
        /// <summary>돈을 내고 다음 소식을 미리 안다.</summary>
        Activity_Gather,

        /// <summary>시간을 써서 돈을 번다. 기획서 §10의 종잣돈 퀘스트.</summary>
        Activity_Work,

        /// <summary>아무것도 하지 않고 다음 장을 기다린다.</summary>
        Activity_Rest,

        /// <summary>
        /// 다른 곳으로 들어간다. 지하시장 입구가 여기에 해당한다.
        ///
        /// 거래소로 가는 길이 화면 구석의 단추가 아니라 **걸어가서 여는 문**이어야
        /// 영지와 거래소가 한 세계로 이어진다.
        /// </summary>
        Activity_Enter
    }

    /// <summary>
    /// 영지에 선 사람이 무슨 자리에 있는가. 영지가 비어 보이지 않게 최대한 많이 세운다.
    /// </summary>
    public enum NpcRole
    {
        /// <summary>건물마다 한두 명. 문 앞 근처에 서 있다.</summary>
        Role_Resident,

        /// <summary>노점마다 한 명. 자리를 지킨다.</summary>
        Role_Vendor,

        /// <summary>
        /// 모험단의 한 사람. 단 단위로 무리 지어 영지를 돌아다닌다.
        /// 지하시장이 이들을 거래하지만 이들은 그것을 모른다. 설계안 §1-2.
        /// </summary>
        Role_Adventurer
    }

    /// <summary>주문서 읽기에서 짚은 주문서가 무엇이었나(2026-10-18).</summary>
    public enum SlipPick
    {
        /// <summary>그 단의 이번 계절, 취소되지 않은 주문. 소문을 판정할 증거다.</summary>
        Pick_Evidence,

        /// <summary>다른 단의 주문.</summary>
        Pick_OtherBand,

        /// <summary>그 단의 주문이지만 지난 계절 것.</summary>
        Pick_LastSeason,

        /// <summary>그 단의 이번 계절 주문이지만 취소됐다.</summary>
        Pick_Cancelled
    }

    /// <summary>영지의 휴장 하나(2026-10-19 — 행동력). 영지의 낮·밤도 이 셋으로 바뀐다.</summary>
    public enum VillagePhase
    {
        /// <summary>장 전 아침(잠에서 깬 뒤 오전장까지).</summary>
        Phase_Morning,

        /// <summary>오전장이 끝난 뒤 오후장까지.</summary>
        Phase_Afternoon,

        /// <summary>오후장이 끝난 뒤 새벽. 장이 없다 — 자면 다음 날 오전.</summary>
        Phase_Night
    }

    /// <summary>기록 짜맞추기 단서가 무엇과 무엇을 잇는가.</summary>
    public enum ClueKind
    {
        Clue_BandDestination,
        Clue_BandClient,
        Clue_DestinationClient
    }
}
