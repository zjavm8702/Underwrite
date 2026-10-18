namespace Underwrite.Global
{
    /// <summary>
    /// 씬 이름을 한곳에 모은다.
    ///
    /// 코드 여기저기에 "VillageTest" 같은 날문자열이 박히면
    /// 씬 이름을 바꿀 때 무엇이 깨지는지 알 수 없다.
    /// </summary>
    public static class SceneRoutes
    {
        public const string INTRO = "Intro";
        public const string MARKET = "MarketPrototype";

        /// <summary>걸어 다니는 지하 방. 영지에서 창고로 내려가면 여기로 온다(2026-10-17).</summary>
        public const string UNDERWORLD = "Underworld";
        /// <summary>
        /// 영지. 2026-10-17부터 전체 파츠 지도(메뉴 「Underwrite/영지 전체 씬 만들기」)다.
        /// 옛 통짜 배경 영지(VillageTest)는 비교용으로 남겨 둔다.
        /// </summary>
        public const string VILLAGE = "Village";

        /// <summary>
        /// 지하에서 올라왔는가. 영지가 뜨면 주인공을 여관 문 앞에 세우고 지운다 — 창고는 여관 지하다(2026-10-18 사장님).
        /// 씬 사이에 한 번만 건네는 값이라 저장하지 않는다.
        /// </summary>
        public static bool ArrivedFromCellar;
    }
}
