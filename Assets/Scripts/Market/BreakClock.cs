namespace Underwrite.Market
{
    /// <summary>
    /// 씬을 넘나드는 시계 한 벌.
    ///
    /// 영지와 거래소가 각자 시계를 들면 시각이 어긋난다. 장이 끝난 시각에서
    /// 영지의 시간이 이어 흘러야 하므로 같은 시계를 양쪽이 본다.
    /// 저장 기능이 들어오면 이 시계도 함께 저장한다.
    /// </summary>
    public static class BreakClock
    {
        /// <summary>지금 쓰는 시계. 거래 화면이 시뮬레이션을 만들 때 걸어 둔다.</summary>
        public static MarketClock Current { get; set; }
    }
}
