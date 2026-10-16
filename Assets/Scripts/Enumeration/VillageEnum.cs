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
        Activity_Rest
    }
}
