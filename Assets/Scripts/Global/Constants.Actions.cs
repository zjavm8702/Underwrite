namespace Underwrite.Global
{
    /// <summary>영지 행동력(2026-10-19). 문구는 과장 초안 — 사장님 검토 대기.</summary>
    public static partial class StringConstants
    {
        public static string TEXT_PHASE_MORNING = "오전";
        public static string TEXT_PHASE_AFTERNOON = "오후";
        public static string TEXT_PHASE_NIGHT = "새벽";
        public static string TEXT_VILLAGE_STATUS = "{0} · {1} · 행동력 {2}";
        public static string TEXT_AP_FULL = "●";
        public static string TEXT_AP_EMPTY = "○";

        public static string TEXT_MENU_TO_SESSION = "장으로 간다";
        public static string TEXT_MENU_TO_SESSION_NOTE = "남은 행동력 {0}은 사라진다";
        public static string TEXT_MENU_NIGHT_NO_SESSION = "새벽엔 장이 없다 · 다락방에서 잔다";
        public static string TEXT_MENU_SKIP_SESSION = "이번 장은 쉰다 · {0}";
        public static string TEXT_MENU_SKIP_LOCKED = "재개장은 쉴 수 없다";
        public static string TEXT_SKIP_SESSION_FADE = "장을 쉬었다";

        // 모험단에게 말 걸기의 반응(필드 말풍선).
        public static string TEXT_NPC_ALREADY_KNOWN = "이미 아는 얘기다";
        public static string TEXT_NPC_NO_AP = "오늘은 더 캐물을 기운이 없다";
        public static string TEXT_NPC_SHRUG = "…별 얘기는 없다";

        // 장면 일러스트를 까는 대화 배경 클래스(MarketScreen.uss).
        public const string SCENE_CLASS_ATTIC = "dialogue-root--attic";
        public const string SCENE_CLASS_HEROES = "dialogue-root--heroes";

        // 게임 안 웹툰(2일째 아침 7장).
        public static string TEXT_WEBTOON_HINT = "클릭 · 스페이스 — 다음";
        public const string WEBTOON_CH7_RESOURCE = "Webtoon/webtoon_07";

        // 2일째 아침 — 행동력 안내.
        public static string TEXT_AP_TUT_1_TITLE = "행동력";
        public static string TEXT_AP_TUT_1_BODY = "영지에서는 시간 대신 행동력을 쓴다. 오전·오후에는 3, 새벽에는 2. 오른쪽 위에 지금 휴장과 남은 행동력이 보인다.";
        public static string TEXT_AP_TUT_2_TITLE = "무엇에 드나";
        public static string TEXT_AP_TUT_2_BODY = "돈 내고 묻기(여관·길드·대장간·영주관) — 돈과 행동력 1.\n모험단에게 말 걸어 캐묻기 — 행동력 1.\n미니게임(주사위·주문서·기록) — 행동력 1.\n품삯 일 — 행동력 2.\n걷기·용사단과 대화·익명 의뢰는 공짜다.";
        public static string TEXT_AP_TUT_3_TITLE = "장으로 간다";
        public static string TEXT_AP_TUT_3_BODY = "오전·오후에는 메뉴의 「장으로 간다」(또는 여관 창고)로 지하 장에 내려간다. 행동력이 남아도 갈 수 있고, 남은 것은 사라진다. 장을 거르고 싶으면 「이번 장은 쉰다」.";
        public static string TEXT_AP_TUT_4_TITLE = "새벽";
        public static string TEXT_AP_TUT_4_BODY = "오후장이 끝나고 올라오면 새벽이다. 새벽에는 장이 없다 — 남은 행동력을 쓰고 다락방에서 자면 다음 날 오전이 된다.";
    }
}
