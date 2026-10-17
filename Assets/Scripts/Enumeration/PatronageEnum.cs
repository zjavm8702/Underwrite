namespace Underwrite.Enumeration
{
    /// <summary>
    /// 익명 의뢰가 무엇을 위한 것인가. 후원 설계안 §3-2의 첫 항목이다.
    /// 소문을 잘못 읽으면 엉뚱한 것이 간다. 이 판단이 후원의 핵심이다.
    /// </summary>
    public enum RequestPurpose
    {
        /// <summary>치료비를 선금으로 주는 의뢰.</summary>
        Purpose_Healing,

        /// <summary>장비 수리비를 선금으로 주는 의뢰.</summary>
        Purpose_Gear,

        /// <summary>식량·물자를 미리 대 주는 의뢰.</summary>
        Purpose_Supplies,

        /// <summary>교관을 붙여 주는 의뢰.</summary>
        Purpose_Training
    }

    /// <summary>용사단 사정 하나가 어디까지 왔는가.</summary>
    public enum NeedState
    {
        /// <summary>아직 일어나지 않았다.</summary>
        State_Hidden,

        /// <summary>일어났다. 여관에서 들을 수 있다. 기한이 흐른다.</summary>
        State_Open,

        /// <summary>맞는 의뢰가 들어가 해결됐다.</summary>
        State_Met,

        /// <summary>기한을 넘겼다. 시나리오가 깨진다(상장흐름 설계안 §1-5).</summary>
        State_Failed
    }
}
