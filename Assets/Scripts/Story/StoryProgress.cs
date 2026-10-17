namespace Underwrite.Story
{
    /// <summary>
    /// 이야기의 큰 고비를 지났는지 적어 둔다. 씬을 넘나들어야 하므로 정적이다.
    /// 저장 기능이 들어오면 함께 저장한다.
    /// </summary>
    public static class StoryProgress
    {
        /// <summary>
        /// 여관 창고 안쪽의 지하거래소를 찾았는가. 웹툰 5~6화를 플레이어가 직접 겪은 뒤에 참이 된다.
        /// 그 전에는 거래소로 내려가는 길이 보이지 않는다(2026-10-17 사용자 결정 — 마을 사건을 먼저 겪는다).
        /// </summary>
        public static bool CellarFound { get; set; }

        /// <summary>지하거래소 첫 안내를 마쳤는가(건너뛴 것도 마친 것으로 본다).</summary>
        public static bool MarketTutorialDone { get; set; }

        /// <summary>영지 첫 안내를 마쳤는가.</summary>
        public static bool VillageTutorialDone { get; set; }

        public static void Reset()
        {
            CellarFound = false;
            MarketTutorialDone = false;
            VillageTutorialDone = false;
        }
    }
}
