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

        public static string TEXT_GATHER_GUILD_TITLE = "기록에 적혀 있다";
        public static string TEXT_GATHER_BOARD_TITLE = "게시판을 훑는다";
        public static string TEXT_GATHER_BOARD_BODY = "새로 붙은 건 없어. 요즘은 모험단 소식만 올라오네.";

        // 장소 주인마다 말투가 다르다(대사 검증 v1, 10/19). 순서: 여관 주인 · 기록 담당 · 대장장이 · 중개인 · 집사(InfoPeople.Pick).
        public static readonly string[] TEXT_GATHER_ASK_BY =
        {
            "들은 게 있긴 한데, 공짜로 줄 순 없지.", "들은 게 있긴 하네만, 공짜로 줄 수는 없네.", "들은 건 있지. 공짜로 알려 줄 순 없고.",
            "들은 게 있긴 한데, 공짜로 줄 순 없지.", "들은 게 있긴 하네만, 공짜로 줄 수는 없네."
        };
        public static readonly string[] TEXT_GATHER_ASK_AGAIN_BY =
        {
            "또 무슨 일이야?", "또 볼일이 있나?", "또 뭐 필요한 거 있어?", "또 궁금한 게 있어?", "또 무슨 볼일인가?"
        };
        public static readonly string[] TEXT_GATHER_NOTHING_BY =
        {
            "아직 들은 얘기가 없어.", "아직 전해 줄 이야기가 없네.", "아직 들은 게 없어.", "아직 도는 얘기가 없어.", "아직 전해 줄 이야기가 없네."
        };
        public static readonly string[] TEXT_GATHER_TOO_LATE_BY =
        {
            "오늘은 피곤해 보이네. 좀 쉬어.", "지친 모양이군. 쉬었다 오게.", "피곤하면 쉬어. 다치기 전에.", "오늘은 좀 쉬어.", "피곤한 모양이군. 쉬었다 오게."
        };
        public static readonly string[] TEXT_WORK_RUMOR_BY =
        {
            "그러고 보니 {0} 쪽에 무슨 일이 있다더라.", "그러고 보니 {0} 쪽에 무슨 일이 있다는군.", "그러고 보니 {0} 쪽에 일이 생겼다던데.",
            "그러고 보니 {0} 쪽에 무슨 일이 있다더라.", "그러고 보니 {0} 쪽에 무슨 일이 있다는군."
        };
        public static readonly string[] TEXT_CODEX_LEARNED_BY =
        {
            "그 단 얘기라면 들은 게 있어. {0}", "그 단에 관한 기록일세. {0}", "그 단 얘기라면 이 정도는 알지. {0}",
            "그 단 얘기라면 들은 게 있어. {0}", "그 단에 관한 이야기라면 이 정도일세. {0}"
        };
        public static string TEXT_GATHER_SMITH_TITLE = "주문 장부를 넘겨 본다";
        public static string TEXT_GATHER_SMITH_BODY = "{0} 쪽에서 장비를 잔뜩 맞췄어. 큰 원정이라도 가려나 봐.";
        public static string TEXT_GATHER_SMITH_NOTE = "{0} 쪽이 장비를 크게 맞췄다";
        public static string TEXT_GATHER_LETTER_TITLE = "서신을 읽는다";
        public static string TEXT_GATHER_TAVERN_TITLE = "술자리에서 들었다";
        public static string TEXT_GATHER_TAVERN_BODY = "{0} 얘기가 돌긴 하던데, 무슨 일인지는 못 들었어.";

        // 여관 소문처럼 누구 이야기인지만 아는 수첩 한 줄. 일하다 주워들은 말에도 쓴다.
        public static string TEXT_GATHER_VAGUE_NOTE = "{0} 쪽에 무슨 일이 있다.";

        public static string TEXT_GATHER_SPENT = "{0:N0}을 썼습니다. ";
        public static string TEXT_GATHER_TIME_LEFT = "남은 행동력 {0}.";
        public static string TEXT_GATHER_SECRET = "(아직 아무도 모르는 이야기다) ";
    }
}
