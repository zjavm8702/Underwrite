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

        /// <summary>
        /// 지금 돌고 있는 시장 전체. 거래 화면이 처음 만들 때 걸어 두고, 다시 열릴 때 이것을 이어 쓴다.
        ///
        /// 거래 화면이 시장을 자기 필드로만 들고 있으면, 영지에 다녀오는 순간 씬과 함께 사라진다.
        /// 돌아오면 새 시장이 만들어져 현금·보유·날짜가 전부 처음으로 돌아갔다.
        /// </summary>
        public static MarketSimulation Simulation { get; set; }
    }
}
