namespace Underwrite.Global
{
    /// <summary>
    /// 주문 나르기 — 여관·대장간·길드의 일(2026-10-17 사장님 결정: 실제로 해야 끝나는 일). 문구는 과장 초안.
    /// 물건 목록과 행선지는 일마다 다르고, 순서는 각 배열 안에서만 의미가 있다.
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_WORK_TITLE_TAVERN = "여관 부엌 · 주문 나르기";
        public static string TEXT_WORK_TITLE_SMITH = "대장간 · 장비 배달";
        public static string TEXT_WORK_TITLE_GUILD = "길드 기록실 · 전리품 정산";

        public static readonly string[] WORK_TAVERN_ITEMS = { "맥주", "포도주", "스튜", "빵", "구운 고기" };
        public static readonly string[] WORK_TAVERN_PLACES = { "창가 테이블", "난로 옆", "구석 테이블", "계단 아래", "문가 테이블" };
        public static readonly string[] WORK_SMITH_ITEMS = { "편자", "숫돌", "방패끈", "화살촉", "투구 끈" };
        public static readonly string[] WORK_SMITH_PLACES = { "동쪽 숙소", "강가 천막", "여관 2층", "성문 초소", "방앗간 헛간" };
        public static readonly string[] WORK_GUILD_ITEMS = { "은화 자루", "마물 뿔", "약초 묶음", "원석", "가죽" };
        public static readonly string[] WORK_GUILD_PLACES = { "첫째 정산함", "둘째 정산함", "셋째 정산함", "넷째 정산함", "다섯째 정산함" };

        public static string TEXT_WORK_MEMORIZE = "주문표를 외운다 · {0}초 뒤에 가린다";
        public static string TEXT_WORK_READY = "다 외웠다";
        public static string TEXT_WORK_SLOPPY = "대충 끝낸다";
        public static string TEXT_WORK_SLOPPY_HINT = "대충 끝내면 품삯은 절반이다.";
        public static string TEXT_WORK_ITEM_COUNT = "{0} ×{1}";
        public static string TEXT_WORK_SERVE_HEAD = "{0} / {1} · {2}";
        public static string TEXT_WORK_SERVE = "내놓는다";
        public static string TEXT_WORK_CLEAR = "비운다";
        public static string TEXT_WORK_TRAY = "쟁반";
        public static string TEXT_WORK_TRAY_EMPTY = "비었다";
        public static string TEXT_WORK_RIGHT = "맞다. 받는 사람이 고개를 끄덕인다.";
        public static string TEXT_WORK_WRONG = "아니다 — 주문은 {0}였다.";
        public static string TEXT_WORK_SCORE = "{0}건 가운데 {1}건을 맞게 냈다 · 품삯 {2:N0}";
    }
}
