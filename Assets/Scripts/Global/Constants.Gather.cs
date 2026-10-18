namespace Underwrite.Global
{
    /// <summary>
    /// 영지에서 돈을 내고 묻는 정보 모으기(장소마다 한 번에 60분)의 문구.
    /// 코드에 박혀 있던 것을 옮겼다(2026-10-18, 코드 규칙 §7-2 — 현지화 전에 모두 상수로).
    /// </summary>
    public static partial class StringConstants
    {
        public static string TEXT_GATHER_COST = "{0:N0}을 낸다";
        public static string TEXT_GATHER_FREE = "값은 없다";
        public static string TEXT_GATHER_KEY = "E — 묻는다";
        public static string TEXT_GATHER_KEY_LATE = "행동력이 없다";
        public static string TEXT_GATHER_TOO_LATE = "행동력이 없다. 장으로 가거나, 새벽이면 자야 한다.";
        public static string TEXT_GATHER_NOTHING = "아직 돌고 있는 이야기가 없습니다.";

        public static string TEXT_GATHER_GUILD_TITLE = "기록에 적혀 있다";
        public static string TEXT_GATHER_BOARD_TITLE = "게시판을 훑는다";
        public static string TEXT_GATHER_BOARD_BODY = "새로 붙은 종이는 없습니다. 요즘은 모험단 소식만 붙는군요.";
        public static string TEXT_GATHER_SMITH_TITLE = "주문 장부를 넘겨 본다";
        public static string TEXT_GATHER_SMITH_BODY = "{0} 쪽에서 장비를 크게 맞췄습니다. 큰 원정을 준비하는 모양입니다.";
        public static string TEXT_GATHER_SMITH_NOTE = "{0} 쪽이 장비를 크게 맞췄다";
        public static string TEXT_GATHER_LETTER_TITLE = "서신을 읽는다";
        public static string TEXT_GATHER_TAVERN_TITLE = "술자리에서 들었다";
        public static string TEXT_GATHER_TAVERN_BODY = "{0} 쪽 이야기가 돈다는 것까지는 알아냈습니다. 내용까지는 못 들었습니다.";

        // 여관 소문처럼 누구 이야기인지만 아는 수첩 한 줄. 일하다 주워들은 말에도 쓴다.
        public static string TEXT_GATHER_VAGUE_NOTE = "{0} 쪽에 무슨 일이 있다.";

        public static string TEXT_GATHER_SPENT = "{0:N0}을 썼습니다. ";
        public static string TEXT_GATHER_TIME_LEFT = "남은 행동력 {0}.";
        public static string TEXT_GATHER_SECRET = "(아직 아무도 모르는 이야기다) ";
    }
}
