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
        public const string VILLAGE = "VillageTest";
    }
}
