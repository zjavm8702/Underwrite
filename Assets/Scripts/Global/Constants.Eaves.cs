namespace Underwrite.Global
{
    /// <summary>여관 엿듣기. 문구는 과장 초안 — 사장님 검토 대기(2026-10-18).</summary>
    public static partial class StringConstants
    {
        public static string TEXT_EAVES_CHOICE = "술을 나르며 테이블 사이를 돈다";
        public static string TEXT_EAVES_CHOICE_NOTE = "엿듣기 · 귀 기울일 기회 {0}번, 한 번에 {1}분";
        public static string TEXT_EAVES_NONE = "오늘은 귀를 기울일 만한 테이블이 없다";
        public static string TEXT_EAVES_DONE_TODAY = "오늘은 이미 홀을 돌았다";

        public static string TEXT_EAVES_TITLE = "여관 홀 · 엿듣기";
        public static string TEXT_EAVES_HINT = "한 테이블에 두 번 머물면 이야기를 끝까지 듣는다. 경계하는 테이블에 오래 서 있으면 들킨다.";
        public static string TEXT_EAVES_TURNS = "남은 기회 {0}";
        public static string TEXT_EAVES_LISTEN = "곁에 머문다";
        public static string TEXT_EAVES_LISTENED = "{0}번 머묾";
        public static string TEXT_EAVES_TABLE_DONE = "다 들었다";
        public static string TEXT_EAVES_LEAVE = "부엌으로 돌아간다";

        // 경계심(1 → 3)에 따라. 1이면 첫 머묾 뒤 바로 다음에 들킨다.
        public static readonly string[] TEXT_EAVES_MOODS =
        {
            "목소리를 낮추고 자꾸 주위를 둘러본다",
            "말이 많지만 가끔 이쪽을 본다",
            "취해서 떠든다. 아무도 일꾼을 보지 않는다"
        };

        public static string TEXT_EAVES_FRAGMENT = "{0} 쪽에서 「…{1}…」 하는 소리가 들린다.";
        public static string TEXT_EAVES_FULL = "{0} 쪽 이야기를 끝까지 들었다 — {1}";
        public static string TEXT_EAVES_HEROES_NOTHING = "용사단은 오늘 별 이야기가 없다. 술잔만 오간다.";
        public static string TEXT_EAVES_CAUGHT = "「…뭘 그렇게 듣고 있어?」 {0} 쪽 사람이 고개를 돌렸다. 오늘은 홀에 나가지 않는 게 좋겠다.";
    }
}
